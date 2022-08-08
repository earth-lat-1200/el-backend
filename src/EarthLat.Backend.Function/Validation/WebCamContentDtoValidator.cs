using EarthLat.Backend.Core.Dtos;
using EarthLat.Backend.Function.Extension;

namespace EarthLat.Backend.Function.Validation
{
    internal class WebCamContentDtoValidator : IWebCamContentDtoValidator
    {
        public bool IsValid(WebCamContentDto webcam)
        {
            webcam.ImgTotal.ThrowIfByreArrIsNull("ImageTotal");
            webcam.ImgDetail.ThrowIfByreArrIsNull("ImgDetail");
            webcam.StationName.ThrowIfIsEmptyOrWhitespace("StationName");
            webcam.StationId.ThrowIfIsEmptyOrWhitespace("StationId");
            webcam.SundialName.ThrowIfIsEmptyOrWhitespace("SundialName");
            webcam.Location.ThrowIfIsEmptyOrWhitespace("Location");

            webcam.Latitude.ThrowIfNotInBetween("Latitude", -90, 90);
            webcam.Longitude.ThrowIfNotInBetween("Longitude", -180, 180);

            webcam.WebcamType.ThrowIfIsEmptyOrWhitespace("WebcamType");
            webcam.TransferType.ThrowIfIsEmptyOrWhitespace("TransferType");
            webcam.SundialInfo.ThrowIfIsEmptyOrWhitespace("SundialInfo");
            webcam.TeamName.ThrowIfIsEmptyOrWhitespace("TeamName");
            webcam.NearbyPublicInstitute.ThrowIfIsEmptyOrWhitespace("NearbyPublicInstitute");
            webcam.OrganizationalForm.ThrowIfIsEmptyOrWhitespace("OrganizationalForm");

            return true;
        }

        public bool IsValid(Status status)
        {
            status.SwVersion.ThrowIfIsEmptyOrWhitespace("SwVersion");
            status.CaptureTime.ThrowIfIsEmptyOrWhitespace("CaptureTime");
            status.CaptureLat.ThrowIfIsEmptyOrWhitespace("CaptureLat");
            status.CpuTemparature.ThrowIfIsEmptyOrWhitespace("CpuTemparature");
            status.CameraTemparature.ThrowIfIsEmptyOrWhitespace("CameraTemparature");
            status.OutcaseTemparature.ThrowIfIsEmptyOrWhitespace("OutcaseTemparature");
            return true;
        }
    }
}
