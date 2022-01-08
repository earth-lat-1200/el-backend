using EarthLat.Backend.Function.Dtos;

namespace EarthLat.Backend.Function.Validation
{
    public interface IWebCamContentDtoValidator
    {
        bool IsValid(WebCamContentDto webcam);

        bool IsValid(Status status);
    }
}
