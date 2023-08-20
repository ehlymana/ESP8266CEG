using Java.Lang;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Java.Net;
using Xamarin.Essentials;
using Newtonsoft.Json;
using Android.Content;
using Android.Provider;
using Android.Media;
using static Android.Resource;
using Android.Hardware;
using Android.Graphics;
using Android.OS;
using static Android.App.Assist.AssistStructure;
using System.IO;
using Android.Util;
using static Xamarin.Essentials.Permissions;
using Android.Views;
using Xamarin.CommunityToolkit.UI.Views;
using Java.Lang.Reflect;

namespace MobileController
{
    public partial class MainPage : ContentPage
    {
        bool connected = false,
             speakers = false,
             phoneCall = false;
        string webServer = "",
               cameraID = "";
        byte[] imageData;
        public MainPage()
        {
            InitializeComponent();

            // create timer for communicating with web-server every 10 seconds
            Device.StartTimer(TimeSpan.FromSeconds(10), () =>
            {
                CommunicateWithWebServer();
                return true;
            });
        }

        /// <summary>
        /// Establish connection with the web-server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        async void Connect(object sender, EventArgs args)
        {
            webServer = WebServerEntry.Text;
            cameraID = CameraIDEntry.Text;

            // connect to web-server
            try
            {
                string webAddress = webServer + "/Home/EstablishConnection?cameraID=" + cameraID;
                var httpHandler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (o, cert, chain, errors) => true };
                HttpClient client = new HttpClient(httpHandler);
                var response = await client.PostAsync(webAddress, null);
                connected = response.StatusCode == HttpStatusCode.OK;
            }
            catch
            {
                connected = false;
            }

            // show information about the connection process to the user
            string result = "Connection successfully established!";
            if (!connected)
                result = "Connection failed. Please try again.";

            await DisplayAlert("Information", result, "OK");
        }

        /// <summary>
        /// Take and send pictures to web-server and receive alarm instructions
        /// </summary>
        async void CommunicateWithWebServer()
        {
            if (!connected)
                return;

            // take new photo
            CameraViewControl.Shutter();

            // wait for the image capture to finish
            while (imageData == null) ;

            // send photo to web-server
            string webAddress = webServer + "/Home/ReceivePhoto?cameraID=" + cameraID + "&base64Image=" + Convert.ToBase64String(imageData);
            var httpHandler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (o, cert, chain, errors) => true };
            HttpClient client = new HttpClient(httpHandler);
            var response = await client.PostAsync(webAddress, null);
            string responseString = await response.Content.ReadAsStringAsync();
            if (responseString == "Not found")
            {
                DisplayAlert("Information", "Please reconnect the phone to the web-server.", "OK");
                connected = false;
            }

            imageData = null;

            // alarm needs to be triggered - begin alarm handling
            if (responseString == "Alarm")
            {
                AlarmRectangle.Fill = new SolidColorBrush(Xamarin.Forms.Color.Red);
                AlarmIndicatorLabel.Text = "Triggered";

                try
                {
                    PhoneDialer.Open("911");
                }
                catch
                {
                    DisplayAlert("Information", "Phone call could not be initiated.", "OK");
                }

                Android.Net.Uri soundUri = RingtoneManager.GetDefaultUri(RingtoneType.Alarm);
                Ringtone r = RingtoneManager.GetRingtone(Android.App.Application.Context, soundUri);
                r.Play();

                Thread.Sleep(1000 * 60);
            }
            else
            {
                AlarmRectangle.Fill = new SolidColorBrush(Xamarin.Forms.Color.Green);
                AlarmIndicatorLabel.Text = "Not triggered";
            }

        }
        void CameraView_MediaCaptured(object sender, MediaCapturedEventArgs e)
        {
            imageData = e.ImageData;
        }
    }
}
