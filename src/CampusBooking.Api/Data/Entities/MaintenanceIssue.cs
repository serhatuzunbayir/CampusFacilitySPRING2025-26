using System.ComponentModel.DataAnnotations;
using CampusBooking.Shared.Enums;

namespace CampusBooking.Api.Data.Entities;

/// <summary>
/// Represents a maintenance problem reported by a Student or Staff member (FR7).
/// Lifecycle: Open → Pending (assigned) → InProgress → Resolved.
/// An optional photo is stored on disk; PhotoPath holds the relative server path.
/// Assignment fields are populated by a FacilityManager via the desktop app (FR8).
/// Each status transition is timestamped for the audit trail required by NFR5.
/// </summary>
public class MaintenanceIssue
{
    public int Id { get; set; }

    public int FacilityId { get; set; }
    public Facility Facility { get; set; } = null!;

    /// <summary>The user who submitted the report.</summary>
    public string ReporterId { get; set; } = string.Empty;
    public ApplicationUser Reporter { get; set; } = null!;

    [Required, MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Low / Medium / High / Critical — set by the reporter (FR7).</summary>
    public IssueSeverity Severity { get; set; } = IssueSeverity.Low;

    /// <summary>Relative path to the uploaded photo, e.g. uploads/maintenance/{guid}.jpg. Null if no photo.</summary>
    [MaxLength(512)]
    public string? PhotoPath { get; set; }

    /// <summary>Current state in the maintenance workflow.</summary>
    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Open;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Maintenance Personnel assigned to fix the issue (nullable until assigned).</summary>
    public string? AssigneeId { get; set; }
    public ApplicationUser? Assignee { get; set; }

    /// <summary>FacilityManager who made the assignment.</summary>
    public string? AssignedById { get; set; }
    public ApplicationUser? AssignedBy { get; set; }

    /// <summary>Timestamps for each stage transition (FR9).</summary>
    public DateTime? AssignedAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
}
