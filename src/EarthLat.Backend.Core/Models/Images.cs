using Azure;
using Azure.Data.Tables;

namespace EarthLat.Backend.Core.Models
{
    public class Images : ITableEntity
    {
        public byte[]? ImgTotal { get; set; }
        public byte[]? ImgDetail { get; set; }

        // StationId
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
