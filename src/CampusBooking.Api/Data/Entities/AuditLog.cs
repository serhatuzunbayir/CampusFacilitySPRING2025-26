using System.ComponentModel.DataAnnotations;

namespace CampusBooking.Api.Data.Entities;

public class AuditLog
{
    public long Id { get; set; }

    public string ActorUserId { get; set; } = string.Empty;
    public ApplicationUser? ActorUser { get; set; }

    [Required, MaxLength(64)]
    public string Action { get; set; } = string.Empty;

    [Required, MaxLength(64)]
    public string EntityType { get; set; } = string.Empty;

    [MaxLength(64)]
    public string? EntityId { get; set; }

    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    [MaxLength(2000)]
    public string? Details { get; set; }
}
