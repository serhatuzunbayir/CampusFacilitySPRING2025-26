using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CampusBooking.Api.Data.Entities;
using Microsoft.IdentityModel.Tokens;

namespace CampusBooking.Api.Services;

/// <summary>
/// Generates signed JWT bearer tokens used by the Web and Desktop clients to
/// authenticate against the API (NFR2). The token embeds the user's ID, email,
/// display name, and role(s) so controllers can authorise without an extra DB call.
/// </summary>
public class TokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config) => _config = config;

    /// <summary>
    /// Builds and signs a JWT for the given user.
    /// </summary>
    /// <returns>The serialised token string and its UTC expiry time.</returns>
    public (string token, DateTime expiresAt) CreateToken(ApplicationUser user, IList<string> roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiryMinutes = _config.GetValue<int>("Jwt:ExpiryMinutes", 480);
        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        // Core identity claims included in every token
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("displayName", user.DisplayName)
        };

        // Add one role claim per role so [Authorize(Roles = "...")] works correctly
        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
