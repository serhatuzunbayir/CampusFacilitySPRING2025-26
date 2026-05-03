using CampusBooking.Api.Data.Entities;
using CampusBooking.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CampusBooking.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config, bool testMode = false)
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

        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
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

        if (!testMode) return;

        var db = services.GetRequiredService<AppDbContext>();

        db.Bookings.RemoveRange(db.Bookings);
        db.MaintenanceIssues.RemoveRange(db.MaintenanceIssues);
        db.Facilities.RemoveRange(db.Facilities);
        db.FacilityTypes.RemoveRange(db.FacilityTypes);
        await db.SaveChangesAsync();

        var labType = new FacilityType { Name = "Lab", RequiresApproval = true };
        var classroomType = new FacilityType { Name = "Classroom", RequiresApproval = false };
        var meetingType = new FacilityType { Name = "Meeting Room", RequiresApproval = false };
        db.FacilityTypes.AddRange(labType, classroomType, meetingType);
        await db.SaveChangesAsync();

        var lab1 = new Facility { Name = "Lab A-101", FacilityTypeId = labType.Id, Capacity = 20, Location = "A Block, Floor 1" };
        var lab2 = new Facility { Name = "Lab A-102", FacilityTypeId = labType.Id, Capacity = 24, Location = "A Block, Floor 1" };
        var class1 = new Facility { Name = "Classroom B-201", FacilityTypeId = classroomType.Id, Capacity = 40, Location = "B Block, Floor 2" };
        var class2 = new Facility { Name = "Classroom B-202", FacilityTypeId = classroomType.Id, Capacity = 35, Location = "B Block, Floor 2" };
        var meeting1 = new Facility { Name = "Meeting Room C-301", FacilityTypeId = meetingType.Id, Capacity = 8, Location = "C Block, Floor 3" };
        db.Facilities.AddRange(lab1, lab2, class1, class2, meeting1);
        await db.SaveChangesAsync();

        var studentEmail = "student@campus.local";
        var student = await userManager.FindByEmailAsync(studentEmail);
        if (student is null)
        {
            student = new ApplicationUser
            {
                UserName = studentEmail,
                Email = studentEmail,
                EmailConfirmed = true,
                DisplayName = "Test Student"
            };
            await userManager.CreateAsync(student, "Student!123");
            await userManager.AddToRoleAsync(student, nameof(UserRole.Student));
        }

        var personnelEmail = "personnel@campus.local";
        var personnel = await userManager.FindByEmailAsync(personnelEmail);
        if (personnel is null)
        {
            personnel = new ApplicationUser
            {
                UserName = personnelEmail,
                Email = personnelEmail,
                EmailConfirmed = true,
                DisplayName = "Test Personnel"
            };
            await userManager.CreateAsync(personnel, "Personnel!123");
            await userManager.AddToRoleAsync(personnel, nameof(UserRole.MaintenancePersonnel));
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.Bookings.AddRange(
            new Booking { FacilityId = lab1.Id, UserId = student.Id, Date = today.AddDays(1), TimeSlot = 10, Status = BookingStatus.Pending },
            new Booking { FacilityId = lab2.Id, UserId = student.Id, Date = today.AddDays(2), TimeSlot = 14, Status = BookingStatus.Pending },
            new Booking { FacilityId = class1.Id, UserId = student.Id, Date = today.AddDays(1), TimeSlot = 9, Status = BookingStatus.Confirmed }
        );

        db.MaintenanceIssues.Add(new MaintenanceIssue
        {
            FacilityId = class2.Id,
            ReporterId = student.Id,
            Description = "Projector not working.",
            Severity = IssueSeverity.Medium,
            Status = MaintenanceStatus.Open
        });

        await db.SaveChangesAsync();
    }
}
