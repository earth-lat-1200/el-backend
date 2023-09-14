using EarthLat.Backend.Core.Dtos;
using EarthLat.Backend.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace EarthLat.Backend.Core.Interfaces
{
    public interface ISundialLogic
    {
        Task<IEnumerable<Station>> GetAllStationsAsync();
        Task<Images> GetLatestCombinedImagesByIdAsync(string stationId);
        Task<Images> GetLatestImagesById(string stationId);
        Task<Station> GetStationByIdAsync(string stationId);
        Task<RemoteConfig> AddAsync(Station station, Images images, Status status);
        Task<RemoteConfig> AddOrUpdateRemoteConfigAsync(RemoteConfig remoteConfig, string stationId);
        Task<RemoteConfig?> GetRemoteConfigById(string stationId);
        Task<string> SetPriorityOfStationById(string stationId, double newPriority);
        Task<string> SetIsOnlineOfStationById(string stationId);
        Task<PriorityMultiplicatorsDto> GetPriorityMultiplicators();
        Task<PriorityMultiplicatorsDto> SetPriorityMultiplicators(double pm, double dm, double rm);
        Task<int> CleanUp(DateTime deleteAllBeforeTimestamp, string stationId = "");
    }
}
