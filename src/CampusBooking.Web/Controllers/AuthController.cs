using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace CampusBooking.Web.Controllers{
    public class AuthController : Controller{
        private readonly HttpClient _httpClient;

        public AuthController(){
            _httpClient = new HttpClient();
        }

        [HttpGet]
        public IActionResult Login(){
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password){
            var loginData = new{
                email = email,
                password = password
            };

            var json = JsonSerializer.Serialize(loginData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                "http://localhost:5279/api/Auth/login",
                content
            );

            if (response.IsSuccessStatusCode){
                var jsonResponse = await response.Content.ReadAsStringAsync();

                using var document = JsonDocument.Parse(jsonResponse);
                var token = document.RootElement.GetProperty("token").GetString();

                HttpContext.Session.SetString("JWToken", token!);

                return RedirectToAction("Index", "Home");
            }

            var error = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError("", "Invalid email or password");
            
            return View();
        }
    }
}