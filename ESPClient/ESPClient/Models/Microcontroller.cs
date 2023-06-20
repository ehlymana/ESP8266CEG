using System.Drawing;

namespace ESPClient.Models
{
    public class Microcontroller
    {
        public string? IPAddress { get; set; }

        public SystemMode SystemMode { get; set; }

        public SystemRegime SystemRegime { get; set; }
        
        public bool LightSensor { get; set; }

        public bool MovementSensor { get; set; }

        public int SoundSensor { get; set; }

        public Color LEDRingStrip { get; set; }

        public List<Camera> Cameras { get; set; }
    }
}
