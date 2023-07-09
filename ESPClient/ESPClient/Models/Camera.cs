﻿namespace ESPClient.Models
{
    public class Camera
    {
        #region Properties

        public string? LatestImage { get; set; }

        public DateTime LatestImageTimestamp { get; set; }

        public List<string>? Events { get; set; }

        #endregion
    }
}
