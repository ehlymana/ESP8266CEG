namespace ESPClient.Models
{
    public class Subsystems
    {
        #region Properties

        public Microcontroller Microcontroller { get; set; }

        public List<Camera> Cameras { get; set; }

        public int ActiveCamera { get; set; }

        public string SignalToSend { get; set; }

        #endregion

        #region Constructor

        public Subsystems ()
        {
            Microcontroller = new Microcontroller();
            Cameras = new List<Camera>();
            ActiveCamera = -1;
        }

        #endregion
    }
}
