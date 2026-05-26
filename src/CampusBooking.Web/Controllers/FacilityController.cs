using Microsoft.AspNetCore.Mvc;
using CampusBooking.Web.Models;
using System.Text.Json;

namespace CampusBooking.Web.Controllers{
    public class FacilityController : Controller{
        private readonly HttpClient _httpClient;

        public FacilityController(){
            _httpClient = new HttpClient();
        }

        public async Task<IActionResult> Index(){
            var token = HttpContext.Session.GetString("JWToken");

            if (string.IsNullOrEmpty(token)){
                return RedirectToAction("Login", "Auth");
            }

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync("http://localhost:5279/api/facilities");

            if (!response.IsSuccessStatusCode){
                return View(new List<Facility>());
            }

            var json = await response.Content.ReadAsStringAsync();

            var facilities = JsonSerializer.Deserialize<List<Facility>>(json,
                new JsonSerializerOptions{
                    PropertyNameCaseInsensitive = true
                });

            return View(facilities ?? new List<Facility>());
        }
    }
}