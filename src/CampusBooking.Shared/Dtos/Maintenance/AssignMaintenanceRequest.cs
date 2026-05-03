using System.ComponentModel.DataAnnotations;

namespace CampusBooking.Shared.Dtos.Maintenance;

public class AssignMaintenanceRequest
{
    [Required]
    public string AssigneeId { get; set; } = string.Empty;
}
