namespace CampusBooking.Shared.Enums;

// Lifecycle: Open (just reported) -> Pending (assigned) -> InProgress -> Resolved.
public enum MaintenanceStatus
{
    Open,
    Pending,
    InProgress,
    Resolved
}
