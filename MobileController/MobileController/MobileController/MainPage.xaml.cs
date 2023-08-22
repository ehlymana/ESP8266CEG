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
using Plugin.Messaging;
using Android.Content.PM;
using Plugin.Permissions;
using System.Drawing;

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

            // grant additional phone call permission
            GetRuntimePermission();
        }

        /// <summary>
        /// Grant the phone call runtime permission when the application is started
        /// </summary>
        async void GetRuntimePermission()
        {
            Plugin.Permissions.Abstractions.PermissionStatus status = await CrossPermissions.Current.CheckPermissionStatusAsync<Plugin.Permissions.PhonePermission>();
            if (status.ToString() != PermissionStatus.Granted.ToString())
            {
                status = await CrossPermissions.Current.RequestPermissionAsync<Plugin.Permissions.PhonePermission>();
            }
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
        /// Take and send pictures
        /// </summary>
        void CommunicateWithWebServer()
        {
            if (!connected)
                return;

            // take new photo
            CameraViewControl.Shutter();
        }

        /// <summary>
        /// Save image captured by the CameraView to web-server and receive alarm instructions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        async void CameraView_MediaCaptured(object sender, MediaCapturedEventArgs e)
        {
            imageData = e.ImageData;

            // send photo to web-server

            var formData = new Dictionary<string, string>
            {
                { "cameraID", cameraID },
                { "base64Image", Convert.ToBase64String(imageData) }
            };

            var encodedItems = formData.Select(i => WebUtility.UrlEncode(i.Key) + "=" + WebUtility.UrlEncode(i.Value));
            var encodedContent = new StringContent(System.String.Join("&", encodedItems), null, "application/x-www-form-urlencoded");

            string webAddress = webServer + "/Home/ReceivePhoto";
            var httpHandler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (o, cert, chain, errors) => true };
            HttpClient client = new HttpClient(httpHandler);

            try
            {
                var response = await client.PostAsync(webAddress, encodedContent);
                string responseString = await response.Content.ReadAsStringAsync();
                if (responseString == "Not found")
                {
                    DisplayAlert("Information", "Please reconnect the phone to the web-server.", "OK");
                    connected = false;
                }

                // alarm needs to be triggered - begin alarm handling
                if (responseString == "Alarm")
                {
                    AlarmRectangle.Fill = new SolidColorBrush(Xamarin.Forms.Color.Red);
                    AlarmIndicatorLabel.Text = "Triggered";

                    // call the police
                    try
                    {
                        var phoneDialer = CrossMessaging.Current.PhoneDialer;
                        if (phoneDialer.CanMakePhoneCall)
                            phoneDialer.MakePhoneCall("911");
                        phoneCall = true;
                    }
                    catch
                    {
                        DisplayAlert("Information", "Phone call could not be initiated.", "OK");
                        phoneCall = false;
                    }

                    // play the default alarm on speakers
                    try
                    {
                        Android.Net.Uri soundUri = RingtoneManager.GetDefaultUri(RingtoneType.Alarm);
                        Ringtone r = RingtoneManager.GetRingtone(Android.App.Application.Context, soundUri);
                        r.Play();

                        speakers = true;
                    }
                    catch
                    {
                        speakers = false;
                    }

                    // rest for a minute for the phone call to complete and alarm to finish playing
                    Thread.Sleep(1000 * 60);
                }
                else
                {
                    AlarmRectangle.Fill = new SolidColorBrush(Xamarin.Forms.Color.Green);
                    AlarmIndicatorLabel.Text = "Not triggered";
                    phoneCall = false;
                    speakers = false;
                }
            }
            catch (System.Exception ex)
            {
                DisplayAlert("Information", "Please reconnect the phone to the web-server.", "OK");
                connected = false;
            }

            // reset image data for sending
            imageData = null;

            PhoneCallSwitch.IsToggled = phoneCall;
            SpeakersSwitch.IsToggled = speakers;
        }
    }
}
