using System.ComponentModel.DataAnnotations;
using CampusBooking.Shared.Enums;

namespace CampusBooking.Api.Dtos;

// API-side multipart binding model. Lives in the API project (not Shared) so the IFormFile
// dependency on Microsoft.AspNetCore.Http stays out of the WinForms Desktop reference graph.
// Wraps the same fields as Shared.Dtos.Maintenance.CreateMaintenanceIssueRequest plus the photo,
// which lets Swashbuckle generate a clean multipart schema (it can't merge a complex [FromForm]
// type with a separate [FromForm] IFormFile parameter).
public class CreateMaintenanceIssueForm
{
    public int FacilityId { get; set; }

    [Required, MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    public IssueSeverity Severity { get; set; }

    public IFormFile? Photo { get; set; }
}
