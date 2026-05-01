using CampusBooking.Api.Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CampusBooking.Api.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<FacilityType> FacilityTypes => Set<FacilityType>();
    public DbSet<Facility> Facilities => Set<Facility>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<MaintenanceIssue> MaintenanceIssues => Set<MaintenanceIssue>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<FacilityType>(e =>
        {
            e.HasIndex(x => x.Name).IsUnique();
        });

        builder.Entity<Facility>(e =>
        {
            e.HasOne(x => x.FacilityType)
                .WithMany(t => t.Facilities)
                .HasForeignKey(x => x.FacilityTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => x.Name);
        });

        builder.Entity<Booking>(e =>
        {
            e.HasOne(x => x.Facility)
                .WithMany(f => f.Bookings)
                .HasForeignKey(x => x.FacilityId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // NFR3: prevent double-booking the same facility/date/slot.
            // Cancelled and Rejected bookings are hard-deleted so the slot is freed;
            // history is retained in AuditLogs.
            e.HasIndex(x => new { x.FacilityId, x.Date, x.TimeSlot }).IsUnique();
        });

        builder.Entity<MaintenanceIssue>(e =>
        {
            e.HasOne(x => x.Facility)
                .WithMany(f => f.MaintenanceIssues)
                .HasForeignKey(x => x.FacilityId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Reporter)
                .WithMany()
                .HasForeignKey(x => x.ReporterId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Assignee)
                .WithMany()
                .HasForeignKey(x => x.AssigneeId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.AssignedBy)
                .WithMany()
                .HasForeignKey(x => x.AssignedById)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.AssigneeId);
        });

        builder.Entity<AuditLog>(e =>
        {
            e.HasOne(x => x.ActorUser)
                .WithMany()
                .HasForeignKey(x => x.ActorUserId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new { x.EntityType, x.EntityId });
            e.HasIndex(x => x.TimestampUtc);
        });
    }
}
