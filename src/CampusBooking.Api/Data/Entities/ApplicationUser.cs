using Microsoft.AspNetCore.Identity;

namespace CampusBooking.Api.Data.Entities;

/// <summary>
/// Extends ASP.NET Core Identity's IdentityUser with a display name.
/// All four roles (Student, Staff, FacilityManager, MaintenancePersonnel) share this type.
/// Accounts are created only by a FacilityManager; no public self-registration (FR1).
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>Full name shown in the UI and stored in JWT claims.</summary>
    public string DisplayName { get; set; } = string.Empty;
}
