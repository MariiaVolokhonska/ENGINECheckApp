using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;
using WeatherApp.Models;
using System.Linq;

namespace WeatherApp.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        // HomeController constructor
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        // Index Action: Displays weather data, filtered by days if applicable
        public IActionResult Index(int days = 0)
        {
            // Retrieve the username from the claims (for logged-in user)
            ViewBag.Username = User.FindFirst(ClaimTypes.Name)?.Value;

            // Create an instance of AzureService and fetch the weather data
            var azureService = new AzureService();
            var data = azureService.GetWeatherData();
            
            // Count the current number of notifications (based on the threshold of Temperature >= 120)
            var currentNotificationNumber = data.Count(d => d.Temperature >= 120);

            // Retrieve the previous notification number from the session (default to 0 if not set)
            var previousNotificationNumber = HttpContext.Session.GetInt32("NotificationNumber") ?? currentNotificationNumber;

            // If the 'days' parameter is provided, filter the data for the given date range
            if (days > 0)
            {
                var endDate = DateTime.UtcNow;
                var startDate = endDate.AddDays(-days);
                data = data.Where(d => d.CreationTime >= startDate && d.CreationTime <= endDate).ToList();
            }

            // Calculate the number of new notifications
            var newNotifications = currentNotificationNumber - previousNotificationNumber;
            
            // Store the new notification count in the session
            HttpContext.Session.SetInt32("NotificationNumber", currentNotificationNumber);

            // Store the number of new notifications in ViewBag to display on the view
            Console.WriteLine(newNotifications);
            ViewBag.HighTempCount = newNotifications;

            // Return the data to the view for rendering
            return View(data);
        }

        

        // Notifications Action: Checks for high temperature and plays sound alert
        public async Task<IActionResult> Notifications() 
        {
            var azureService = new AzureService();
            var data = azureService.GetWeatherData();

            //get the result of the prediction (word "true" or "false")
            string req =  await API.InvokeRequestResponseService();

            //put result in ViewBag.Message to show it on Notification page
            ViewBag.Message = req;

            // If data is available, filter for records with high temperature (>= 120)
            if (data != null)
            {
                data = data.Where(x => x.Temperature >= 120).ToList();
            }

            // Calculate the count of records with temperature > 120
            ViewBag.HighTempCount = data.Count;

            // Return the high-temperature data to the view
            return View(data);
        }

        // Privacy Action: Displays privacy page
        public IActionResult Privacy()
        {
            return View();
        }

        // Error Action: Displays error page with the request ID
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
