using System.Security.Claims;
using CampusBooking.Api.Data;
using CampusBooking.Api.Data.Entities;
using CampusBooking.Api.Services;
using CampusBooking.Shared;
using CampusBooking.Shared.Dtos.Bookings;
using CampusBooking.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusBooking.Api.Controllers;

[ApiController]
[Route("api/bookings")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly NotificationService _notifier;

    public BookingsController(AppDbContext db, NotificationService notifier)
    {
        _db = db;
        _notifier = notifier;
    }

    [HttpGet("availability")]
    public async Task<ActionResult<List<object>>> GetAvailability([FromQuery] AvailabilityQuery query)
    {
        if (query.TimeSlot < 8 || query.TimeSlot > 19)
            return BadRequest(new { message = "TimeSlot must be between 8 and 19." });

        // Skip rejected and cancelled rows so a freed slot shows up as available again.
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

        // First conflict check is in-app so we can return a friendly per-slot message.
        var conflicting = await _db.Bookings
            .Where(b => b.FacilityId == request.FacilityId &&
                        b.Date == request.Date &&
                        request.TimeSlots.Contains(b.TimeSlot) &&
                        b.Status != BookingStatus.Rejected &&
                        b.Status != BookingStatus.Cancelled)
            .Select(b => b.TimeSlot)
            .ToListAsync();

        if (conflicting.Any())
            return Conflict(new { message = $"Slot already booked: {string.Join(", ", conflicting.Select(t => $"{t:00}:00"))}." });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // Labs need a manager decision; classrooms and meeting rooms auto-confirm.
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

        var audit = new AuditLog
        {
            ActorUserId = userId,
            Action = status == BookingStatus.Confirmed ? "BookingConfirmed" : "BookingPending",
            EntityType = "Booking",
            TimestampUtc = DateTime.UtcNow
        };

        _db.Bookings.AddRange(bookings);
        _db.AuditLogs.Add(audit);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Two requests can race past the in-app check; the filtered unique index in DDL is the final guard.
            return Conflict(new { message = "Slot already booked." });
        }

        audit.EntityId = string.Join(",", bookings.Select(b => b.Id));
        await _db.SaveChangesAsync();

        if (status == BookingStatus.Confirmed)
            await _notifier.SendAsync(userId, NotificationKind.BookingConfirmed,
                $"Your booking for {facility.Name} on {request.Date} has been confirmed.");

        var responses = await _db.Bookings
            .Include(b => b.Facility)
            .Include(b => b.User)
            .Where(b => bookings.Select(x => x.Id).Contains(b.Id))
            .Select(b => ToResponse(b))
            .ToListAsync();

        return CreatedAtAction(nameof(GetMyBookings), null, responses);
    }

    [HttpGet]
    [Authorize(Roles = "FacilityManager")]
    public async Task<ActionResult<List<BookingResponse>>> GetAll(
        [FromQuery] int? facilityId = null,
        [FromQuery] DateOnly? date = null,
        [FromQuery] BookingStatus? status = null)
    {
        var query = _db.Bookings
            .Include(b => b.Facility)
            .Include(b => b.User)
            .AsQueryable();

        if (facilityId.HasValue)
            query = query.Where(b => b.FacilityId == facilityId.Value);

        if (date.HasValue)
            query = query.Where(b => b.Date == date.Value);

        if (status.HasValue)
            query = query.Where(b => b.Status == status.Value);

        var bookings = await query
            .OrderBy(b => b.Date).ThenBy(b => b.TimeSlot)
            .Select(b => ToResponse(b))
            .ToListAsync();

        return Ok(bookings);
    }

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

    [HttpDelete("{id}")]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var booking = await _db.Bookings
            .Include(b => b.Facility)
            .FirstOrDefaultAsync(b => b.Id == id);
        if (booking is null) return NotFound();
        if (booking.UserId != userId)
            return Forbid();

        if (!IsWithinCancellationWindow(booking))
            return BadRequest(new { message = "Cancellations must be made at least 2 hours before the slot starts." });

        var facilityName = booking.Facility.Name;
        var bookingDate = booking.Date;

        _db.Bookings.Remove(booking);
        await _db.SaveChangesAsync();
        await WriteAuditAsync(userId, "BookingCancelled", "Booking", id.ToString());
        await _notifier.SendToRoleAsync("FacilityManager", NotificationKind.BookingCancelled,
            $"A booking for {facilityName} on {bookingDate} has been cancelled.");

        return NoContent();
    }

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
            return Conflict(new { message = "Slot already booked." });
        }

        await WriteAuditAsync(userId, "BookingModified", "Booking", id.ToString());

        return Ok(ToResponse(booking));
    }

    // Users can cancel or modify their own booking up to 2 hours before it starts.
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
