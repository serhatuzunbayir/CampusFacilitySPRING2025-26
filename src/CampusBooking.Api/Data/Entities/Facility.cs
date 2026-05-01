using System.ComponentModel.DataAnnotations;

namespace CampusBooking.Api.Data.Entities;

/// <summary>
/// Represents a single bookable room or space on campus (FR2).
/// Each facility belongs to a FacilityType, which determines whether
/// reservations require manager approval.
/// IsActive supports soft-delete so historical bookings remain intact.
/// </summary>
public class Facility
{
    public int Id { get; set; }

    [Required, MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Foreign key to FacilityType (e.g. Lab, Classroom).</summary>
    public int FacilityTypeId { get; set; }
    public FacilityType FacilityType { get; set; } = null!;

    /// <summary>Maximum number of occupants; used for minimum-capacity filtering in FR3.</summary>
    public int Capacity { get; set; }

    [Required, MaxLength(256)]
    public string Location { get; set; } = string.Empty;

    /// <summary>False means the facility is deactivated and excluded from availability search.</summary>
    public bool IsActive { get; set; } = true;

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<MaintenanceIssue> MaintenanceIssues { get; set; } = new List<MaintenanceIssue>();
}
