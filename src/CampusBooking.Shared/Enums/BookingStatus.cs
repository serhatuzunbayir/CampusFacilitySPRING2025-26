namespace CampusBooking.Shared.Enums;

// Confirmed = auto-confirmed (Classroom / Meeting Room). Approved = manager approved (Lab). Order matters: enum is stored as int.
public enum BookingStatus
{
    Pending,
    Confirmed,
    Approved,
    Rejected,
    Cancelled
}
