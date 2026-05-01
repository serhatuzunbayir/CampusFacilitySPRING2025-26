namespace CampusBooking.Shared;

/// <summary>
/// Delegate used by both the Web and Desktop applications to handle in-app notifications.
/// Defined once here in Shared so neither client duplicates the signature.
/// </summary>
/// <param name="kind">The category of event that occurred.</param>
/// <param name="message">Human-readable description of the event.</param>
public delegate void NotificationHandler(NotificationKind kind, string message);

/// <summary>
/// Identifies which business event triggered a notification (FR6).
/// </summary>
public enum NotificationKind
{
    BookingConfirmed,
    BookingApproved,
    BookingRejected,
    BookingCancelled,
    MaintenanceAssigned,
    MaintenanceStatusChanged
}
