using CampusBooking.Api.Data;
using CampusBooking.Api.Data.Entities;
using CampusBooking.Shared;
using Microsoft.AspNetCore.Identity;

namespace CampusBooking.Api.Services;

public class NotificationService
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public NotificationService(AppDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public async Task SendAsync(string recipientUserId, NotificationKind kind, string message)
    {
        _db.Notifications.Add(new Notification
        {
            RecipientUserId = recipientUserId,
            Kind = kind,
            Message = message
        });
        await _db.SaveChangesAsync();
    }

    public async Task SendToRoleAsync(string roleName, NotificationKind kind, string message)
    {
        var users = await _users.GetUsersInRoleAsync(roleName);
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
    }
}
