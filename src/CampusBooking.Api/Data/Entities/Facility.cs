using System.ComponentModel.DataAnnotations;

namespace CampusBooking.Api.Data.Entities;

public class Facility
{
    public int Id { get; set; }

    [Required, MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    public int FacilityTypeId { get; set; }
    public FacilityType FacilityType { get; set; } = null!;

    public int Capacity { get; set; }

    [Required, MaxLength(256)]
    public string Location { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<MaintenanceIssue> MaintenanceIssues { get; set; } = new List<MaintenanceIssue>();
}
