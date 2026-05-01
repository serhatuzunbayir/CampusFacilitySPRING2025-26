using System.ComponentModel.DataAnnotations;
using CampusBooking.Shared.Enums;

namespace CampusBooking.Api.Dtos.Maintenance;

public class CreateMaintenanceIssueRequest
{
    public int FacilityId { get; set; }

    [Required, MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    public IssueSeverity Severity { get; set; } = IssueSeverity.Low;

    public IFormFile? Photo { get; set; }
}
