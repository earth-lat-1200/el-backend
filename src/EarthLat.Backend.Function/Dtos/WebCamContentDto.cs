namespace EarthLat.Backend.Function.Dtos
{
    public class WebCamContentDto
    {
        public byte[] ImgTotal { get; set; }
        public byte[] ImgDetail { get; set; }
        public string StationName { get; set; }
        public string StationId { get; set; }
        public string SundialName { get; set; }
        public string Location { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string WebcamType { get; set; }
        public string TransferType { get; set; }
        public string SundialInfo { get; set; }
        public string WebsiteUrl { get; set; }
        public string TeamName { get; set; }
        public string NearbyPublicInstitute { get; set; }
        public string OrganizationalForm { get; set; }
    }
}
