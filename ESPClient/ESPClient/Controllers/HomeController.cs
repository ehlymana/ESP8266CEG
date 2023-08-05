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

        static Subsystems subsystems = new Subsystems();

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
            return View(subsystems);
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
                    subsystems.Microcontroller.IPAddress = IPAddress;
                else
                    subsystems.Microcontroller.IPAddress = null;
            }
            catch
            {
                subsystems.Microcontroller.IPAddress = null;
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
            if (subsystems.Microcontroller.IPAddress == null)
                return RedirectToAction("Index"); ;

            try
            {
                HttpClient client = new HttpClient();
                var responseString = await client.GetStringAsync("http://" + subsystems.Microcontroller.IPAddress + "/" + mode);
                if (responseString != "Change: OK")
                    subsystems.Microcontroller.IPAddress = null;

                responseString = await client.GetStringAsync("http://" + subsystems.Microcontroller.IPAddress + "/" + regime);
                if (responseString != "Change: OK")
                    subsystems.Microcontroller.IPAddress = null;
            }
            catch
            {
                subsystems.Microcontroller.IPAddress = null;
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Read new sensor values from the microcontroller
        /// </summary>
        public async void GetNewSensorValues()
        {
            if (subsystems.Microcontroller.IPAddress == null)
                return;

            try
            {

                HttpClient client = new HttpClient();
                var responseString = await client.GetStringAsync("http://" + subsystems.Microcontroller.IPAddress + "/sensors");
                var sensorValues = responseString.Split(",");

                subsystems.Microcontroller.LightSensor = sensorValues[0] == "0";
                subsystems.Microcontroller.MovementSensor = sensorValues[1] == "1";
                subsystems.Microcontroller.SoundSensor = Int32.Parse(sensorValues[2]);
                subsystems.Microcontroller.LEDRingStrip = Color.FromName(sensorValues[3]);
                subsystems.Microcontroller.SystemMode = (SystemMode)Enum.Parse(typeof(SystemMode), sensorValues[4]);
                subsystems.Microcontroller.SystemRegime = (SystemRegime)Enum.Parse(typeof(SystemRegime), sensorValues[5]);
            }
            catch
            {
                subsystems.Microcontroller.IPAddress = null;
            }
        }

        #endregion

        #region Routes for Cameras Communication

        [HttpPost]
        public ActionResult EstablishConnection(string cameraID)
        {
            if (!subsystems.Cameras.Any(c => c.ID == cameraID))
            {
                subsystems.Cameras.Add(new Camera(cameraID));
            }

            subsystems.Cameras[0].DetectFace();

            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult ChangeCamera(string camera)
        {
            subsystems.ActiveCamera = subsystems.Cameras.FindIndex(c => c.ID == camera);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult ReceivePhoto(string ID, string base64Image)
        {
            Camera camera = subsystems.Cameras.Find(c => c.ID == ID);

            if (camera == null)
            {
                camera = new Camera(ID);
                subsystems.Cameras.Add(camera);
                subsystems.ActiveCamera = subsystems.Cameras.Count - 1;
            }

            camera.LatestImage = base64Image;
            camera.LatestImageTimestamp = DateTime.Now;

            if (camera.DetectFace().Result && subsystems.Microcontroller.LEDRingStrip == Color.Red)
                camera.Instruction = "Alarm";
            else
                camera.Instruction = "OK";

            return RedirectToAction("Index");
        }

        [HttpGet]
        public string GetInstruction(string ID)
        {
            return subsystems.Cameras.Find(c => c.ID == ID).Instruction;
        }

        #endregion
    }
}