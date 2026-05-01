using CampusBooking.Shared.Enums;

namespace CampusBooking.Api.Dtos.Maintenance;

public class UpdateStatusRequest
{
    public MaintenanceStatus Status { get; set; }
}
