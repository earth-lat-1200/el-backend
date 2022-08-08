namespace EarthLat.Backend.Core.Dtos
{
    public class Status
    {
        public string SwVersion { get; set; }
        public string CaptureTime { get; set; }
        public string CaptureLat { get; set; }
        public string CpuTemparature { get; set; }
        public string CameraTemparature { get; set; }
        public string OutcaseTemparature { get; set; }
        public double Brightness { get; set; }
        public bool Sunny { get; set; }
        public bool Cloudy { get; set; }
        public bool Night { get; set; }
    }
}


