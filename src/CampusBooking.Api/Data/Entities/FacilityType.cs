using System.ComponentModel.DataAnnotations;

namespace CampusBooking.Api.Data.Entities;

/// <summary>
/// Represents a category of bookable space (e.g. Lab, Classroom, Meeting Room).
/// FacilityManagers can define custom types via the desktop app (FR2).
/// RequiresApproval drives the conditional approval flow in FR4:
/// when true, new bookings start as Pending instead of Confirmed.
/// </summary>
public class FacilityType
{
    public int Id { get; set; }

    [Required, MaxLength(64)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// When true, bookings for facilities of this type require explicit manager approval (FR4).
    /// Labs are the canonical example.
    /// </summary>
    public bool RequiresApproval { get; set; }

    /// <summary>Soft-delete flag; deactivated types are hidden from search results.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>All facilities that belong to this type.</summary>
    public ICollection<Facility> Facilities { get; set; } = new List<Facility>();
}
