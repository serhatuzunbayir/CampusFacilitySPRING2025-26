namespace CampusBooking.Shared;

/// <summary>
/// Singleton service that owns the NotificationHandler event.
/// Registered in the API as a singleton and in each client app separately,
/// so every layer can subscribe its own UI handlers without duplicating event logic.
/// </summary>
public class NotificationService
{
    /// <summary>
    /// Subscribers (toast handlers in Desktop, banner handlers in Web) attach here.
    /// </summary>
    public event NotificationHandler? OnNotification;

    /// <summary>
    /// Fires the event. Called by NotificationWriter after persisting the notification to the DB.
    /// </summary>
    public void Raise(NotificationKind kind, string message)
        => OnNotification?.Invoke(kind, message);
}
