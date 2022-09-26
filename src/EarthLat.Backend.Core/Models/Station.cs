using Azure;
using Azure.Data.Tables;

namespace EarthLat.Backend.Core.Models
{
    public class Station : ITableEntity
    {
        public string? StationName { get; set; }
        public string? SundialName { get; set; }
        public bool IsActive { get; set; } = true;
        public string? LastImageKey { get; set; }
        public string? Location { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? WebcamType { get; set; }
        public string? TransferType { get; set; }
        public string? SundialInfo { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? TeamName { get; set; }
        public string? NearbyPublicInstitute { get; set; }
        public string? OrganizationalForm { get; set; }
        public byte[]? ReferenceImage { get; set; }
        public int TimezoneOffset { get; set; }


        public string PartitionKey { get; set; } = "station";

        // StationId
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
