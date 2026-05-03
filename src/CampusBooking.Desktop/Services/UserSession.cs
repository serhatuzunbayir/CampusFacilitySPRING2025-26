namespace CampusBooking.Desktop.Services;

public class UserSession
{
    public string Token { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    public bool IsManager => Role == "FacilityManager";
    public bool IsLoggedIn => !string.IsNullOrEmpty(Token);

    public void Clear()
    {
        Token = string.Empty;
        UserId = string.Empty;
        DisplayName = string.Empty;
        Role = string.Empty;
    }
}
