using CampusBooking.Shared.Enums;

namespace CampusBooking.Api.Data.Entities;

public class Booking
{
    public int Id { get; set; }

    public int FacilityId { get; set; }
    public Facility Facility { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public DateOnly Date { get; set; }

    // Hour of day, 8 to 19 (08:00 through 19:00). Filtered unique index on (FacilityId, Date, TimeSlot) lives in DDL.
    public int TimeSlot { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
