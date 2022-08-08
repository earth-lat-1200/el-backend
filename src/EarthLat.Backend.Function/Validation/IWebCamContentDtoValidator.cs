using EarthLat.Backend.Core.Dtos;

namespace EarthLat.Backend.Function.Validation
{
    public interface IWebCamContentDtoValidator
    {
        bool IsValid(WebCamContentDto webcam);

        bool IsValid(Status status);
    }
}
