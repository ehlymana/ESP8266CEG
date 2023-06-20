using ESPClient.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ESPClient.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        Microcontroller microcontroller = new Microcontroller();

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(microcontroller);
        }

        [HttpPost]
        public async Task<IActionResult> Index(Microcontroller newConfiguration)
        {
            return View();
            /*
            // new IP address is entered
            if (microcontroller.IPAddress != newConfiguration.IPAddress && newConfiguration.IPAddress.Length > 0)
            {
                // attempt to connect to ESP
                HttpClient client = new HttpClient();
                var responseString = await client.GetStringAsync("http://" + newConfiguration.IPAddress);
                responseString = "";
            }
            else
            {
                microcontroller.IPAddress = "";
                microcontroller.ConnectionStatus = "Disconnected";
            }

            // user changed the time of the day manually (or automatic change????)
            if (microcontroller.ConnectionStatus == "Connected" && microcontroller.Time != newConfiguration.Time)
            {
                // send request to ESP to change the time

                microcontroller.Time = newConfiguration.Time;
            }

            // user changed the mode manually
            if (microcontroller.ConnectionStatus == "Connected" && microcontroller.Mode != newConfiguration.Mode)
            {
                // send request to ESP to change the mode

                microcontroller.Mode = newConfiguration.Mode;
            }

            return View(microcontroller);*/
        }

        [HttpPost]
        public ActionResult Connect(string IPAddress)
        {
            return View();
        }

        [HttpPost]
        public ActionResult ModeRegime(string mode, string regime)
        {
            return View();
        }
    }
}