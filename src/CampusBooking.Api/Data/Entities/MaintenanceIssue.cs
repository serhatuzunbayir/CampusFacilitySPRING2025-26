using System.ComponentModel.DataAnnotations;
using CampusBooking.Shared.Enums;

namespace CampusBooking.Api.Data.Entities;

public class MaintenanceIssue
{
    public int Id { get; set; }

    public int FacilityId { get; set; }
    public Facility Facility { get; set; } = null!;

    public string ReporterId { get; set; } = string.Empty;
    public ApplicationUser Reporter { get; set; } = null!;

    [Required, MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    public IssueSeverity Severity { get; set; } = IssueSeverity.Low;

    [MaxLength(512)]
    public string? PhotoPath { get; set; }

    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Open;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public string? AssigneeId { get; set; }
    public ApplicationUser? Assignee { get; set; }

    public string? AssignedById { get; set; }
    public ApplicationUser? AssignedBy { get; set; }

    public DateTime? AssignedAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
}
