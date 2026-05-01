namespace CampusBooking.Api.Dtos.Bookings;

public class AvailabilityQuery
{
    public DateOnly Date { get; set; }
    public int TimeSlot { get; set; }
    public int? FacilityTypeId { get; set; }
    public int? MinCapacity { get; set; }
}
