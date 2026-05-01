using System.ComponentModel.DataAnnotations;

namespace CampusBooking.Api.Data.Entities;

/// <summary>
/// Immutable audit record written whenever a significant business action occurs (NFR5).
/// Covers: booking creation, approval/rejection, cancellation, maintenance reporting,
/// assignment, and status transitions.
/// Rows are never updated or deleted — they form a permanent history for FacilityManagers.
/// </summary>
public class AuditLog
{
    public long Id { get; set; }

    /// <summary>The user who performed the action.</summary>
    public string ActorUserId { get; set; } = string.Empty;
    public ApplicationUser? ActorUser { get; set; }

    /// <summary>Short verb describing what happened (e.g. "BookingConfirmed", "MaintenanceAssigned").</summary>
    [Required, MaxLength(64)]
    public string Action { get; set; } = string.Empty;

    /// <summary>Name of the affected domain object (e.g. "Booking", "MaintenanceIssue").</summary>
    [Required, MaxLength(64)]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Primary key of the affected row, stored as string for flexibility.</summary>
    [MaxLength(64)]
    public string? EntityId { get; set; }

    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Optional free-text with extra context (e.g. assignee ID on assignment events).</summary>
    [MaxLength(2000)]
    public string? Details { get; set; }
}
