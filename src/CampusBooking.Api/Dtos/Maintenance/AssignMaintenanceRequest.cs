using System.ComponentModel.DataAnnotations;

namespace CampusBooking.Api.Dtos.Maintenance;

public class AssignMaintenanceRequest
{
    [Required]
    public string AssigneeId { get; set; } = string.Empty;
}
