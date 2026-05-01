using CampusBooking.Shared.Enums;

namespace CampusBooking.Api.Dtos.Maintenance;

public class MaintenanceIssueResponse
{
    public int Id { get; set; }
    public int FacilityId { get; set; }
    public string FacilityName { get; set; } = string.Empty;
    public string ReporterId { get; set; } = string.Empty;
    public string ReporterName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IssueSeverity Severity { get; set; }
    public string? PhotoPath { get; set; }
    public MaintenanceStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? AssigneeId { get; set; }
    public string? AssigneeName { get; set; }
    public string? AssignedById { get; set; }
    public string? AssignedByName { get; set; }
    public DateTime? AssignedAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
}
