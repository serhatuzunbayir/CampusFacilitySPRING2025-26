using System.Security.Claims;
using CampusBooking.Api.Data;
using CampusBooking.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusBooking.Api.Controllers;

/// <summary>
/// Provides each user's notification inbox (FR6).
/// The Web app polls this to display banners; the Desktop polls it on a 30-second timer
/// to show toast popups. Notifications are scoped to the authenticated user —
/// no user can read another user's inbox.
/// </summary>
[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly AppDbContext _db;

    public NotificationsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetMine([FromQuery] bool unreadOnly = false)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var query = _db.Notifications
            .Where(n => n.RecipientUserId == userId);

        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        var notifications = await query
            .OrderByDescending(n => n.CreatedAtUtc)
            .Select(n => new
            {
                n.Id,
                n.Kind,
                n.Message,
                n.IsRead,
                n.CreatedAtUtc
            })
            .ToListAsync();

        return Ok(notifications);
    }

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.RecipientUserId == userId);

        if (notification is null) return NotFound();

        notification.IsRead = true;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        await _db.Notifications
            .Where(n => n.RecipientUserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

        return NoContent();
    }
}
