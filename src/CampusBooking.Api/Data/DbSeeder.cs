using CampusBooking.Api.Data.Entities;
using CampusBooking.Shared.Enums;
using Microsoft.AspNetCore.Identity;

namespace CampusBooking.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in Enum.GetNames<UserRole>())
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        var adminEmail = config["Seed:AdminEmail"] ?? "admin@campus.local";
        var adminPassword = config["Seed:AdminPassword"] ?? "Admin!123";
        var adminDisplayName = config["Seed:AdminDisplayName"] ?? "Initial Facility Manager";

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
