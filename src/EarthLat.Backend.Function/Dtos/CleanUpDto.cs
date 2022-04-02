using System;

namespace EarthLat.Backend.Function.Dtos
{
    public class CleanUpDto
    {
        public string StationId { get; set; }
        public DateTime DeleteAllBeforeTimestamp { get; set; }
    }
}
