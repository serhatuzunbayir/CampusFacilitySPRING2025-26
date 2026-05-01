namespace CampusBooking.Api.Dtos.Bookings;

public class ModifyBookingRequest
{
    public DateOnly NewDate { get; set; }
    public int NewTimeSlot { get; set; }
}
