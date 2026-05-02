using System.ComponentModel.DataAnnotations;

namespace CampusBooking.Api.Dtos.Users;

public class CreateUserRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required, MaxLength(128)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;
}
