using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using WeatherApp.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace WeatherApp.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterAsync(RegisterViewModel model)
        {
            AzureService azureService = new AzureService();

            if (ModelState.IsValid)
            {
                // Start the registration task
                var registerTask = azureService.RegisterUserAsync(model.Email, model.Username, model.Password);

                try
                {
                    await registerTask;
                    // If no exception is thrown, continue to the next step
                    return RedirectToAction("Login", "Account");
                }
                catch (Exception ex)
                {   
                    // Handle the faulted task
                    ModelState.AddModelError("", ex.Message);  // Display the error message (e.g., user already exists)
                }
            }
            return View(model);  // Return view with validation errors if any
        }


        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            AzureService azureService = new AzureService();

            if (ModelState.IsValid)
            {
                // Use the correct method name
                if (azureService.IsValidUser(model.Username, model.Password))
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, model.Username)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
                    return RedirectToAction("Notifications", "Home");
                }
                ModelState.AddModelError("", "Invalid login attempt.");
                return View();
            }

            return View(model);
        }

        private bool ValidateLogin(string password)
        {
            return password.Length >= 8 && !password.Contains(" ");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
    }
}
