namespace CampusBooking.Desktop.Services;

/// <summary>
/// Holds the currently logged-in user's session data in memory.
/// Populated after a successful login and read by all forms.
/// </summary>
public static class SessionState
{
    public static string Token       { get; set; } = string.Empty;
    public static string UserId      { get; set; } = string.Empty;
    public static string DisplayName { get; set; } = string.Empty;
    public static string Role        { get; set; } = string.Empty;

    /// <summary>True when the logged-in user has the FacilityManager role.</summary>
    public static bool IsManager => Role == "FacilityManager";
}
