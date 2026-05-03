using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CampusBooking.Shared.Dtos.Auth;
using CampusBooking.Shared.Dtos.Bookings;
using CampusBooking.Shared.Dtos.Facilities;
using CampusBooking.Shared.Dtos.Maintenance;
using CampusBooking.Shared.Dtos.Users;

namespace CampusBooking.Desktop.Services;

public class ApiClient
{
    private readonly HttpClient _http;
    private readonly UserSession _session;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public ApiClient(HttpClient http, UserSession session)
    {
        _http = http;
        _session = session;
    }

    private void AttachToken(HttpRequestMessage req)
    {
        if (!string.IsNullOrEmpty(_session.Token))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _session.Token);
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string url, object? body = null)
    {
        using var req = new HttpRequestMessage(method, url);
        AttachToken(req);
        if (body is not null)
            req.Content = JsonContent.Create(body, options: JsonOpts);

        using var res = await _http.SendAsync(req);
        await EnsureOk(res);

        if (typeof(T) == typeof(EmptyResult))
            return (T)(object)new EmptyResult();

        var result = await res.Content.ReadFromJsonAsync<T>(JsonOpts);
        return result ?? throw new ApiException(res.StatusCode, "Empty response body.");
    }

    private async Task SendAsync(HttpMethod method, string url, object? body = null)
    {
        using var req = new HttpRequestMessage(method, url);
        AttachToken(req);
        if (body is not null)
            req.Content = JsonContent.Create(body, options: JsonOpts);

        using var res = await _http.SendAsync(req);
        await EnsureOk(res);
    }

    private static async Task EnsureOk(HttpResponseMessage res)
    {
        if (res.IsSuccessStatusCode) return;
        string message;
        try
        {
            var body = await res.Content.ReadAsStringAsync();
            message = string.IsNullOrWhiteSpace(body) ? res.ReasonPhrase ?? "Request failed." : body;
        }
        catch
        {
            message = res.ReasonPhrase ?? "Request failed.";
        }
        throw new ApiException(res.StatusCode, message);
    }

    public Task<LoginResponse> LoginAsync(LoginRequest req)
        => SendAsync<LoginResponse>(HttpMethod.Post, "api/auth/login", req);

    public Task ChangePasswordAsync(ChangePasswordRequest req)
        => SendAsync(HttpMethod.Post, "api/auth/change-password", req);

    public Task<List<FacilityTypeResponse>> GetFacilityTypesAsync()
        => SendAsync<List<FacilityTypeResponse>>(HttpMethod.Get, "api/facility-types");

    public Task<List<FacilityResponse>> GetFacilitiesAsync(bool includeInactive = false)
        => SendAsync<List<FacilityResponse>>(HttpMethod.Get, $"api/facilities?includeInactive={includeInactive}");

    public Task<FacilityResponse> CreateFacilityAsync(CreateFacilityRequest req)
        => SendAsync<FacilityResponse>(HttpMethod.Post, "api/facilities", req);

    public Task DeactivateFacilityAsync(int id)
        => SendAsync(HttpMethod.Delete, $"api/facilities/{id}");

    public Task<List<BookingResponse>> GetMyBookingsAsync()
        => SendAsync<List<BookingResponse>>(HttpMethod.Get, "api/bookings/mine");

    public Task<List<BookingResponse>> GetAllBookingsAsync(int? facilityId = null, DateOnly? date = null)
    {
        var qs = new List<string>();
        if (facilityId.HasValue) qs.Add($"facilityId={facilityId}");
        if (date.HasValue) qs.Add($"date={date.Value:yyyy-MM-dd}");
        var url = "api/bookings" + (qs.Count > 0 ? "?" + string.Join("&", qs) : "");
        return SendAsync<List<BookingResponse>>(HttpMethod.Get, url);
    }

    public Task<List<BookingResponse>> GetPendingBookingsAsync()
        => SendAsync<List<BookingResponse>>(HttpMethod.Get, "api/bookings/pending");

    public Task<List<FacilityResponse>> GetAvailabilityAsync(DateOnly date, int timeSlot, int? facilityTypeId = null)
    {
        var url = $"api/bookings/availability?date={date:yyyy-MM-dd}&timeSlot={timeSlot}";
        if (facilityTypeId.HasValue) url += $"&facilityTypeId={facilityTypeId}";
        return SendAsync<List<FacilityResponse>>(HttpMethod.Get, url);
    }

    public Task<BookingResponse> CreateBookingAsync(CreateBookingRequest req)
        => SendAsync<BookingResponse>(HttpMethod.Post, "api/bookings", req);

    public Task CancelBookingAsync(int id)
        => SendAsync(HttpMethod.Delete, $"api/bookings/{id}");

    public Task ApproveBookingAsync(int id)
        => SendAsync(HttpMethod.Put, $"api/bookings/{id}/approve");

    public Task RejectBookingAsync(int id)
        => SendAsync(HttpMethod.Put, $"api/bookings/{id}/reject");

    public Task<List<NotificationItem>> GetUnreadNotificationsAsync()
        => SendAsync<List<NotificationItem>>(HttpMethod.Get, "api/notifications/unread");

    public Task<List<NotificationItem>> GetNotificationsAsync(bool unreadOnly = false)
        => SendAsync<List<NotificationItem>>(HttpMethod.Get, $"api/notifications?unreadOnly={unreadOnly}");

    public Task MarkAllReadAsync()
        => SendAsync(HttpMethod.Put, "api/notifications/read-all");

    public Task<List<MaintenanceIssueResponse>> GetMaintenanceIssuesAsync()
        => SendAsync<List<MaintenanceIssueResponse>>(HttpMethod.Get, "api/maintenance");

    public Task<MaintenanceIssueResponse> CreateMaintenanceIssueAsync(CreateMaintenanceIssueRequest req)
        => SendAsync<MaintenanceIssueResponse>(HttpMethod.Post, "api/maintenance", req);

    public Task AssignMaintenanceAsync(int id, AssignMaintenanceRequest req)
        => SendAsync(HttpMethod.Put, $"api/maintenance/{id}/assign", req);

    public Task UpdateMaintenanceStatusAsync(int id, UpdateStatusRequest req)
        => SendAsync(HttpMethod.Put, $"api/maintenance/{id}/status", req);

    public Task<List<UserResponse>> GetUsersAsync()
        => SendAsync<List<UserResponse>>(HttpMethod.Get, "api/users");

    public Task<UserResponse> CreateUserAsync(CreateUserRequest req)
        => SendAsync<UserResponse>(HttpMethod.Post, "api/users", req);

    public Task UpdateUserAsync(string id, UpdateUserRequest req)
        => SendAsync(HttpMethod.Put, $"api/users/{id}", req);

    private sealed class EmptyResult { }
}

public class NotificationItem
{
    public int Id { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
