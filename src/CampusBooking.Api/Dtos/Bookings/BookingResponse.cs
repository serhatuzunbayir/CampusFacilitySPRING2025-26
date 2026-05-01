using CampusBooking.Shared.Enums;

namespace CampusBooking.Api.Dtos.Bookings;

public class BookingResponse
{
    public int Id { get; set; }
    public int FacilityId { get; set; }
    public string FacilityName { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserDisplayName { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public int TimeSlot { get; set; }
    public BookingStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
