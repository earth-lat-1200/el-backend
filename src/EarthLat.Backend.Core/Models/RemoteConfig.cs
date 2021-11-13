using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthLat.Backend.Core.Models
{
    public class RemoteConfig
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
