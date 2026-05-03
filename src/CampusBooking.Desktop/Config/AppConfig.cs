namespace CampusBooking.Desktop.Config;

public class ApiOptions
{
    public string BaseUrl { get; set; } = "";
    public int PollingIntervalSeconds { get; set; } = 15;
}
