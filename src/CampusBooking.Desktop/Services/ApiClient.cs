using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CampusBooking.Desktop.Models;

namespace CampusBooking.Desktop.Services;

/// <summary>
/// Thin wrapper around HttpClient that communicates with the CampusBooking REST API.
/// All methods return null / empty list on failure so callers can show a user-friendly
/// error without crashing.
/// </summary>
public class ApiClient
{
    private readonly HttpClient _http;

    /// <summary>
    /// JSON options shared by all requests: case-insensitive property matching
    /// and enum values serialised as strings (matches the API's default behaviour).
    /// </summary>
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <param name="baseUrl">Full base URL of the running API, e.g. http://localhost:5279/</param>
    public ApiClient(string baseUrl)
    {
        // Accept self-signed dev certificates so the desktop works out-of-the-box
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        _http = new HttpClient(handler) { BaseAddress = new Uri(baseUrl) };
    }

    /// <summary>Attaches the JWT bearer token to every subsequent request.</summary>
    public void SetToken(string token)
        => _http.DefaultRequestHeaders.Authorization =
               new AuthenticationHeaderValue("Bearer", token);

    // ── Auth ────────────────────────────────────────────────────────────────

    /// <summary>Calls POST /api/auth/login. Returns null on invalid credentials.</summary>
    public async Task<LoginResponse?> LoginAsync(string email, string password)
    {
        var res = await _http.PostAsJsonAsync("api/auth/login", new { email, password });
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<LoginResponse>(JsonOpts);
    }

    // ── Facility Types ───────────────────────────────────────────────────────

    /// <summary>Returns all facility types (e.g. Lab, Classroom).</summary>
    public async Task<List<FacilityTypeDto>> GetFacilityTypesAsync()
    {
        var result = await _http.GetFromJsonAsync<List<FacilityTypeDto>>("api/facility-types", JsonOpts);
        return result ?? [];
    }

    // ── Facilities ───────────────────────────────────────────────────────────

    /// <summary>
    /// Returns facilities. Managers can pass includeInactive = true to see
    /// deactivated facilities as well.
    /// </summary>
    public async Task<List<FacilityDto>> GetFacilitiesAsync(bool includeInactive = false)
    {
        var result = await _http.GetFromJsonAsync<List<FacilityDto>>(
            $"api/facilities?includeInactive={includeInactive}", JsonOpts);
        return result ?? [];
    }

    /// <summary>Creates a new facility. Returns true on success.</summary>
    public async Task<bool> CreateFacilityAsync(string name, int facilityTypeId, int capacity, string location)
    {
        var res = await _http.PostAsJsonAsync("api/facilities",
            new { name, facilityTypeId, capacity, location });
        return res.IsSuccessStatusCode;
    }

    /// <summary>Soft-deletes (deactivates) a facility. Returns true on success.</summary>
    public async Task<bool> DeactivateFacilityAsync(int id)
    {
        var res = await _http.DeleteAsync($"api/facilities/{id}");
        return res.IsSuccessStatusCode;
    }

    // ── Bookings ─────────────────────────────────────────────────────────────

    /// <summary>Returns the logged-in user's own bookings (GET /api/bookings/mine).</summary>
    public async Task<List<BookingDto>> GetMyBookingsAsync()
    {
        var result = await _http.GetFromJsonAsync<List<BookingDto>>("api/bookings/mine", JsonOpts);
        return result ?? [];
    }

    /// <summary>
    /// Returns all bookings visible to a FacilityManager (GET /api/bookings).
    /// Supports optional filtering by facility and date.
    /// </summary>
    public async Task<List<BookingDto>> GetAllBookingsAsync(int? facilityId = null, DateOnly? date = null)
    {
        var qs = new List<string>();
        if (facilityId.HasValue) qs.Add($"facilityId={facilityId}");
        if (date.HasValue)       qs.Add($"date={date.Value:yyyy-MM-dd}");
        var url = "api/bookings" + (qs.Count > 0 ? "?" + string.Join("&", qs) : "");

        var result = await _http.GetFromJsonAsync<List<BookingDto>>(url, JsonOpts);
        return result ?? [];
    }

    /// <summary>Returns all bookings awaiting manager approval.</summary>
    public async Task<List<BookingDto>> GetPendingBookingsAsync()
    {
        var result = await _http.GetFromJsonAsync<List<BookingDto>>("api/bookings/pending", JsonOpts);
        return result ?? [];
    }

    /// <summary>
    /// Searches for facilities available on a specific date and time slot.
    /// Used to populate the Create Booking dialog.
    /// </summary>
    public async Task<List<FacilityDto>> GetAvailabilityAsync(DateOnly date, int timeSlot, int? facilityTypeId = null)
    {
        var url = $"api/bookings/availability?date={date:yyyy-MM-dd}&timeSlot={timeSlot}";
        if (facilityTypeId.HasValue) url += $"&facilityTypeId={facilityTypeId}";

        var result = await _http.GetFromJsonAsync<List<FacilityDto>>(url, JsonOpts);
        return result ?? [];
    }

    /// <summary>Creates a booking for the given facility, date and time slots.</summary>
    public async Task<(bool ok, string error)> CreateBookingAsync(int facilityId, DateOnly date, int[] timeSlots)
    {
        var res = await _http.PostAsJsonAsync("api/bookings",
            new { facilityId, date, timeSlots });
        if (res.IsSuccessStatusCode) return (true, string.Empty);
        var body = await res.Content.ReadAsStringAsync();
        return (false, body);
    }

    /// <summary>Cancels a booking. Returns true on success.</summary>
    public async Task<bool> CancelBookingAsync(int id)
        => (await _http.DeleteAsync($"api/bookings/{id}")).IsSuccessStatusCode;

    /// <summary>Approves a pending booking (manager only).</summary>
    public async Task<bool> ApproveBookingAsync(int id)
        => (await _http.PutAsync($"api/bookings/{id}/approve", null)).IsSuccessStatusCode;

    /// <summary>Rejects a pending booking (manager only).</summary>
    public async Task<bool> RejectBookingAsync(int id)
        => (await _http.PutAsync($"api/bookings/{id}/reject", null)).IsSuccessStatusCode;

    // ── Notifications ────────────────────────────────────────────────────────

    /// <summary>Returns the current user's notification inbox.</summary>
    public async Task<List<NotificationDto>> GetNotificationsAsync(bool unreadOnly = false)
    {
        var result = await _http.GetFromJsonAsync<List<NotificationDto>>(
            $"api/notifications?unreadOnly={unreadOnly}", JsonOpts);
        return result ?? [];
    }

    /// <summary>Marks all unread notifications as read.</summary>
    public async Task MarkAllReadAsync()
        => await _http.PutAsync("api/notifications/read-all", null);

    // ── Maintenance ──────────────────────────────────────────────────────────

    /// <summary>Returns maintenance issues visible to the current user.</summary>
    public async Task<List<MaintenanceIssueDto>> GetMaintenanceIssuesAsync()
    {
        var result = await _http.GetFromJsonAsync<List<MaintenanceIssueDto>>("api/maintenance", JsonOpts);
        return result ?? [];
    }
}
