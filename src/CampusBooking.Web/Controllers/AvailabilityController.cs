using Microsoft.AspNetCore.Mvc;
using CampusBooking.Web.Models;
using System.Text.Json;

namespace CampusBooking.Web.Controllers{
    public class AvailabilityController : Controller{
        private readonly HttpClient _httpClient;

        public AvailabilityController(){
            _httpClient = new HttpClient();
        }

        [HttpGet]
        public IActionResult Index(){
            return View(new AvailabilitySearchViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Index(AvailabilitySearchViewModel model){
            var token = HttpContext.Session.GetString("JWToken");

            if (string.IsNullOrEmpty(token)){
                return RedirectToAction("Login", "Auth");
            }

            _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var url = $"http://localhost:5279/api/bookings/availability?Date={model.Date}&TimeSlot={model.TimeSlot}";

            if (model.FacilityTypeId.HasValue)
                url += $"&FacilityTypeId={model.FacilityTypeId.Value}";

            if (model.MinCapacity.HasValue)
                url += $"&MinCapacity={model.MinCapacity.Value}";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode){
                ViewBag.Error = "Availability search failed.";
                return View(model);
            }

            var json = await response.Content.ReadAsStringAsync();

            model.Results = JsonSerializer.Deserialize<List<Facility>>(json,
                new JsonSerializerOptions{
                    PropertyNameCaseInsensitive = true
                }) ?? new List<Facility>();

            return View(model);
        }
    }
}