using Azure;
using Azure.Data.Tables;

namespace EarthLat.Backend.Core.Models
{
    public class RemoteConfig : ITableEntity
    {
        public bool IsCamOffline { get; set; }
        public int Period { get; set; }
        public bool IsSeries { get; set; }
        public bool IsZoomMove { get; set; }
        public bool IsZoomDrawRect { get; set; }
        public int ZoomCenterPerCX { get; set; }
        public int ZoomCenterPerCy { get; set; }
        // StationId
        public string PartitionKey { get; set; }

        // PartitionKey + _Config
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
