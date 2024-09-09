using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using WeatherApp.Models;
using System.Security.Claims;
using System.Threading.Tasks;

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
                await azureService.RegisterUserAsync(model.Email, model.Username, model.Password);
                return RedirectToAction("Login", "Account");
            }

            return View(model);
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
                    return RedirectToAction("Index", "Home");
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
