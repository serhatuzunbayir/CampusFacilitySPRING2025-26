using CampusBooking.Api.Data;
using CampusBooking.Api.Data.Entities;
using CampusBooking.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CampusBooking.Api.Services;

/// <summary>
/// Scoped service that persists a notification to the database and then
/// fires the in-process NotificationHandler delegate (FR6).
/// Separating persistence from the delegate call keeps controllers thin
/// and ensures the inbox is always consistent with the event history.
/// </summary>
public class NotificationWriter
{
    private readonly AppDbContext _db;
    private readonly NotificationService _notificationService;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationWriter(AppDbContext db, NotificationService notificationService,
        UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _notificationService = notificationService;
        _userManager = userManager;
    }

    /// <summary>
    /// Saves a notification for a single recipient and fires the delegate.
    /// </summary>
    public async Task SendAsync(string recipientUserId, NotificationKind kind, string message)
    {
        _db.Notifications.Add(new Notification
        {
            RecipientUserId = recipientUserId,
            Kind = kind,
            Message = message
        });
        await _db.SaveChangesAsync();

        // Fire the in-process event so subscribed UI handlers (toast, banner) react immediately
        _notificationService.Raise(kind, message);
    }

    /// <summary>
    /// Saves one notification per user in the given role and fires the delegate once.
    /// Used for broadcast events like BookingCancelled (all FacilityManagers receive it).
    /// </summary>
    public async Task SendToRoleAsync(string roleName, NotificationKind kind, string message)
    {
        var users = await _userManager.GetUsersInRoleAsync(roleName);

        // Create one inbox entry per recipient so each user can mark it read independently
        foreach (var user in users)
        {
            _db.Notifications.Add(new Notification
            {
                RecipientUserId = user.Id,
                Kind = kind,
                Message = message
            });
        }
        await _db.SaveChangesAsync();

        _notificationService.Raise(kind, message);
    }
}
