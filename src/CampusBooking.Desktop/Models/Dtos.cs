namespace CampusBooking.Desktop.Models;

// ---------------------------------------------------------------------------
// Simple record types used only for deserialising API responses.
// They mirror the API's response DTOs but live in the Desktop project so
// the Desktop does not need a direct reference to the API project.
// ---------------------------------------------------------------------------

/// <summary>Response body returned by POST /api/auth/login.</summary>
public record LoginResponse(
    string Token,
    DateTime ExpiresAtUtc,
    string UserId,
    string DisplayName,
    string Role);

/// <summary>One facility type (e.g. Lab, Classroom).</summary>
public record FacilityTypeDto(
    int Id,
    string Name,
    bool RequiresApproval,
    bool IsActive);

/// <summary>One bookable facility (room / lab / space).</summary>
public record FacilityDto(
    int Id,
    string Name,
    int FacilityTypeId,
    string FacilityTypeName,
    bool RequiresApproval,
    int Capacity,
    string Location,
    bool IsActive);

/// <summary>One reservation record.</summary>
public record BookingDto(
    int Id,
    int FacilityId,
    string FacilityName,
    string UserId,
    string UserDisplayName,
    DateOnly Date,
    int TimeSlot,
    string Status,
    DateTime CreatedAtUtc);

/// <summary>One notification inbox entry.</summary>
public record NotificationDto(
    int Id,
    string Kind,
    string Message,
    bool IsRead,
    DateTime CreatedAtUtc);

/// <summary>One maintenance issue.</summary>
public record MaintenanceIssueDto(
    int Id,
    int FacilityId,
    string FacilityName,
    string ReporterId,
    string ReporterName,
    string Description,
    string Severity,
    string Status,
    DateTime CreatedAtUtc);
