using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations.Schema;

namespace EarthLat.Backend.Core.Models
{
    public class Images : ITableEntity
    {
        public byte[]? ImgTotal { get; set; }
        public byte[]? ImgTotalv2 { get; set; }
        public byte[]? ImgDetail { get; set; }
        public byte[]? ImgDetailv2 { get; set; }

        public int ImgTotalKb { get; set; }
        public int ImgDetailKb { get; set; }
        public int ImgTotalCompressedKb { get; set; }
        public int ImgDetailCompressedKb { get; set; }

        // StationId
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
