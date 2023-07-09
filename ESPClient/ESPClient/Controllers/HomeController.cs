using ESPClient.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Reflection;
using System.Text;

namespace ESPClient.Controllers
{
    public class HomeController : Controller
    {
        #region Attributes

        private readonly ILogger<HomeController> _logger;

        static Microcontroller microcontroller = new Microcontroller();

        #endregion

        #region Constructor

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        #endregion

        #region Routes for Microcontroller communication

        /// <summary>
        /// Show webpage with new settings
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Index()
        {
            // perform web-request to update sensor values
            GetNewSensorValues();
            return View(microcontroller);
        }

        /// <summary>
        /// Connect to the specified IP address
        /// </summary>
        /// <param name="IPAddress"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> Connect(string IPAddress)
        {
            try
            {
                HttpClient client = new HttpClient();
                var responseString = await client.GetStringAsync("http://" + IPAddress + "/");
                if (responseString == "Connection: OK")
                    microcontroller.IPAddress = IPAddress;
                else
                    microcontroller.IPAddress = null;
            }
            catch
            {
                microcontroller.IPAddress = null;
            }
            
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Change active mode and regime
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="regime"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> ModeRegime(string mode, string regime)
        {
            if (microcontroller.IPAddress == null)
                return RedirectToAction("Index"); ;

            try
            {
                HttpClient client = new HttpClient();
                var responseString = await client.GetStringAsync("http://" + microcontroller.IPAddress + "/" + mode);
                if (responseString != "Change: OK")
                    microcontroller.IPAddress = null;

                responseString = await client.GetStringAsync("http://" + microcontroller.IPAddress + "/" + regime);
                if (responseString != "Change: OK")
                    microcontroller.IPAddress = null;
            }
            catch
            {
                microcontroller.IPAddress = null;
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Read new sensor values from the microcontroller
        /// </summary>
        public async void GetNewSensorValues()
        {
            if (microcontroller.IPAddress == null)
                return;

            try
            {

                HttpClient client = new HttpClient();
                var responseString = await client.GetStringAsync("http://" + microcontroller.IPAddress + "/sensors");
                var sensorValues = responseString.Split(",");

                microcontroller.LightSensor = sensorValues[0] == "0";
                microcontroller.MovementSensor = sensorValues[1] == "1";
                microcontroller.SoundSensor = Int32.Parse(sensorValues[2]);
                microcontroller.LEDRingStrip = Color.FromName(sensorValues[3]);
                microcontroller.SystemMode = (SystemMode)Enum.Parse(typeof(SystemMode), sensorValues[4]);
                microcontroller.SystemRegime = (SystemRegime)Enum.Parse(typeof(SystemRegime), sensorValues[5]);
            }
            catch
            {
                microcontroller.IPAddress = null;
            }
        }

        #endregion

        #region Routes for Cameras Communication

        #endregion
    }
}