namespace CampusBooking.Shared;

// Shared by API and Desktop. The desktop poller raises this so subscribers (e.g. MainForm) can show toast popups.
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
