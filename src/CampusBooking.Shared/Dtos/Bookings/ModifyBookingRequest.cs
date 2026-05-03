namespace CampusBooking.Shared.Dtos.Bookings;

public class ModifyBookingRequest
{
    public DateOnly NewDate { get; set; }
    public int NewTimeSlot { get; set; }
}
