using System;

namespace EarthLat.Backend.Core.Dtos
{
    public class CleanUpDto
    {
        public string StationId { get; set; }
        public DateTime DeleteAllBeforeTimestamp { get; set; }
    }
}
