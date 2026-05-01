using CampusBooking.Api.Data.Entities;
using CampusBooking.Api.Dtos.Auth;
using CampusBooking.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CampusBooking.Api.Controllers;

/// <summary>
/// Handles authentication for both the Web and Desktop clients.
/// All other API endpoints require the JWT bearer token returned by this controller (NFR2).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly TokenService _tokenService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        TokenService tokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
    }

    /// <summary>
    /// Validates credentials and returns a signed JWT.
    /// Returns 401 for unknown email or wrong password (same message to prevent user enumeration).
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Unauthorized(new { message = "Invalid credentials." });

        // CheckPasswordSignInAsync validates the password without issuing a cookie
        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
            return Unauthorized(new { message = "Invalid credentials." });

        var roles = await _userManager.GetRolesAsync(user);
        var (token, expiresAt) = _tokenService.CreateToken(user, roles);

        return Ok(new LoginResponse
        {
            Token = token,
            ExpiresAtUtc = expiresAt,
            UserId = user.Id,
            DisplayName = user.DisplayName,
            Role = roles.FirstOrDefault() ?? string.Empty
        });
    }
}
