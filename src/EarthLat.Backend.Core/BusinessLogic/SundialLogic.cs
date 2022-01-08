using AutoMapper;
using EarthLat.Backend.Core.Extensions;
using EarthLat.Backend.Core.Interfaces;
using EarthLat.Backend.Core.Models;
using Microsoft.Extensions.Logging;

namespace EarthLat.Backend.Core.BusinessLogic
{
    public class SundialLogic : ISundialLogic
    {
        private readonly ILogger<ISundialLogic> logger;
        private readonly ITableStorageService _tableStorageService;

        public SundialLogic(ILogger<ISundialLogic> logger, 
            ITableStorageService tableStorageService, 
            IMapper mapper)
        {
            this.logger = logger;
            _tableStorageService = tableStorageService ?? throw new ArgumentNullException(nameof(tableStorageService));
        }

        public async Task<IEnumerable<Station>> GetAllStationsAsync()
        {
            _tableStorageService.Init("stations");
            return await _tableStorageService.GetAllAsync<Station>();
        }

        /// <summary>
        /// Gets the lastest images of a station by station identifier.
        /// </summary>
        /// <param name="stationId">The unique station identifier.</param>
        /// <returns></returns>
        public async Task<Images> GetLatestImagesByIdAsync(string stationId)
        {
            _tableStorageService.Init("stations");
            var station = await _tableStorageService.GetByFilterAsync<Station>($"PartitionKey eq '{stationId}'");
            var images = new List<Images>();
            if (station.Any())
            {
                var currentStation = station.First();
                _tableStorageService.Init("images");
                images = (await _tableStorageService.GetByFilterAsync<Images>($"PartitionKey eq '{stationId}' and RowKey eq '{currentStation.LastImageKey}'")).ToList();
            }

            return images.FirstOrDefault();
        }

        /// <summary>
        /// Gets the lastest images of a station by angles.
        /// </summary>
        /// <param name="longitude">The longitude.</param>
        /// <param name="latitude">The latitude.</param>
        /// <returns></returns>
        public async Task<Images> GetLatestImagesByAnglesAsync(string longitude, string latitude)
        {
            _tableStorageService.Init("stations");
            var station = await _tableStorageService.GetByFilterAsync<Station>($"RowKey eq '{longitude}_{latitude}'");
            var images = new List<Images>();
            if (station.Any())
            {
                var currentStation = station.First();
                _tableStorageService.Init("images");
                images = (await _tableStorageService.GetByFilterAsync<Images>($"RowKey eq '{currentStation.LastImageKey}'")).ToList();
            }

            return images.FirstOrDefault();
        }

        /// <summary>
        /// Gets a station by the unique station id.
        /// </summary>
        /// <param name="stationId">The station identifier.</param>
        /// <returns></returns>
        public async Task<Station> GetStationByIdAsync(string stationId)
        {
            _tableStorageService.Init("stations");
            
            var result = await _tableStorageService.GetByFilterAsync<Station>($"PartitionKey eq '{stationId}'");
            return result.FirstOrDefault();
        }

        /// <summary>
        /// Gets a station by the angles (longitude, latitude).
        /// </summary>
        /// <param name="longitude">The longitude.</param>
        /// <param name="latitude">The latitude.</param>
        /// <returns></returns>
        public async Task<Station> GetStationByAnglesAsync(string longitude, string latitude)
        {
            _tableStorageService.Init("stations");
            var result = await _tableStorageService.GetByFilterAsync<Station>($"RowKey eq '{longitude}_{latitude}'");
            return result.FirstOrDefault();
        }

        /// <summary>
        /// Adds a new or updates an existing station entry plus the images of the station. 
        /// </summary>
        /// <param name="station">The station.</param>
        /// <param name="images">The images.</param>
        /// <returns></returns>
        public async Task<RemoteConfig> AddAsync(Station station, Images images)
        {
            images.SetImagesRowKey();
            station.LastImageKey = images.RowKey;
            station.SetStationRowKey();

            _tableStorageService.Init("stations");
            await _tableStorageService.AddOrUpdateAsync(station);

            _tableStorageService.Init("images");
            await _tableStorageService.AddAsync(images);

            var config = await GetRemoteConfigById(station.PartitionKey);

            if (config is null)
            {
                var defaultConfig = new RemoteConfig()
                {
                    IsCamOffline = false,
                    IsSeries = false,
                    IsZoomDrawRect = false,
                    IsZoomMove = false,
                    Period = new TimeSpan(0, 2, 0),
                    ZoomCenterPerCX = 0,
                    ZoomCenterPerCy = 0,
                };

                config = await AddOrUpdateRemoteConfigAsync(defaultConfig, station.PartitionKey);
            }

            return config;
        }

        /// <summary>
        /// Adds a new or updates an existing station remote config entry. 
        /// </summary>
        /// <param name="remoteConfig">The remote config to add or udpate.</param>
        /// <param name="stationId">The station id.</param>
        /// <returns></returns>
        public async Task<RemoteConfig> AddOrUpdateRemoteConfigAsync(RemoteConfig remoteConfig, string stationId)
        {
            remoteConfig.SetRemoteConfigKeys(stationId);
            _tableStorageService.Init("remoteconfigs");
            await _tableStorageService.AddOrUpdateAsync(remoteConfig);
            return remoteConfig;
        }

        /// <summary>
        /// Get a station remote config by a station id.
        /// </summary>
        /// <param name="stationId">The station id.</param>
        /// <returns></returns>
        public async Task<RemoteConfig?> GetRemoteConfigById(string stationId)
        {
            _tableStorageService.Init("remoteconfigs");
            var result = await _tableStorageService.GetByFilterAsync<RemoteConfig>($"PartitionKey eq '{stationId}' and RowKey eq '{stationId}{ModelsExtensions.RemoteConfigRowKeyPostfix}'");
            return result.FirstOrDefault();
        }
    }
}
