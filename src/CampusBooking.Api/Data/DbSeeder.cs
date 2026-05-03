using CampusBooking.Api.Data.Entities;
using CampusBooking.Shared;
using CampusBooking.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CampusBooking.Api.Data;

public static class DbSeeder
{
    private const string AdminEmail = "admin@campus.local";
    private const string AdminPassword = "Admin!23";
    private const string TestPassword = "Pass!23";

    public static async Task SeedAsync(IServiceProvider services, IConfiguration config, bool testMode = false)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var db = services.GetRequiredService<AppDbContext>();

        await WipeApplicationRowsAsync(db, userManager);
        await EnsureRolesAsync(roleManager);
        await EnsureAdminAsync(userManager);
        await EnsureFacilityTypesAsync(db);

        // Base seed (roles, admin, facility types) is enough to log in and run smoke tests.
        if (!testMode) return;

        await SeedTestDataAsync(db, userManager);
    }

    private static async Task WipeApplicationRowsAsync(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        // Delete children before parents so foreign keys do not block the wipe.
        db.Notifications.RemoveRange(db.Notifications);
        db.AuditLogs.RemoveRange(db.AuditLogs);
        db.MaintenanceIssues.RemoveRange(db.MaintenanceIssues);
        db.Bookings.RemoveRange(db.Bookings);
        db.Facilities.RemoveRange(db.Facilities);
        db.FacilityTypes.RemoveRange(db.FacilityTypes);
        await db.SaveChangesAsync();

        // Keep the admin so a re-seed never locks an operator out.
        var nonAdmins = await db.Users
            .Where(u => u.Email != AdminEmail)
            .ToListAsync();

        foreach (var user in nonAdmins)
        {
            await userManager.DeleteAsync(user);
        }
    }

    private static async Task EnsureRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in Enum.GetNames<UserRole>())
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    private static async Task EnsureAdminAsync(UserManager<ApplicationUser> userManager)
    {
        var admin = await userManager.FindByEmailAsync(AdminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = AdminEmail,
                Email = AdminEmail,
                EmailConfirmed = true,
                DisplayName = "Admin"
            };

            var result = await userManager.CreateAsync(admin, AdminPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create admin user: {errors}");
            }
        }
        else if (admin.DisplayName != "Admin")
        {
            admin.DisplayName = "Admin";
            await userManager.UpdateAsync(admin);
        }

        if (!await userManager.IsInRoleAsync(admin, nameof(UserRole.FacilityManager)))
            await userManager.AddToRoleAsync(admin, nameof(UserRole.FacilityManager));
    }

    private static async Task EnsureFacilityTypesAsync(AppDbContext db)
    {
        if (await db.FacilityTypes.AnyAsync()) return;

        db.FacilityTypes.AddRange(
            new FacilityType { Name = "Lab", RequiresApproval = true },
            new FacilityType { Name = "Classroom", RequiresApproval = false },
            new FacilityType { Name = "Meeting Room", RequiresApproval = false }
        );
        await db.SaveChangesAsync();
    }

    private static async Task SeedTestDataAsync(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        var student1 = await CreateTestUserAsync(userManager, "student1@campus.local", "Selin Demir", UserRole.Student);
        var staff1 = await CreateTestUserAsync(userManager, "staff1@campus.local", "Ahmet Yilmaz", UserRole.Staff);
        var manager1 = await CreateTestUserAsync(userManager, "manager1@campus.local", "Aral Kaya", UserRole.FacilityManager);
        var mp1 = await CreateTestUserAsync(userManager, "mp1@campus.local", "Arda Sahin", UserRole.MaintenancePersonnel);

        var lab = await db.FacilityTypes.FirstAsync(t => t.Name == "Lab");
        var classroom = await db.FacilityTypes.FirstAsync(t => t.Name == "Classroom");
        var meeting = await db.FacilityTypes.FirstAsync(t => t.Name == "Meeting Room");

        var l101 = new Facility { Name = "Lab L-101", FacilityTypeId = lab.Id, Capacity = 24, Location = "Engineering Building / 1st floor" };
        var l102 = new Facility { Name = "Lab L-102", FacilityTypeId = lab.Id, Capacity = 24, Location = "Engineering Building / 1st floor" };
        var c105 = new Facility { Name = "Classroom C-105", FacilityTypeId = classroom.Id, Capacity = 40, Location = "Main Building / 1st floor" };
        var c201 = new Facility { Name = "Classroom C-201", FacilityTypeId = classroom.Id, Capacity = 40, Location = "Main Building / 2nd floor" };
        var b204 = new Facility { Name = "Meeting Room B-204", FacilityTypeId = meeting.Id, Capacity = 8, Location = "Business Building / 2nd floor" };
        var b205 = new Facility { Name = "Meeting Room B-205", FacilityTypeId = meeting.Id, Capacity = 8, Location = "Business Building / 2nd floor" };
        db.Facilities.AddRange(l101, l102, c105, c201, b204, b205);
        await db.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var nowUtc = DateTime.UtcNow;

        var confirmed = new Booking
        {
            FacilityId = b204.Id,
            UserId = student1.Id,
            Date = today.AddDays(2),
            TimeSlot = 14,
            Status = BookingStatus.Confirmed,
            CreatedAtUtc = nowUtc.AddHours(-3)
        };

        var approved = new Booking
        {
            FacilityId = l101.Id,
            UserId = staff1.Id,
            Date = today.AddDays(3),
            TimeSlot = 10,
            Status = BookingStatus.Approved,
            CreatedAtUtc = nowUtc.AddHours(-6)
        };

        var pending = new Booking
        {
            FacilityId = l102.Id,
            UserId = staff1.Id,
            Date = today.AddDays(5),
            TimeSlot = 13,
            Status = BookingStatus.Pending,
            CreatedAtUtc = nowUtc.AddHours(-1)
        };

        var cancelled = new Booking
        {
            FacilityId = c105.Id,
            UserId = student1.Id,
            Date = today.AddDays(1),
            TimeSlot = 9,
            Status = BookingStatus.Cancelled,
            CreatedAtUtc = nowUtc.AddHours(-12)
        };

        db.Bookings.AddRange(confirmed, approved, pending, cancelled);
        await db.SaveChangesAsync();

        db.AuditLogs.AddRange(
            new AuditLog
            {
                ActorUserId = student1.Id,
                Action = "Create",
                EntityType = nameof(Booking),
                EntityId = confirmed.Id.ToString(),
                TimestampUtc = confirmed.CreatedAtUtc,
                Details = $"Auto-confirmed booking on {b204.Name} at slot {confirmed.TimeSlot}."
            },
            new AuditLog
            {
                ActorUserId = staff1.Id,
                Action = "Create",
                EntityType = nameof(Booking),
                EntityId = approved.Id.ToString(),
                TimestampUtc = approved.CreatedAtUtc,
                Details = $"Pending lab booking submitted for {l101.Name}."
            },
            new AuditLog
            {
                ActorUserId = manager1.Id,
                Action = "Approve",
                EntityType = nameof(Booking),
                EntityId = approved.Id.ToString(),
                TimestampUtc = nowUtc.AddHours(-2),
                Details = $"Manager approved lab booking on {l101.Name}."
            },
            new AuditLog
            {
                ActorUserId = staff1.Id,
                Action = "Create",
                EntityType = nameof(Booking),
                EntityId = pending.Id.ToString(),
                TimestampUtc = pending.CreatedAtUtc,
                Details = $"Pending lab booking submitted for {l102.Name}."
            },
            new AuditLog
            {
                ActorUserId = student1.Id,
                Action = "Cancel",
                EntityType = nameof(Booking),
                EntityId = cancelled.Id.ToString(),
                TimestampUtc = nowUtc.AddHours(-1),
                Details = $"Owner cancelled booking on {c105.Name}."
            }
        );

        var issue = new MaintenanceIssue
        {
            FacilityId = c105.Id,
            ReporterId = staff1.Id,
            Description = "Projector lens cracked, image flickers",
            Severity = IssueSeverity.High,
            Status = MaintenanceStatus.Open,
            CreatedAtUtc = nowUtc.AddHours(-4)
        };
        db.MaintenanceIssues.Add(issue);
        await db.SaveChangesAsync();

        db.Notifications.AddRange(
            new Notification
            {
                RecipientUserId = student1.Id,
                Kind = NotificationKind.BookingConfirmed,
                Message = $"Your booking on {b204.Name} is confirmed.",
                CreatedAtUtc = confirmed.CreatedAtUtc,
                IsRead = false
            },
            new Notification
            {
                RecipientUserId = student1.Id,
                Kind = NotificationKind.BookingCancelled,
                Message = $"Your booking on {c105.Name} was cancelled.",
                CreatedAtUtc = nowUtc.AddHours(-1),
                IsRead = false
            },
            new Notification
            {
                RecipientUserId = manager1.Id,
                Kind = NotificationKind.BookingApproved,
                Message = $"Lab booking on {l101.Name} approved.",
                CreatedAtUtc = nowUtc.AddHours(-2),
                IsRead = true
            },
            new Notification
            {
                RecipientUserId = manager1.Id,
                Kind = NotificationKind.MaintenanceStatusChanged,
                Message = $"New high-severity issue reported on {c105.Name}.",
                CreatedAtUtc = issue.CreatedAtUtc,
                IsRead = false
            }
        );
        await db.SaveChangesAsync();
    }

    private static async Task<ApplicationUser> CreateTestUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string displayName,
        UserRole role)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = displayName
        };

        var result = await userManager.CreateAsync(user, TestPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create test user {email}: {errors}");
        }

        await userManager.AddToRoleAsync(user, role.ToString());
        return user;
    }
}
