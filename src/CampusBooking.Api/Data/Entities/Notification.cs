using System.ComponentModel.DataAnnotations;
using CampusBooking.Shared;

namespace CampusBooking.Api.Data.Entities;

/// <summary>
/// Persisted inbox entry for a single user (FR6).
/// Created by NotificationWriter whenever a business event occurs.
/// Both the Web inbox page and the Desktop notification panel read from this table.
/// IsRead lets the UI badge unread counts and mark items as seen.
/// </summary>
public class Notification
{
    public int Id { get; set; }

    /// <summary>The user who should see this notification.</summary>
    public string RecipientUserId { get; set; } = string.Empty;
    public ApplicationUser RecipientUser { get; set; } = null!;

    /// <summary>The event category, matches NotificationKind enum in Shared.</summary>
    public NotificationKind Kind { get; set; }

    [Required, MaxLength(512)]
    public string Message { get; set; } = string.Empty;

    /// <summary>False until the user opens the inbox and reads the item.</summary>
    public bool IsRead { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
