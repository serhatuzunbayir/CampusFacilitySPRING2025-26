using CampusBooking.Shared.Enums;

namespace CampusBooking.Api.Data.Entities;

/// <summary>
/// Represents one hourly time slot reserved by a user for a specific facility (FR4).
/// Each row covers exactly one hour: Date + TimeSlot (8–19) together identify the slot.
/// A unique index on (FacilityId, Date, TimeSlot) prevents double-booking (NFR3).
/// Cancelled and rejected bookings are hard-deleted so the slot is immediately freed;
/// their history lives in AuditLog.
/// </summary>
public class Booking
{
    public int Id { get; set; }

    public int FacilityId { get; set; }
    public Facility Facility { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    /// <summary>The calendar date of the reservation.</summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// Start hour of the reserved slot (8 = 08:00–09:00, 19 = 19:00–20:00).
    /// Valid range: 8–19 inclusive (FR3).
    /// </summary>
    public int TimeSlot { get; set; }

    /// <summary>
    /// Pending → manager must approve (Lab bookings).
    /// Confirmed / Approved → access granted.
    /// Rejected / Cancelled → row is deleted.
    /// </summary>
    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
