using Microsoft.AspNetCore.Mvc;
using CampusBooking.Web.Models;
using System.Text;
using System.Text.Json;

namespace CampusBooking.Web.Controllers{
    public class BookingController : Controller{
        private readonly HttpClient _httpClient;

        public BookingController(){
            _httpClient = new HttpClient();
        }

        private async Task<List<Facility>> GetFacilitiesAsync(){
            var token = HttpContext.Session.GetString("JWToken");

            if (string.IsNullOrEmpty(token)){
                return new List<Facility>();
            }

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync("http://localhost:5279/api/facilities");

            if (!response.IsSuccessStatusCode){
                return new List<Facility>();
            }

            var json = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<List<Facility>>(json,
                new JsonSerializerOptions{
                    PropertyNameCaseInsensitive = true
                }) ?? new List<Facility>();
        }

        [HttpGet]
        public async Task<IActionResult> Create(){
            var token = HttpContext.Session.GetString("JWToken");

            if (string.IsNullOrEmpty(token)){
                return RedirectToAction("Login", "Auth");
            }

            var model = new BookingCreateViewModel();
            model.Facilities = await GetFacilitiesAsync();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(BookingCreateViewModel model){
            var token = HttpContext.Session.GetString("JWToken");

            if (string.IsNullOrEmpty(token)){
                return RedirectToAction("Login", "Auth");
            }

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var requestBody = new{
                facilityId = model.FacilityId,
                date = model.Date,
                timeSlots = new[] { model.TimeSlot }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                "http://localhost:5279/api/bookings",
                content
            );

            if (response.IsSuccessStatusCode){
                model.Message = "Booking created successfully.";
            }else{
                var error = await response.Content.ReadAsStringAsync();
                model.Message = "Booking failed: " + response.StatusCode + " " + error;
            }

            model.Facilities = await GetFacilitiesAsync();

            return View(model);
        }
    }
}