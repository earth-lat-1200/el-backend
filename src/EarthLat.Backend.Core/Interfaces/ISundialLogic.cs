using EarthLat.Backend.Core.Models;

namespace EarthLat.Backend.Core.Interfaces
{
    public interface ISundialLogic
    {
        Task<IEnumerable<Station>> GetAllStationsAsync();
        Task<Images> GetLatestImagesByIdAsync(string stationId);
        Task<Images> GetLatestImagesByAnglesAsync(string longitude, string latitude);
        Task<Station> GetStationByIdAsync(string stationId);
        Task<Station> GetStationByAnglesAsync(string longitude, string latitude);
        Task<RemoteConfig> AddAsync(Station station, Images images);
    }
}
