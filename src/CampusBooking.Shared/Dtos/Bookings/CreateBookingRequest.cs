using System.ComponentModel.DataAnnotations;

namespace CampusBooking.Shared.Dtos.Bookings;

public class CreateBookingRequest
{
    public int FacilityId { get; set; }

    public DateOnly Date { get; set; }

    [Required, MinLength(1)]
    public List<int> TimeSlots { get; set; } = new();
}
