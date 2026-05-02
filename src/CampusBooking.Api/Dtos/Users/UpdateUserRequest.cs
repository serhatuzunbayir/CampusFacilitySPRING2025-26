using System.ComponentModel.DataAnnotations;

namespace CampusBooking.Api.Dtos.Users;

public class UpdateUserRequest
{
    [Required, MaxLength(128)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;
}
