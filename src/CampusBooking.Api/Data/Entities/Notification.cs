using System.ComponentModel.DataAnnotations;
using CampusBooking.Shared;

namespace CampusBooking.Api.Data.Entities;

public class Notification
{
    public int Id { get; set; }

    public string RecipientUserId { get; set; } = string.Empty;
    public ApplicationUser RecipientUser { get; set; } = null!;

    public NotificationKind Kind { get; set; }

    [Required, MaxLength(512)]
    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
