using EarthLat.Backend.Core.Dtos;
using EarthLat.Backend.Core.Models;

namespace EarthLat.Backend.Core.Interfaces
{
    public interface ISundialLogic
    {
        Task<IEnumerable<Station>> GetAllStationsAsync();
        Task<Images> GetLatestImagesByIdAsync(string stationId);
        Task<Station> GetStationByIdAsync(string stationId);
        Task<RemoteConfig> AddAsync(Station station, Images images, Status status, string requestBody);
        Task<RemoteConfig> AddOrUpdateRemoteConfigAsync(RemoteConfig remoteConfig, string stationId);
        Task<RemoteConfig?> GetRemoteConfigById(string stationId);
        Task<int> CleanUp(DateTime deleteAllBeforeTimestamp, string stationId = "");
    }
}
