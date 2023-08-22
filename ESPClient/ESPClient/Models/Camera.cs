using Microsoft.AspNetCore.DataProtection.KeyManagement;
using System;
using System.Net;
using System.Net.Http.Headers;
using static System.Net.Mime.MediaTypeNames;

namespace ESPClient.Models
{
    public class Camera
    {
        #region Properties

        public string? ID { get; set; }

        public string? LatestImage { get; set; }

        public DateTime LatestImageTimestamp { get; set; }

        public List<string>? Events { get; set; }

        public string Instruction { get; set; }

        #endregion

        #region Constructor

        public Camera(string id)
        {
            ID = id;
            Events = new List<string>();
            Instruction = "OK";
        }

        #endregion

        #region Methods

        public string GetAllEvents()
        {
            string result = "";
            foreach (string e in Events)
                result += e.ToString() + Environment.NewLine;
            return result;
        }

        public async Task<bool> DetectFace()
        {
            // reduce image quality
            byte[] imageBytes = System.Convert.FromBase64String(LatestImage.Replace("data:image/jpeg;base64,", String.Empty));
            System.IO.MemoryStream myMemStream = new System.IO.MemoryStream(imageBytes);
            System.Drawing.Image fullsizeImage = System.Drawing.Image.FromStream(myMemStream);
            fullsizeImage.RotateFlip(System.Drawing.RotateFlipType.Rotate90FlipNone);
            System.Drawing.Image newImage = fullsizeImage.GetThumbnailImage(200, 200, null, IntPtr.Zero);
            System.IO.MemoryStream myResult = new System.IO.MemoryStream();
            newImage.Save(myResult, System.Drawing.Imaging.ImageFormat.Png);
            byte[] compressedImage = myResult.ToArray();
            LatestImage = "data:image/jpeg;base64," + Convert.ToBase64String(compressedImage);

            // call face detection API
            string apiKey = "R-obeaVP3CRAI9ILR0-_OeqDSWOylX2M",
                   apiSecret = "jcRfB3Eh-sg2j9UjS56fwEPDVf8sOdqN",
                   apiURL = "https://api-us.faceplusplus.com/facepp/v3/detect";
            HttpClient client = new HttpClient();
            var values = new Dictionary<string, string>
              {
                  { "api_key", apiKey },
                  { "api_secret", apiSecret },
                  { "image_base64", LatestImage }
              };

            var content = new FormUrlEncodedContent(values);

            var response = await client.PostAsync(apiURL, content);

            string responseString = await response.Content.ReadAsStringAsync(),
                   phrase = "\"face_num\":",
                   facesCount = responseString.Substring(responseString.IndexOf(phrase) + phrase.Length);
            facesCount = facesCount.Replace("}", "").Replace("\n", "");
            return Int32.Parse(facesCount) > 0;
        }

        #endregion
    }
}
