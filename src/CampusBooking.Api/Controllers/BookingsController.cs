using System.Security.Claims;
using CampusBooking.Api.Data;
using CampusBooking.Api.Data.Entities;
using CampusBooking.Api.Dtos.Bookings;
using CampusBooking.Api.Services;
using CampusBooking.Shared;
using CampusBooking.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusBooking.Api.Controllers;

/// <summary>
/// Manages the full booking lifecycle: availability search (FR3), reservation with
/// conditional approval (FR4), cancellation and modification with a 2-hour window (FR5).
/// Also exposes pending-approval and approve/reject endpoints for FacilityManagers.
/// Every state change is written to AuditLog and triggers a notification (FR6).
/// </summary>
[ApiController]
[Route("api/bookings")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly NotificationWriter _notifier;

    public BookingsController(AppDbContext db, NotificationWriter notifier)
    {
        _db = db;
        _notifier = notifier;
    }

    // FR3 — search available facilities for a given date + time slot
    [HttpGet("availability")]
    public async Task<ActionResult<List<object>>> GetAvailability([FromQuery] AvailabilityQuery query)
    {
        if (query.TimeSlot < 8 || query.TimeSlot > 19)
            return BadRequest(new { message = "TimeSlot must be between 8 and 19 (08:00–19:00)." });

        var bookedFacilityIds = await _db.Bookings
            .Where(b => b.Date == query.Date &&
                        b.TimeSlot == query.TimeSlot &&
                        b.Status != BookingStatus.Rejected &&
                        b.Status != BookingStatus.Cancelled)
            .Select(b => b.FacilityId)
            .ToListAsync();

        var facilitiesQuery = _db.Facilities
            .Include(f => f.FacilityType)
            .Where(f => f.IsActive && !bookedFacilityIds.Contains(f.Id));

        if (query.FacilityTypeId.HasValue)
            facilitiesQuery = facilitiesQuery.Where(f => f.FacilityTypeId == query.FacilityTypeId.Value);

        if (query.MinCapacity.HasValue)
            facilitiesQuery = facilitiesQuery.Where(f => f.Capacity >= query.MinCapacity.Value);

        var results = await facilitiesQuery
            .OrderBy(f => f.Name)
            .Select(f => new
            {
                f.Id,
                f.Name,
                f.FacilityTypeId,
                FacilityTypeName = f.FacilityType.Name,
                f.FacilityType.RequiresApproval,
                f.Capacity,
                f.Location
            })
            .ToListAsync();

        return Ok(results);
    }

    // FR4 — create a booking (one record per time slot)
    [HttpPost]
    [Authorize(Roles = "Student,Staff")]
    public async Task<ActionResult<List<BookingResponse>>> Create([FromBody] CreateBookingRequest request)
    {
        if (request.TimeSlots.Any(t => t < 8 || t > 19))
            return BadRequest(new { message = "All time slots must be between 8 and 19." });

        var facility = await _db.Facilities
            .Include(f => f.FacilityType)
            .FirstOrDefaultAsync(f => f.Id == request.FacilityId && f.IsActive);

        if (facility is null)
            return NotFound(new { message = "Facility not found or inactive." });

        // FR4: conflict check for all requested slots
        var conflicting = await _db.Bookings
            .Where(b => b.FacilityId == request.FacilityId &&
                        b.Date == request.Date &&
                        request.TimeSlots.Contains(b.TimeSlot) &&
                        b.Status != BookingStatus.Rejected &&
                        b.Status != BookingStatus.Cancelled)
            .Select(b => b.TimeSlot)
            .ToListAsync();

        if (conflicting.Any())
            return Conflict(new { message = $"Time slot(s) already booked: {string.Join(", ", conflicting.Select(t => $"{t:00}:00"))}." });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // FR4: Labs require approval, others auto-confirm
        var status = facility.FacilityType.RequiresApproval
            ? BookingStatus.Pending
            : BookingStatus.Confirmed;

        var bookings = request.TimeSlots.Select(slot => new Booking
        {
            FacilityId = request.FacilityId,
            UserId = userId,
            Date = request.Date,
            TimeSlot = slot,
            Status = status
        }).ToList();

        _db.Bookings.AddRange(bookings);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // NFR3: unique constraint violation — concurrent booking
            return Conflict(new { message = "One or more slots were booked concurrently. Please try again." });
        }

        await WriteAuditAsync(userId, status == BookingStatus.Confirmed ? "BookingConfirmed" : "BookingPending",
            "Booking", string.Join(",", bookings.Select(b => b.Id)));

        if (status == BookingStatus.Confirmed)
            await _notifier.SendAsync(userId, NotificationKind.BookingConfirmed,
                $"Your booking for {facility.Name} on {request.Date} has been confirmed.");

        var responses = await _db.Bookings
            .Include(b => b.Facility)
            .Include(b => b.User)
            .Where(b => bookings.Select(x => x.Id).Contains(b.Id))
            .Select(b => ToResponse(b))
            .ToListAsync();

        return CreatedAtAction(nameof(GetMyBookings), responses);
    }

    // list caller's own bookings
    [HttpGet("mine")]
    public async Task<ActionResult<List<BookingResponse>>> GetMyBookings()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var bookings = await _db.Bookings
            .Include(b => b.Facility)
            .Include(b => b.User)
            .Where(b => b.UserId == userId)
            .OrderBy(b => b.Date).ThenBy(b => b.TimeSlot)
            .Select(b => ToResponse(b))
            .ToListAsync();

        return Ok(bookings);
    }

    // FR4 — list pending bookings for manager review
    [HttpGet("pending")]
    [Authorize(Roles = "FacilityManager")]
    public async Task<ActionResult<List<BookingResponse>>> GetPending()
    {
        var bookings = await _db.Bookings
            .Include(b => b.Facility)
            .Include(b => b.User)
            .Where(b => b.Status == BookingStatus.Pending)
            .OrderBy(b => b.Date).ThenBy(b => b.TimeSlot)
            .Select(b => ToResponse(b))
            .ToListAsync();

        return Ok(bookings);
    }

    // FR4 — approve a pending booking
    [HttpPut("{id}/approve")]
    [Authorize(Roles = "FacilityManager")]
    public async Task<ActionResult<BookingResponse>> Approve(int id)
    {
        var booking = await _db.Bookings
            .Include(b => b.Facility)
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking is null) return NotFound();
        if (booking.Status != BookingStatus.Pending)
            return BadRequest(new { message = "Only pending bookings can be approved." });

        booking.Status = BookingStatus.Approved;
        await _db.SaveChangesAsync();

        var managerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await WriteAuditAsync(managerId, "BookingApproved", "Booking", id.ToString());
        await _notifier.SendAsync(booking.UserId, NotificationKind.BookingApproved,
            $"Your booking for {booking.Facility.Name} on {booking.Date} has been approved.");

        return Ok(ToResponse(booking));
    }

    // FR4 — reject a pending booking
    [HttpPut("{id}/reject")]
    [Authorize(Roles = "FacilityManager")]
    public async Task<ActionResult<BookingResponse>> Reject(int id)
    {
        var booking = await _db.Bookings
            .Include(b => b.Facility)
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking is null) return NotFound();
        if (booking.Status != BookingStatus.Pending)
            return BadRequest(new { message = "Only pending bookings can be rejected." });

        booking.Status = BookingStatus.Rejected;
        await _db.SaveChangesAsync();

        var managerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await WriteAuditAsync(managerId, "BookingRejected", "Booking", id.ToString());
        await _notifier.SendAsync(booking.UserId, NotificationKind.BookingRejected,
            $"Your booking for {booking.Facility.Name} on {booking.Date} has been rejected.");

        return Ok(ToResponse(booking));
    }

    // FR5 — cancel a booking (must be > 2 hours before slot start)
    [HttpDelete("{id}")]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var booking = await _db.Bookings.FindAsync(id);
        if (booking is null) return NotFound();
        if (booking.UserId != userId)
            return Forbid();

        if (!IsWithinCancellationWindow(booking))
            return BadRequest(new { message = "Cancellations must be made at least 2 hours before the slot starts." });

        var facilityName = booking.Facility?.Name ?? booking.FacilityId.ToString();
        var bookingDate = booking.Date;

        _db.Bookings.Remove(booking);
        await _db.SaveChangesAsync();
        await WriteAuditAsync(userId, "BookingCancelled", "Booking", id.ToString());
        await _notifier.SendToRoleAsync("FacilityManager", NotificationKind.BookingCancelled,
            $"A booking for {facilityName} on {bookingDate} has been cancelled.");

        return NoContent();
    }

    // FR5 — modify a booking (change date and/or time slot, must be > 2 hours before old slot)
    [HttpPut("{id}")]
    public async Task<ActionResult<BookingResponse>> Modify(int id, [FromBody] ModifyBookingRequest request)
    {
        if (request.NewTimeSlot < 8 || request.NewTimeSlot > 19)
            return BadRequest(new { message = "TimeSlot must be between 8 and 19." });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var booking = await _db.Bookings
            .Include(b => b.Facility)
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking is null) return NotFound();
        if (booking.UserId != userId)
            return Forbid();

        if (!IsWithinCancellationWindow(booking))
            return BadRequest(new { message = "Modifications must be made at least 2 hours before the slot starts." });

        // conflict check for the new slot
        var conflict = await _db.Bookings.AnyAsync(b =>
            b.FacilityId == booking.FacilityId &&
            b.Date == request.NewDate &&
            b.TimeSlot == request.NewTimeSlot &&
            b.Id != id &&
            b.Status != BookingStatus.Rejected &&
            b.Status != BookingStatus.Cancelled);

        if (conflict)
            return Conflict(new { message = $"Slot {request.NewTimeSlot:00}:00 on {request.NewDate} is already booked." });

        booking.Date = request.NewDate;
        booking.TimeSlot = request.NewTimeSlot;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "Slot was booked concurrently. Please try again." });
        }

        await WriteAuditAsync(userId, "BookingModified", "Booking", id.ToString());

        return Ok(ToResponse(booking));
    }

    /// <summary>
    /// Returns true if the current time is more than 2 hours before the slot starts (FR5).
    /// Slot start is derived from Date + TimeSlot hour, treated as UTC.
    /// </summary>
    private static bool IsWithinCancellationWindow(Booking booking)
    {
        var slotStart = booking.Date.ToDateTime(new TimeOnly(booking.TimeSlot, 0), DateTimeKind.Utc);
        return DateTime.UtcNow < slotStart.AddHours(-2);
    }

    private static BookingResponse ToResponse(Booking b) => new()
    {
        Id = b.Id,
        FacilityId = b.FacilityId,
        FacilityName = b.Facility.Name,
        UserId = b.UserId,
        UserDisplayName = b.User.DisplayName,
        Date = b.Date,
        TimeSlot = b.TimeSlot,
        Status = b.Status,
        CreatedAtUtc = b.CreatedAtUtc
    };

    private async Task WriteAuditAsync(string actorId, string action, string entityType, string? entityId)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            TimestampUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }
}
