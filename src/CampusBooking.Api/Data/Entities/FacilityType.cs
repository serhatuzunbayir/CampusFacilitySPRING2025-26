using System.ComponentModel.DataAnnotations;

namespace CampusBooking.Api.Data.Entities;

public class FacilityType
{
    public int Id { get; set; }

    [Required, MaxLength(64)]
    public string Name { get; set; } = string.Empty;

    // True for Lab so reservations enter Pending; false for Classroom and Meeting Room which auto-confirm.
    public bool RequiresApproval { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Facility> Facilities { get; set; } = new List<Facility>();
}
