using Microsoft.AspNetCore.Mvc;
using CampusBooking.Web.Models;
using System.Text.Json;

namespace CampusBooking.Web.Controllers{
    public class NotificationController : Controller{
        private readonly HttpClient _httpClient;

        public NotificationController(){
            _httpClient = new HttpClient();
        }

        public async Task<IActionResult> Index(){
            var token = HttpContext.Session.GetString("JWToken");

            if (string.IsNullOrEmpty(token)){
                return RedirectToAction("Login", "Auth");
            }

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer",
                    token
                );

            var response = await _httpClient.GetAsync(
                "http://localhost:5279/api/notifications/unread"
            );

            if (!response.IsSuccessStatusCode){
                return View(new List<NotificationViewModel>());
            }

            var json = await response.Content.ReadAsStringAsync();

            var notifications =
                JsonSerializer.Deserialize<List<NotificationViewModel>>(
                    json,
                    new JsonSerializerOptions{
                        PropertyNameCaseInsensitive = true
                    });

            return View(notifications ?? new List<NotificationViewModel>());
        }
    }
}