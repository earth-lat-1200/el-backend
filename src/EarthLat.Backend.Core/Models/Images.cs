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
        public string SwVersion { get; set; }
        public string CaptureTime { get; set; }
        public string CaptureLat { get; set; }
        public string CpuTemparature { get; set; }
        public string CameraTemparature { get; set; }
        public string OutcaseTemparature { get; set; }
        public string Brightness { get; set; }
        public bool Sunny { get; set; }
        public bool Cloudy { get; set; }
        public bool Night { get; set; }
        public string SunlitLikelyhood { get; set; }

        // StationId
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
