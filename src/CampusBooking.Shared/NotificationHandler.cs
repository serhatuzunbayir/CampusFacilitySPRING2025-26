namespace CampusBooking.Shared;

public delegate void NotificationHandler(NotificationKind kind, string message);

public enum NotificationKind
{
    BookingConfirmed,
    BookingApproved,
    BookingRejected,
    BookingCancelled,
    MaintenanceAssigned,
    MaintenanceStatusChanged
}
