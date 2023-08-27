using ESPClient.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.Linq;

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

        #region Black Box Testing

        /// <summary>
        /// Route for performing black-box testing and exporting the testing results to a TXT file
        /// </summary>
        [HttpGet]
        public ActionResult BlackBoxTesting()
        {
            List<double> FDR_CEG1 = new List<double>(), FDR_CEG1Min1 = new List<double>(), FDR_CEG1Min2 = new List<double>(),
                         FDR_CEG2 = new List<double>(), FDR_CEG2Min1 = new List<double>(), FDR_CEG2Min2 = new List<double>(),
                         MTSI_CEG1Min1 = new List<double>(), MTSI_CEG1Min2 = new List<double>(),
                         MTSI_CEG2Min1 = new List<double>(), MTSI_CEG2Min2 = new List<double>(),
                         ITE_CEG1 = new List<double>(), ITE_CEG1Min1 = new List<double>(), ITE_CEG1Min2 = new List<double>(),
                         ITE_CEG2 = new List<double>(), ITE_CEG2Min1 = new List<double>(), ITE_CEG2Min2 = new List<double>();

            // find out which tests detect which faults
            List<List<int>> testsWhichDetectFaultsCEG1 = new List<List<int>>(),
                            testsWhichDetectFaultsCEG2 = new List<List<int>>();

            for (int i = 0; i < 7; i++)
                testsWhichDetectFaultsCEG1.Add(FindOutWhichTestDetectsFault(0, i));

            for (int i = 0; i < 5; i++)
                testsWhichDetectFaultsCEG2.Add(FindOutWhichTestDetectsFault(1, i));

            for (int tryBB = 0; tryBB < 100; tryBB++)
            {
                // form the list of all feasible test cases and expected outcomes
                List<Test> testsCEG1 = GetAllFeasibleTests(0),
                           testsCEG2 = GetAllFeasibleTests(1);


                // activate a random number of faults for CEG1
                Random random = new Random();
                int numberOfFaults = random.Next(1, 8);
                List<int> allFaults = new List<int>();
                for (int i = 0; i < numberOfFaults; i++)
                {
                    int nextFault = random.Next(0, 7);
                    if (!allFaults.Contains(nextFault))
                        allFaults.Add(nextFault);
                }

                // the faults need to be sorted so that software errors do not affect hardware errors
                allFaults.Sort();

                // inject each fault into all tests
                foreach (int fault in allFaults)
                    foreach (Test test in testsCEG1)
                        test.InjectFault(fault);

                // check how many faults were detected
                List<int> detectedFaultsCEG1 = new List<int>(),
                          detectedFaultsCEG1Min1 = new List<int>(),
                          detectedFaultsCEG1Min2 = new List<int>();

                for (int i = 0; i < testsCEG1.Count; i++)
                {
                    // check if test fails
                    if (!testsCEG1[i].TestPasses())
                    {
                        // check each injected fault
                        foreach (int fault in allFaults)
                        {
                            // test does not detect this fault - move to the next fault
                            if (!testsWhichDetectFaultsCEG1[fault].Contains(i))
                                continue;

                            // this fault was not detected before - capture it
                            if  (!detectedFaultsCEG1.Contains(fault))
                                detectedFaultsCEG1.Add(fault);
                            if (!detectedFaultsCEG1Min1.Contains(fault) && i < 10)
                                detectedFaultsCEG1Min1.Add(fault);
                            if (!detectedFaultsCEG1Min2.Contains(fault) && (i == 0 || i == 7 || i == 9))
                                detectedFaultsCEG1Min2.Add(fault);
                        }
                    }
                }

                // calculate metrics for CEG1
                FDR_CEG1.Add((double)detectedFaultsCEG1.Count / (double)allFaults.Count * 100.00);
                FDR_CEG1Min1.Add((double)detectedFaultsCEG1Min1.Count / (double)allFaults.Count * 100.00);
                FDR_CEG1Min2.Add((double)detectedFaultsCEG1Min2.Count / (double)allFaults.Count * 100.00);

                if (detectedFaultsCEG1.Count > 0)
                {
                    MTSI_CEG1Min1.Add((1.00 - (double)detectedFaultsCEG1Min1.Count / (double)detectedFaultsCEG1.Count) * 100.00);
                    MTSI_CEG1Min2.Add((1.00 - (double)detectedFaultsCEG1Min2.Count / (double)detectedFaultsCEG1.Count) * 100.00);
                }
                else
                {
                    MTSI_CEG1Min1.Add(0.00);
                    MTSI_CEG1Min2.Add(0.00);
                }              

                ITE_CEG1.Add((double)detectedFaultsCEG1.Count / 64.00);
                ITE_CEG1Min1.Add((double)detectedFaultsCEG1Min1.Count / 10.00);
                ITE_CEG1Min2.Add((double)detectedFaultsCEG1Min2.Count / 3.00);

                // activate a random number of faults for CEG2
                numberOfFaults = random.Next(1, 6);
                allFaults = new List<int>();
                for (int i = 0; i < numberOfFaults; i++)
                {
                    int nextFault = random.Next(0, 5);

                    if (!allFaults.Contains(nextFault))
                        allFaults.Add(nextFault);
                }

                // inject each fault into all tests
                foreach (int fault in allFaults)
                    foreach (Test test in testsCEG2)
                        test.InjectFault(fault);

                // check how many faults were detected
                List<int> detectedFaultsCEG2 = new List<int>(),
                          detectedFaultsCEG2Min1 = new List<int>(),
                          detectedFaultsCEG2Min2 = new List<int>();

                for (int i = 0; i < testsCEG2.Count; i++)
                {
                    // check if test fails
                    if (!testsCEG2[i].TestPasses())
                    {
                        // check each injected fault
                        foreach (int fault in allFaults)
                        {
                            // test does not detect this fault - move to the next fault
                            if (!testsWhichDetectFaultsCEG2[fault].Contains(i))
                                continue;

                            // this fault was not detected before - capture it
                            if (!detectedFaultsCEG2.Contains(fault))
                                detectedFaultsCEG2.Add(fault);
                            if (!detectedFaultsCEG2Min1.Contains(fault) && (i == 7 || i == 10 || i == 13 || i == 16 || i == 17 || i == 19 || i == 22 || i == 25))
                                detectedFaultsCEG2Min1.Add(fault);
                            if (!detectedFaultsCEG2Min2.Contains(fault) && (i == 16 || i == 17 || i == 25))
                                detectedFaultsCEG2Min2.Add(fault);
                        }
                    }
                }

                // calculate metrics for CEG2
                FDR_CEG2.Add((double)detectedFaultsCEG2.Count / (double)allFaults.Count * 100.00);
                FDR_CEG2Min1.Add((double)detectedFaultsCEG2Min1.Count / (double)allFaults.Count * 100.00);
                FDR_CEG2Min2.Add((double)detectedFaultsCEG2Min2.Count / (double)allFaults.Count * 100.00);

                if (detectedFaultsCEG2.Count > 0)
                {
                    MTSI_CEG2Min1.Add((1.00 - (double)detectedFaultsCEG2Min1.Count / (double)detectedFaultsCEG2.Count) * 100.00);
                    MTSI_CEG2Min2.Add((1.00 - (double)detectedFaultsCEG2Min2.Count / (double)detectedFaultsCEG2.Count) * 100.00);
                }
                else
                {
                    MTSI_CEG2Min1.Add(0.00);
                    MTSI_CEG2Min2.Add(0.00);
                }

                ITE_CEG2.Add((double)detectedFaultsCEG2.Count / 30.00);
                ITE_CEG2Min1.Add((double)detectedFaultsCEG2Min1.Count / 8.00);
                ITE_CEG2Min2.Add((double)detectedFaultsCEG2Min2.Count / 3.00);
            }

            // export average, min and max metrics results
            string results = "";

            results += "CEG1 FDR, MAX: " + Math.Round(FDR_CEG1.Max(), 2) + "%, AVG: " + Math.Round(FDR_CEG1.Average(), 2) + "%, MIN: " + Math.Round(FDR_CEG1.Min(), 2) + "%\n";
            results += "FDR CEG1 MIN1, MAX: " + Math.Round(FDR_CEG1Min1.Max(), 2) + "%, AVG: " + Math.Round(FDR_CEG1Min1.Average(), 2) + "%, MIN: " + Math.Round(FDR_CEG1Min1.Min(), 2) + "%\n";
            results += "FDR CEG1 MIN2, MAX: " + Math.Round(FDR_CEG1Min2.Max(), 2) + "%, AVG: " + Math.Round(FDR_CEG1Min2.Average(), 2) + "%, MIN: " + Math.Round(FDR_CEG1Min2.Min(), 2) + "%\n";

            results += "\n";

            results += "MTSI CEG1 MIN1, MAX: " + Math.Round(MTSI_CEG1Min1.Max(), 2) + "%, AVG: " + Math.Round(MTSI_CEG1Min1.Average(), 2) + "%, MIN: " + Math.Round(MTSI_CEG1Min1.Min(), 2) + "%\n";
            results += "MTSI CEG1 MIN2, MAX: " + Math.Round(MTSI_CEG1Min2.Max(), 2) + "%, AVG: " + Math.Round(MTSI_CEG1Min2.Average(), 2) + "%, MIN: " + Math.Round(MTSI_CEG1Min2.Min(), 2) + "%\n";

            results += "\n";

            results += "ITE CEG1, MAX: " + Math.Round(ITE_CEG1.Max(), 2) + ", AVG: " + Math.Round(ITE_CEG1.Average(), 2) + ", MIN: " + Math.Round(ITE_CEG1.Min(), 2) + "\n";
            results += "ITE CEG1 MIN1, MAX: " + Math.Round(ITE_CEG1Min1.Max(), 2) + ", AVG: " + Math.Round(ITE_CEG1Min1.Average(), 2) + ", MIN: " + Math.Round(ITE_CEG1Min1.Min(), 2) + "\n";
            results += "ITE CEG1 MIN2, MAX: " + Math.Round(ITE_CEG1Min2.Max(), 2) + ", AVG: " + Math.Round(ITE_CEG1Min2.Average(), 2) + ", MIN: " + Math.Round(ITE_CEG1Min2.Min(), 2) + "\n";

            results += "\n";

            results += "FDR CEG2, MAX: " + Math.Round(FDR_CEG2.Max(), 2) + "%, AVG: " + Math.Round(FDR_CEG2.Average(), 2) + "%, MIN: " + Math.Round(FDR_CEG2.Min(), 2) + "%\n";
            results += "FDR CEG2 MIN1, MAX: " + Math.Round(FDR_CEG2Min1.Max(), 2) + "%, AVG: " + Math.Round(FDR_CEG2Min1.Average(), 2) + "%, MIN: " + Math.Round(FDR_CEG2Min1.Min(), 2) + "%\n";
            results += "FDR CEG2 MIN2, MAX: " + Math.Round(FDR_CEG2Min2.Max(), 2) + "%, AVG: " + Math.Round(FDR_CEG2Min2.Average(), 2) + "%, MIN: " + Math.Round(FDR_CEG2Min2.Min(), 2) + "%\n";

            results += "\n";

            results += "MTSI CEG2 MIN1, MAX: " + Math.Round(MTSI_CEG2Min1.Max(), 2) + "%, AVG: " + Math.Round(MTSI_CEG2Min1.Average(), 2) + "%, MIN: " + Math.Round(MTSI_CEG2Min1.Min(), 2) + "%\n";
            results += "MTSI CEG2 MIN2, MAX: " + Math.Round(MTSI_CEG2Min2.Max(), 2) + "%, AVG: " + Math.Round(MTSI_CEG2Min2.Average(), 2) + "%, MIN: " + Math.Round(MTSI_CEG2Min2.Min(), 2) + "%\n";

            results += "\n";

            results += "ITE CEG2, MAX: " + Math.Round(ITE_CEG2.Max(), 2) + ", AVG: " + Math.Round(ITE_CEG2.Average(), 2) + ", MIN: " + Math.Round(ITE_CEG2.Min(), 2) + "\n";
            results += "ITE CEG2 MIN1, MAX: " + Math.Round(ITE_CEG2Min1.Max(), 2) + ", AVG: " + Math.Round(ITE_CEG2Min1.Average(), 2) + ", MIN: " + Math.Round(ITE_CEG2Min1.Min(), 2) + "\n";
            results += "ITE CEG2 MIN2, MAX: " + Math.Round(ITE_CEG2Min2.Max(), 2) + ", AVG: " + Math.Round(ITE_CEG2Min2.Average(), 2) + ", MIN: " + Math.Round(ITE_CEG2Min2.Min(), 2) + "\n";

            System.IO.File.WriteAllText("BB_metrics.txt", results);

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Helper method for finding out which tests detect which faults
        /// </summary>
        public List<int> FindOutWhichTestDetectsFault(int graph, int fault)
        {
            if (graph == 0 && fault == 0)
                System.IO.File.WriteAllText("detection_of_faults.txt", "");

            List<int> testsWhichDetectFault = new List<int>();

            // forming the list of all feasible test cases and expected outcomes
            List<Test> testsCEG = GetAllFeasibleTests(graph);


            // inject the fault into all tests
            for (int i = 0; i < testsCEG.Count; i++)
            {
                testsCEG[i].InjectFault(fault);
                if (!testsCEG[i].TestPasses())
                    testsWhichDetectFault.Add(i);
            }

            string results = "CEG: " + graph + ", FAULT: " + fault + "\n";
            results += "TESTS: " + testsWhichDetectFault.Count + ", INDIVIDUAL: ";
            foreach (int test in testsWhichDetectFault)
                results += "T" + test + " ";
            results += "\n\n";

            System.IO.File.AppendAllText("detection_of_faults.txt", results);

            return testsWhichDetectFault;
        }

        /// <summary>
        /// Helper method for importing all test cases from TXT files
        /// </summary>
        /// <param name="graph"></param>
        /// <returns></returns>
        List<Test> GetAllFeasibleTests(int graph)
        {
            List<string> inputFiles = new List<string>() { "testCases-IoT.txt", "testCases-server.txt" };
            List<Test> tests = new List<Test>();

            // read all test configurations from the input file
            string[] testsTXT = System.IO.File.ReadAllLines(inputFiles[graph]);

            // divide all tests into cause and effect values
            foreach (var test in testsTXT.Skip(1))
            {
                string[] causesAndEffectsTXT = test.Split("\t");
                List<bool> causes = new List<bool>();
                List<bool> effects = new List<bool>();

                int countCauses = 9, countEffects = 3;
                if (graph == 1)
                {
                    countCauses = 8;
                    countEffects = 6;
                }

                for (int j = 1; j < countCauses; j++)
                    causes.Add(causesAndEffectsTXT[j] == "1");

                for (int j = countCauses; j < countCauses + countEffects; j++)
                    effects.Add(causesAndEffectsTXT[j] == "1");

                tests.Add(new Test(graph, causes, effects));
            }

            return tests;
        }

        #endregion

    }
}