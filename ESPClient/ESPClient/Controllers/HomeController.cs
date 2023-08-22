using ESPClient.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Net.Http.Headers;
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

            SystemMode newMode = mode == "day" ? SystemMode.Day : SystemMode.Night;
            SystemRegime newRegime = regime == "home" ? SystemRegime.AtHome : SystemRegime.Away;

            try
            {
                HttpClient client = new HttpClient();
                var responseString = await client.GetStringAsync("http://" + subsystems.Microcontroller.IPAddress + "/" + mode);
                if (responseString != "Change: OK")
                    subsystems.Microcontroller.IPAddress = null;
                else
                    subsystems.Microcontroller.SystemMode = newMode;

                responseString = await client.GetStringAsync("http://" + subsystems.Microcontroller.IPAddress + "/" + regime);
                if (responseString != "Change: OK")
                    subsystems.Microcontroller.IPAddress = null;
                else
                    subsystems.Microcontroller.SystemRegime = newRegime;
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
                subsystems.Microcontroller.SoundSensor = Double.Parse(sensorValues[2]);
                subsystems.Microcontroller.LEDRingStrip = Color.FromName(sensorValues[3]);
            }
            catch
            {
                subsystems.Microcontroller.IPAddress = null;
            }
        }

        #endregion

        #region Routes for Cameras Communication

        [HttpPost]
        public HttpResponseMessage EstablishConnection(string cameraID)
        {
            if (!subsystems.Cameras.Any(c => c.ID == cameraID))
            {
                subsystems.Cameras.Add(new Camera(cameraID));
                subsystems.Cameras[subsystems.Cameras.Count - 1].Events.Add("Connection successfully established at: " + DateTime.Now.ToString());
                subsystems.ActiveCamera = subsystems.Cameras.Count - 1;
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        [HttpPost]
        public ActionResult ChangeCamera(string camera)
        {
            subsystems.ActiveCamera = subsystems.Cameras.FindIndex(c => c.ID == camera);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public string ReceivePhoto([FromForm] PhotoModel data)
        {
            Camera camera = subsystems.Cameras.Find(c => c.ID == data.cameraID);

            if (camera == null)
                return "Not found";

            camera.Events.Add("New image received at: " + DateTime.Now.ToString());
            camera.LatestImage = "data:image/jpeg;base64," + data.base64Image;
            camera.LatestImageTimestamp = DateTime.Now;

            bool faces = camera.DetectFace().Result;
            if (faces)
                camera.Events.Add("Faces detected on the image");
            else
                camera.Events.Add("No faces detected on the image.");

            if (faces && subsystems.Microcontroller.LEDRingStrip == Color.Red)
            {
                camera.Events.Add("Alarm signal initiated");
                camera.Instruction = "Alarm";
            }
            else
            {
                camera.Instruction = "OK";
            }

            return camera.Instruction;
        }

    #endregion
    }
}