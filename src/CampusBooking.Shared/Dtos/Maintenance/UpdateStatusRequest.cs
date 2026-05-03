using CampusBooking.Shared.Enums;

namespace CampusBooking.Shared.Dtos.Maintenance;

public class UpdateStatusRequest
{
    public MaintenanceStatus Status { get; set; }
}
