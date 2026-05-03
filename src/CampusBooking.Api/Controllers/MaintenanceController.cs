using System.Security.Claims;
using CampusBooking.Api.Data;
using CampusBooking.Api.Data.Entities;
using CampusBooking.Api.Dtos;
using CampusBooking.Api.Services;
using CampusBooking.Shared;
using CampusBooking.Shared.Dtos.Maintenance;
using CampusBooking.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusBooking.Api.Controllers;

[ApiController]
[Route("api/maintenance")]
[Authorize]
public class MaintenanceController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly NotificationService _notifier;

    public MaintenanceController(AppDbContext db, IWebHostEnvironment env, NotificationService notifier)
    {
        _db = db;
        _env = env;
        _notifier = notifier;
    }

    [HttpPost]
    [Authorize(Roles = "Student,Staff")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<MaintenanceIssueResponse>> Create([FromForm] CreateMaintenanceIssueForm form)
    {
        var facility = await _db.Facilities.FindAsync(form.FacilityId);
        if (facility is null)
            return NotFound(new { message = "Facility not found." });

        string? photoPath = null;
        if (form.Photo is not null)
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var ext = Path.GetExtension(form.Photo.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
                return BadRequest(new { message = "Only image files are allowed." });

            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var uploadDir = Path.Combine(webRoot, "uploads", "maintenance");
            Directory.CreateDirectory(uploadDir);

            var fileName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(uploadDir, fileName);

            await using var stream = System.IO.File.Create(fullPath);
            await form.Photo.CopyToAsync(stream);

            photoPath = Path.Combine("uploads", "maintenance", fileName).Replace('\\', '/');
        }

        var reporterId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var issue = new MaintenanceIssue
        {
            FacilityId = form.FacilityId,
            ReporterId = reporterId,
            Description = form.Description,
            Severity = form.Severity,
            PhotoPath = photoPath,
            Status = MaintenanceStatus.Open
        };

        _db.MaintenanceIssues.Add(issue);
        await _db.SaveChangesAsync();

        await WriteAuditAsync(reporterId, "MaintenanceIssueReported", "MaintenanceIssue", issue.Id.ToString());

        return CreatedAtAction(nameof(GetById), new { id = issue.Id }, await ToResponseAsync(issue.Id));
    }

    [HttpGet]
    public async Task<ActionResult<List<MaintenanceIssueResponse>>> GetAll([FromQuery] MaintenanceStatus? status = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isManager = User.IsInRole("FacilityManager");

        var query = _db.MaintenanceIssues
            .Include(i => i.Facility)
            .Include(i => i.Reporter)
            .Include(i => i.Assignee)
            .Include(i => i.AssignedBy)
            .AsQueryable();

        if (!isManager)
            query = query.Where(i => i.ReporterId == userId);

        if (status.HasValue)
            query = query.Where(i => i.Status == status.Value);

        var issues = await query
            .OrderByDescending(i => i.CreatedAtUtc)
            .ToListAsync();

        return Ok(issues.Select(ToResponse).ToList());
    }

    [HttpGet("{id}/photo")]
    public async Task<IActionResult> GetPhoto(int id)
    {
        var issue = await _db.MaintenanceIssues.FindAsync(id);
        if (issue is null) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!User.IsInRole("FacilityManager") &&
            issue.ReporterId != userId &&
            issue.AssigneeId != userId)
            return Forbid();

        if (string.IsNullOrEmpty(issue.PhotoPath))
            return NotFound(new { message = "No photo." });

        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var fullPath = Path.Combine(webRoot, issue.PhotoPath.Replace('/', Path.DirectorySeparatorChar));
        if (!System.IO.File.Exists(fullPath))
            return NotFound(new { message = "Photo file missing." });

        var contentType = Path.GetExtension(fullPath).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png"            => "image/png",
            ".gif"            => "image/gif",
            ".webp"           => "image/webp",
            _                 => "application/octet-stream"
        };

        return PhysicalFile(fullPath, contentType);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MaintenanceIssueResponse>> GetById(int id)
    {
        var issue = await _db.MaintenanceIssues
            .Include(i => i.Facility)
            .Include(i => i.Reporter)
            .Include(i => i.Assignee)
            .Include(i => i.AssignedBy)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (issue is null) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isManager = User.IsInRole("FacilityManager");
        var isAssignee = issue.AssigneeId == userId;

        if (!isManager && issue.ReporterId != userId && !isAssignee)
            return Forbid();

        return Ok(ToResponse(issue));
    }

    [HttpPut("{id}/assign")]
    [Authorize(Roles = "FacilityManager")]
    public async Task<ActionResult<MaintenanceIssueResponse>> Assign(int id, [FromBody] AssignMaintenanceRequest request)
    {
        var issue = await _db.MaintenanceIssues
            .Include(i => i.Facility)
            .Include(i => i.Reporter)
            .Include(i => i.Assignee)
            .Include(i => i.AssignedBy)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (issue is null) return NotFound();

        var assignee = await _db.Users.FindAsync(request.AssigneeId);
        if (assignee is null)
            return BadRequest(new { message = "Assignee not found." });

        var isPersonnel = await _db.UserRoles
            .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .AnyAsync(x => x.UserId == request.AssigneeId && x.Name == nameof(UserRole.MaintenancePersonnel));

        if (!isPersonnel)
            return BadRequest(new { message = "Assignee must be MaintenancePersonnel." });

        var managerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        issue.AssigneeId = request.AssigneeId;
        issue.AssignedById = managerId;
        issue.AssignedAtUtc = DateTime.UtcNow;
        issue.Status = MaintenanceStatus.Pending;

        await _db.SaveChangesAsync();

        await WriteAuditAsync(managerId, "MaintenanceAssigned", "MaintenanceIssue", id.ToString(),
            $"Assigned to {request.AssigneeId}");
        await _notifier.SendAsync(request.AssigneeId, NotificationKind.MaintenanceAssigned,
            $"You have been assigned a maintenance task for {issue.Facility.Name}.");

        issue.Assignee = assignee;
        issue.AssignedBy = await _db.Users.FindAsync(managerId);

        return Ok(ToResponse(issue));
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "MaintenancePersonnel")]
    public async Task<ActionResult<MaintenanceIssueResponse>> UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
    {
        var issue = await _db.MaintenanceIssues
            .Include(i => i.Facility)
            .Include(i => i.Reporter)
            .Include(i => i.Assignee)
            .Include(i => i.AssignedBy)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (issue is null) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (issue.AssigneeId != userId)
            return Forbid();

        // Personnel can only move forward through Pending -> InProgress -> Resolved.
        var validTransitions = new Dictionary<MaintenanceStatus, MaintenanceStatus>
        {
            [MaintenanceStatus.Pending] = MaintenanceStatus.InProgress,
            [MaintenanceStatus.InProgress] = MaintenanceStatus.Resolved
        };

        if (!validTransitions.TryGetValue(issue.Status, out var allowedNext) || allowedNext != request.Status)
            return BadRequest(new { message = $"Cannot transition from {issue.Status} to {request.Status}." });

        issue.Status = request.Status;

        if (request.Status == MaintenanceStatus.InProgress)
            issue.StartedAtUtc = DateTime.UtcNow;
        else if (request.Status == MaintenanceStatus.Resolved)
            issue.ResolvedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await WriteAuditAsync(userId, $"Maintenance{request.Status}", "MaintenanceIssue", id.ToString());
        await _notifier.SendToRoleAsync("FacilityManager", NotificationKind.MaintenanceStatusChanged,
            $"Maintenance task for {issue.Facility.Name} is now {request.Status}.");

        return Ok(ToResponse(issue));
    }

    [HttpGet("logs")]
    [Authorize(Roles = "FacilityManager")]
    public async Task<IActionResult> GetLogs(
        [FromQuery] int? facilityId,
        [FromQuery] DateOnly? dateFrom,
        [FromQuery] DateOnly? dateTo,
        [FromQuery] MaintenanceStatus? status,
        [FromQuery] string? assigneeId,
        [FromQuery] string format = "json")
    {
        var query = _db.MaintenanceIssues
            .Include(i => i.Facility)
            .Include(i => i.Reporter)
            .Include(i => i.Assignee)
            .Include(i => i.AssignedBy)
            .AsQueryable();

        if (facilityId.HasValue)
            query = query.Where(i => i.FacilityId == facilityId.Value);

        if (dateFrom.HasValue)
            query = query.Where(i => DateOnly.FromDateTime(i.CreatedAtUtc) >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(i => DateOnly.FromDateTime(i.CreatedAtUtc) <= dateTo.Value);

        if (status.HasValue)
            query = query.Where(i => i.Status == status.Value);

        if (!string.IsNullOrEmpty(assigneeId))
            query = query.Where(i => i.AssigneeId == assigneeId);

        var issues = await query.OrderBy(i => i.CreatedAtUtc).ToListAsync();

        if (format.Equals("csv", StringComparison.OrdinalIgnoreCase))
            return await BuildCsvAsync(issues);

        return Ok(issues.Select(ToResponse).ToList());
    }

    private static async Task<FileContentResult> BuildCsvAsync(List<MaintenanceIssue> issues)
    {
        // Write to a temp file via FileStream so very large logs do not balloon in memory.
        var tempPath = Path.Combine(Path.GetTempPath(), $"maintenance-log-{Guid.NewGuid()}.csv");

        await using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
        await using (var writer = new StreamWriter(fs))
        {
            await writer.WriteLineAsync(
                "Id,Facility,Reporter,Description,Severity,Status,AssignedTo,AssignedBy," +
                "CreatedAtUtc,AssignedAtUtc,StartedAtUtc,ResolvedAtUtc");

            foreach (var i in issues)
            {
                await writer.WriteLineAsync(string.Join(",",
                    i.Id,
                    Escape(i.Facility.Name),
                    Escape(i.Reporter.DisplayName),
                    Escape(i.Description),
                    i.Severity,
                    i.Status,
                    Escape(i.Assignee?.DisplayName),
                    Escape(i.AssignedBy?.DisplayName),
                    i.CreatedAtUtc.ToString("o"),
                    i.AssignedAtUtc?.ToString("o") ?? "",
                    i.StartedAtUtc?.ToString("o") ?? "",
                    i.ResolvedAtUtc?.ToString("o") ?? ""));
            }
        }

        var bytes = await System.IO.File.ReadAllBytesAsync(tempPath);
        System.IO.File.Delete(tempPath);

        return new FileContentResult(bytes, "text/csv")
        {
            FileDownloadName = $"maintenance-log-{DateTime.UtcNow:yyyyMMdd}.csv"
        };
    }

    // RFC 4180 quoting: wrap in quotes if the field has a comma, quote, or newline; double any embedded quotes.
    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }

    private static MaintenanceIssueResponse ToResponse(MaintenanceIssue i) => new()
    {
        Id = i.Id,
        FacilityId = i.FacilityId,
        FacilityName = i.Facility.Name,
        ReporterId = i.ReporterId,
        ReporterName = i.Reporter.DisplayName,
        Description = i.Description,
        Severity = i.Severity,
        PhotoPath = i.PhotoPath,
        Status = i.Status,
        CreatedAtUtc = i.CreatedAtUtc,
        AssigneeId = i.AssigneeId,
        AssigneeName = i.Assignee?.DisplayName,
        AssignedById = i.AssignedById,
        AssignedByName = i.AssignedBy?.DisplayName,
        AssignedAtUtc = i.AssignedAtUtc,
        StartedAtUtc = i.StartedAtUtc,
        ResolvedAtUtc = i.ResolvedAtUtc
    };

    private async Task<MaintenanceIssueResponse> ToResponseAsync(int id)
    {
        var issue = await _db.MaintenanceIssues
            .Include(i => i.Facility)
            .Include(i => i.Reporter)
            .Include(i => i.Assignee)
            .Include(i => i.AssignedBy)
            .FirstAsync(i => i.Id == id);

        return ToResponse(issue);
    }

    private async Task WriteAuditAsync(string actorId, string action, string entityType, string? entityId, string? details = null)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            TimestampUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }
}
