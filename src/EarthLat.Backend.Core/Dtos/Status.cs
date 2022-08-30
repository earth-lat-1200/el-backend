namespace EarthLat.Backend.Core.Dtos
{
    public class Status
    {
        public string SwVersion { get; set; }
        public string CaptureTime { get; set; }
        public string CaptureLat { get; set; }
        public float CpuTemparature { get; set; }
        public float CameraTemparature { get; set; }
        public float OutcaseTemparature { get; set; }
        public float Brightness { get; set; }
        public bool Sunny { get; set; }
        public bool Cloudy { get; set; }
        public bool Night { get; set; }
    }
}


