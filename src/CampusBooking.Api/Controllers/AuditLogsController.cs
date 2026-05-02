using CampusBooking.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusBooking.Api.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Roles = "FacilityManager")]
public class AuditLogsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AuditLogsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? entityType = null,
        [FromQuery] string? entityId = null,
        [FromQuery] string? actorId = null,
        [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        pageSize = Math.Clamp(pageSize, 1, 200);
        page = Math.Max(1, page);

        var query = _db.AuditLogs
            .Include(a => a.ActorUser)
            .AsQueryable();

        if (!string.IsNullOrEmpty(entityType))
            query = query.Where(a => a.EntityType == entityType);

        if (!string.IsNullOrEmpty(entityId))
            query = query.Where(a => a.EntityId == entityId);

        if (!string.IsNullOrEmpty(actorId))
            query = query.Where(a => a.ActorUserId == actorId);

        if (dateFrom.HasValue)
            query = query.Where(a => DateOnly.FromDateTime(a.TimestampUtc) >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(a => DateOnly.FromDateTime(a.TimestampUtc) <= dateTo.Value);

        var total = await query.CountAsync();

        var logs = await query
            .OrderByDescending(a => a.TimestampUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.Id,
                a.Action,
                a.EntityType,
                a.EntityId,
                a.Details,
                a.TimestampUtc,
                ActorId = a.ActorUserId,
                ActorName = a.ActorUser != null ? a.ActorUser.DisplayName : null
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, data = logs });
    }
}
