using AutoMapper;
using EarthLat.Backend.Core.Compression;
using EarthLat.Backend.Core.Extensions;
using EarthLat.Backend.Core.Interfaces;
using EarthLat.Backend.Core.Models;
using Microsoft.Extensions.Logging;

namespace EarthLat.Backend.Core.BusinessLogic
{
    /// <summary>
    /// Logic which handles all the logic cases for sundial management.
    /// </summary>
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
            var stations = (await _tableStorageService.GetAllAsync<Station>()).ToList();
            stations.RemoveAll(x => !x.IsActive);
            return stations;
        }

        /// <summary>
        /// Gets the lastest images of a station by station identifier.
        /// </summary>
        /// <param name="stationId">The unique station identifier.</param>
        /// <returns></returns>
        public async Task<Images> GetLatestImagesByIdAsync(string stationId)
        {
            _tableStorageService.Init("stations");
            var station = await _tableStorageService.GetByFilterAsync<Station>($"RowKey eq '{stationId}'");
            var images = new List<Images>();
            if (station.Any())
            {
                var currentStation = station.First();
                _tableStorageService.Init("images");
                images = (await _tableStorageService.GetByFilterAsync<Images>($"PartitionKey eq '{stationId}' and RowKey eq '{currentStation.LastImageKey}'")).ToList();
            }
            var image = images.FirstOrDefault();

            if(image?.ImgTotal is not null)
            {
                image.ImgTotal = CompressionHelper.DecompressBytes(image.ImgTotal);
            }

            if (image?.ImgDetail is not null)
            {
                image.ImgDetail = CompressionHelper.DecompressBytes(image.ImgDetail);
            }

            return image;
        }

        /// <summary>
        /// Gets a station by the unique station id.
        /// </summary>
        /// <param name="stationId">The station identifier.</param>
        /// <returns></returns>
        public async Task<Station> GetStationByIdAsync(string stationId)
        {
            _tableStorageService.Init("stations");

            var result = await _tableStorageService.GetByFilterAsync<Station>($"RowKey eq '{stationId}'");
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

            _tableStorageService.Init("stations");
            await _tableStorageService.AddOrUpdateAsync(station);

            _tableStorageService.Init("images");

            if (images?.ImgDetail is not null)
            {
                images.ImgDetail = CompressionHelper.CompressBytes(images.ImgDetail);
            }
            if (images?.ImgTotal is not null)
            {
                images.ImgTotal = CompressionHelper.CompressBytes(images.ImgTotal);
            }
 
            await _tableStorageService.AddAsync(images);

            var config = await GetRemoteConfigById(station.RowKey);

            if (config is null)
            {
                var defaultConfig = new RemoteConfig()
                {
                    IsCamOffline = false,
                    IsSeries = false,
                    IsZoomDrawRect = false,
                    IsZoomMove = false,
                    Period = 2,
                    ZoomCenterPerCX = 0,
                    ZoomCenterPerCy = 0,
                };

                config = await AddOrUpdateRemoteConfigAsync(defaultConfig, station.RowKey);
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

        /// <summary>
        /// Clean up images store.
        /// </summary>
        /// <param name="deleteAllBeforeTimestamp">All entries before and on this date will be deleted.</param>
        /// <param name="stationId">Optional stationId, to delete images of a specific station.</param>
        /// <returns></returns>
        public async Task<int> CleanUp(DateTime deleteAllBeforeTimestamp, string stationId = "")
        {
            var timestamp = new DateTimeOffset(deleteAllBeforeTimestamp.AddDays(1));
            _tableStorageService.Init("images");
            int count = 0;

            IEnumerable<Images>? result = string.IsNullOrWhiteSpace(stationId)
                ? await _tableStorageService.GetByFilterAsync<Images>($"Timestamp le datetime'{timestamp:yyyy-MM-ddThh:mm:ssZ}'")
                : await _tableStorageService.GetByFilterAsync<Images>($"PartitionKey eq '{stationId}' and Timestamp lt datetime'{timestamp:yyyy-MM-ddThh:mm:ssZ}'");

            foreach (Images image in result)
            {
                await _tableStorageService.DeleteAsync<Images>(image.PartitionKey, image.RowKey);
                count++;
            }

            return count;
        }
    }
}
