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

            // If the 'days' parameter is provided, filter the data for the given date range
            if (days > 0)
            {
                var endDate = DateTime.UtcNow;
                var startDate = endDate.AddDays(-days);
                data = data.Where(d => d.CreationTime >= startDate && d.CreationTime <= endDate).ToList();
            }

            // Calculate the count of records with temperature >= 120
            ViewBag.HighTempCount = data.Count(d => d.Temperature >= 120);

            bool playSound = false;

            // If data is available, filter for records with high temperature (>= 120)
            if (ViewBag.HighTempCount != null)
            {
                data = data.Where(x => x.Temperature >= 120).ToList();
                playSound = data.Count != 0;  // Set flag if there are any high-temperature records
            }

            // Calculate the count of records with temperature > 120
            ViewBag.HighTempCount = data.Count;

            // Pass the flag to the view for playing the sound
            ViewBag.PlaySound = playSound;


            // Return the data to the view for rendering
            return View(data);
        }

        // GetFilteredData Action: Filters weather data based on the number of days
        public IActionResult GetFilteredData(int days)
        {
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-days);

            // Fetch and filter the weather data using AzureService
            var azureService = new AzureService();
            var data = azureService.GetWeatherData();
            var filteredData = data
                .Where(d => d.CreationTime >= startDate && d.CreationTime <= endDate)
                .ToList();

            // Calculate the count of records with temperature >= 120
            ViewBag.HighTempCount = filteredData.Count(d => d.Temperature > 120);

            // Return the filtered data to the view
            return View("Index", filteredData);  // Return to Index view with filtered data
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

            bool playSound = false;

            // If data is available, filter for records with high temperature (>= 120)
            if (data != null)
            {
                data = data.Where(x => x.Temperature >= 120).ToList();
                playSound = data.Count != 0;  // Set flag if there are any high-temperature records
            }

            // Calculate the count of records with temperature > 120
            ViewBag.HighTempCount = data.Count;

            // Pass the flag to the view for playing the sound
            ViewBag.PlaySound = playSound;

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
