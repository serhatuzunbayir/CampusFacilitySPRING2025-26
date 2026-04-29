using Microsoft.AspNetCore.Identity;

namespace CampusBooking.Api.Data.Entities;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
}
