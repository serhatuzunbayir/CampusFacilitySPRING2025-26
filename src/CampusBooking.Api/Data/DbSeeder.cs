using CampusBooking.Api.Data.Entities;
using CampusBooking.Shared.Enums;
using Microsoft.AspNetCore.Identity;

namespace CampusBooking.Api.Data;

/// <summary>
/// Runs once on application startup to ensure the database has the required
/// reference data (roles and an initial admin account).
/// FR1: no public self-registration — the first FacilityManager is created here.
/// Seed credentials are read from configuration so they can be overridden per environment.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        // Create the four application roles if they don't already exist
        foreach (var role in Enum.GetNames<UserRole>())
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // Read initial admin credentials from appsettings (overridable via env vars in production)
        var adminEmail = config["Seed:AdminEmail"] ?? "admin@campus.local";
        var adminPassword = config["Seed:AdminPassword"] ?? "Admin!123";
        var adminDisplayName = config["Seed:AdminDisplayName"] ?? "Initial Facility Manager";

        // Only create the admin if it doesn't exist yet (idempotent seed)
        var existing = await userManager.FindByEmailAsync(adminEmail);
        if (existing is null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                DisplayName = adminDisplayName
            };

            var result = await userManager.CreateAsync(admin, adminPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to seed admin user: {errors}");
            }

            await userManager.AddToRoleAsync(admin, nameof(UserRole.FacilityManager));
        }
    }
}
