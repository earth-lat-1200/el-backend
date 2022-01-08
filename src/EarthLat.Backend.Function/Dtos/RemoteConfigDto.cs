using System;

namespace EarthLat.Backend.Function.Dtos
{
    public class RemoteConfigDto
    {
        public bool IsCamOffline { get; set; }
        public TimeSpan Period { get; set; }
        public bool IsSeries { get; set; }
        public bool IsZoomMove { get; set; }
        public bool IsZoomDrawRect { get; set; }
        public int ZoomCenterPerCX { get; set; }
        public int ZoomCenterPerCy { get; set; }
    }
}
