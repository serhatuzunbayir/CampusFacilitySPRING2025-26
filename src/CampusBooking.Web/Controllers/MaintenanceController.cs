using Microsoft.AspNetCore.Mvc;
using CampusBooking.Web.Models;
using System.Text.Json;

namespace CampusBooking.Web.Controllers{
    public class MaintenanceController : Controller{
        private readonly HttpClient _httpClient;

        public MaintenanceController(){
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

            return JsonSerializer.Deserialize<List<Facility>>(
                json,
                new JsonSerializerOptions{
                    PropertyNameCaseInsensitive = true
                }) ?? new List<Facility>();
        }

        [HttpGet]
        public async Task<IActionResult> Create(){
            var model = new MaintenanceCreateViewModel();
            model.Facilities = await GetFacilitiesAsync();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(MaintenanceCreateViewModel model){
            var token = HttpContext.Session.GetString("JWToken");

            if (string.IsNullOrEmpty(token)){
                return RedirectToAction("Login", "Auth");
            }

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var form = new MultipartFormDataContent();

            form.Add(new StringContent(model.FacilityId.ToString()), "facilityId");
            form.Add(new StringContent(model.Description), "description");

            var response = await _httpClient.PostAsync(
                "http://localhost:5279/api/maintenance",
                form
            );

            if (response.IsSuccessStatusCode){
                model.Message = "Maintenance issue created successfully.";
            }else{
                var error = await response.Content.ReadAsStringAsync();
                model.Message = "Maintenance issue failed: " + response.StatusCode + " " + error;
            }

            model.Facilities = await GetFacilitiesAsync();

            return View(model);
        }
    }
}