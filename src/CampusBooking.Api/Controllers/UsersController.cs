using System.Security.Claims;
using CampusBooking.Api.Data.Entities;
using CampusBooking.Api.Dtos.Users;
using CampusBooking.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusBooking.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "FacilityManager")]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    private static readonly string[] ValidRoles = Enum.GetNames<UserRole>();

    public UsersController(UserManager<ApplicationUser> userManager)
        => _userManager = userManager;

    // List all users, optionally filtered by role
    [HttpGet]
    public async Task<ActionResult<List<UserResponse>>> GetAll([FromQuery] string? role = null)
    {
        List<ApplicationUser> users;

        if (!string.IsNullOrEmpty(role))
        {
            if (!ValidRoles.Contains(role))
                return BadRequest(new { message = $"Invalid role. Valid roles: {string.Join(", ", ValidRoles)}" });

            users = (await _userManager.GetUsersInRoleAsync(role))
                .OrderBy(u => u.DisplayName)
                .ToList();
        }
        else
        {
            users = await _userManager.Users
                .OrderBy(u => u.DisplayName)
                .ToListAsync();
        }

        var responses = new List<UserResponse>(users.Count);
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            responses.Add(ToResponse(user, roles.FirstOrDefault() ?? string.Empty));
        }

        return Ok(responses);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserResponse>> GetById(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(ToResponse(user, roles.FirstOrDefault() ?? string.Empty));
    }

    // FR1 — FacilityManager creates accounts; no public self-registration
    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create([FromBody] CreateUserRequest request)
    {
        if (!ValidRoles.Contains(request.Role))
            return BadRequest(new { message = $"Invalid role. Valid roles: {string.Join(", ", ValidRoles)}" });

        if (await _userManager.FindByEmailAsync(request.Email) is not null)
            return Conflict(new { message = $"A user with email '{request.Email}' already exists." });

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true,
            DisplayName = request.DisplayName
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { message = string.Join("; ", result.Errors.Select(e => e.Description)) });

        await _userManager.AddToRoleAsync(user, request.Role);

        return CreatedAtAction(nameof(GetById), new { id = user.Id },
            ToResponse(user, request.Role));
    }

    // Update display name and/or role
    [HttpPut("{id}")]
    public async Task<ActionResult<UserResponse>> Update(string id, [FromBody] UpdateUserRequest request)
    {
        if (!ValidRoles.Contains(request.Role))
            return BadRequest(new { message = $"Invalid role. Valid roles: {string.Join(", ", ValidRoles)}" });

        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        user.DisplayName = request.DisplayName;
        await _userManager.UpdateAsync(user);

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (!currentRoles.Contains(request.Role))
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, request.Role);
        }

        return Ok(ToResponse(user, request.Role));
    }

    // Soft-delete via Identity lockout — preserves booking/audit history
    [HttpDelete("{id}")]
    public async Task<IActionResult> Deactivate(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (user.Id == callerId)
            return BadRequest(new { message = "You cannot deactivate your own account." });

        await _userManager.SetLockoutEnabledAsync(user, true);
        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

        return NoContent();
    }

    private static UserResponse ToResponse(ApplicationUser user, string role) => new()
    {
        Id = user.Id,
        Email = user.Email!,
        DisplayName = user.DisplayName,
        Role = role,
        IsActive = user.LockoutEnd is null || user.LockoutEnd < DateTimeOffset.UtcNow
    };
}
