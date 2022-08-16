using AutoMapper;
using EarthLat.Backend.Core.Compression;
using EarthLat.Backend.Core.Dtos;
using EarthLat.Backend.Core.Extensions;
using EarthLat.Backend.Core.Interfaces;
using EarthLat.Backend.Core.Models;
using Microsoft.Extensions.Logging;
using System.Runtime.Serialization.Formatters.Binary;

namespace EarthLat.Backend.Core.BusinessLogic
{
    /// <summary>
    /// Logic which handles all the logic cases for sundial management.
    /// </summary>
    public class SundialLogic : ISundialLogic
    {
        private readonly ILogger<ISundialLogic> logger;
        private readonly ITableStorageService _tableStorageService;
        private readonly string PARTITIONKEY_DATE_PARSER = "yyyy-MM-dd";

        public SundialLogic(ILogger<ISundialLogic> logger,
            ITableStorageService tableStorageService)
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

            if (image?.ImgTotal is not null)
            {
                if (image.ImgTotalv2 is not null)
                {
                    image.ImgTotal = Combine(image.ImgTotal, image.ImgTotalv2);
                }
                image.ImgTotal = CompressionHelper.DecompressBytes(image.ImgTotal);
            }

            if (image?.ImgDetail is not null)
            {
                if (image.ImgDetailv2 is not null)
                {
                    image.ImgDetail = Combine(image.ImgDetail, image.ImgDetailv2);
                }
                image.ImgDetail = CompressionHelper.DecompressBytes(image.ImgDetail);
            }

            return image;
        }

        private static byte[] Combine(byte[] first, byte[] second)
        {
            return first.Concat(second).ToArray();
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
        public async Task<RemoteConfig> AddAsync(Station station, Images images, Status status)
        {
            images.SetImagesRowKey();
            await AddImage(station, images, status);
            //await UpdateStatistics(station, images, status);//the float values are in different formats. I need to see them in Detail to parse them depending on the sending station
            return await GetRemoteConfig(station);
        }

        private async Task AddImage(Station station, Images images, Status status)
        {
            station.LastImageKey = images.RowKey;
            _tableStorageService.Init("stations");
            await _tableStorageService.AddOrUpdateAsync(station);
            SetImagesPropertiesFromStatus(images, status);
            if (images?.ImgDetail is not null)
            {
                CompressImagesDetail(images);
            }
            if (images?.ImgTotal is not null)
            {
                CompressImagesTotal(images);
            }
            _tableStorageService.Init("images");
            await _tableStorageService.AddAsync(images);
        }

        private void SetImagesPropertiesFromStatus(Images images, Status status)
        {
            images.CpuTemparature = status.CpuTemparature;
            images.CameraTemparature = status.CameraTemparature;
            images.OutcaseTemparature = status.OutcaseTemparature;
            images.SwVersion = status.SwVersion;
            images.CaptureTime = status.CaptureTime;
            images.CaptureLat = status.CaptureLat;
            images.Brightness = status.Brightness;
            images.Sunny = status.Sunny;
            images.Cloudy = status.Cloudy;
            images.Night = status.Night;
        }

        private void CompressImagesDetail(Images images)
        {
            images.ImgDetailKb = images.ImgDetail.Length;
            images.ImgDetail = CompressionHelper.CompressBytes(images.ImgDetail);
            images.ImgDetailCompressedKb = images.ImgDetail.Length;
            if (images.ImgDetailCompressedKb > 62000)
            {
                SplitImagesDetail(images);
            }
        }

        private void SplitImagesDetail(Images images)
        {
            var temp = images.ImgDetail;
            var firstPart = images.ImgDetailCompressedKb / 2;
            var secondPart = images.ImgDetailCompressedKb - firstPart;
            images.ImgDetail = temp.Take(firstPart).ToArray();
            images.ImgDetailv2 = temp.Skip(firstPart).Take(secondPart).ToArray();
        }

        private void CompressImagesTotal(Images images)
        {
            images.ImgTotalKb = images.ImgTotal.Length;
            images.ImgTotal = CompressionHelper.CompressBytes(images.ImgTotal);
            images.ImgTotalCompressedKb = images.ImgTotal.Length;

            if (images.ImgTotalCompressedKb > 62000)
            {
                SplitImagesTotal(images);
            }
        }

        private void SplitImagesTotal(Images images)
        {
            var temp = images.ImgTotal;
            var firstPart = images.ImgTotalCompressedKb / 2;
            var secondPart = images.ImgTotalCompressedKb - firstPart;
            images.ImgTotal = temp.Take(firstPart).ToArray();
            images.ImgTotalv2 = temp.Skip(firstPart).Take(secondPart).ToArray();
        }

        //private async Task UpdateStatistics(Station station, Images images, Status status)
        //{
        //    var (statistic, referenceDate) = await GetLatestStatisticAndDate(station, status);
        //    if (statistic == null)
        //    {
        //        await CreateNewStatisticEntry(station, images, status, referenceDate);
        //    }
        //    else
        //    {
        //        await UpdateStatisticEntry(statistic, images, status);
        //    }
        //}

        private async Task<(Statistic,DateTime)> GetLatestStatisticAndDate(Station station, Status status)
        {
            var caputreDateString = status.CaptureLat.Substring(5, 11);
            var referenceDate = DateTime.ParseExact(caputreDateString, "dd MMM yyyy", System.Globalization.CultureInfo.InvariantCulture);
            _tableStorageService.Init("statistics");
            var result = await _tableStorageService.GetByFilterAsync<Statistic>($"PartitionKey eq '{station.RowKey}' and RowKey eq '{referenceDate.ToString(PARTITIONKEY_DATE_PARSER)}'");
            return (result.FirstOrDefault(),referenceDate);
        }

        //private async Task CreateNewStatisticEntry(Station station, Images images, Status status, DateTime referenceDate)
        //{
        //    var timestamps = new List<long> { long.Parse(images.RowKey) };
        //    var brightnessValues = new List<float> { float.Parse(status.Brightness) };
        //    var temperatureValues = new List<float> { float.Parse(status.OutcaseTemparature.GetParsableNumberString()) };
        //    var statistic = new Statistic
        //    {
        //        PartitionKey = station.RowKey,
        //        RowKey = referenceDate.ToString(PARTITIONKEY_DATE_PARSER),
        //        UploadTimestamps = timestamps.ToBase64(),
        //        TemperatureValues = temperatureValues.ToBase64(),
        //        BrightnessValues = brightnessValues.ToBase64()
        //    };
        //    await _tableStorageService.AddAsync(statistic);
        //}

        //private async Task UpdateStatisticEntry(Statistic statistic, Images images, Status status)
        //{
        //    var timestamps = statistic.UploadTimestamps.FromBase64<List<long>>();
        //    var brightnessValues = statistic.BrightnessValues.FromBase64<List<float>>();
        //    var temperatureValues = statistic.TemperatureValues.FromBase64<List<float>>();
        //    timestamps.Add(long.Parse(images.RowKey));
        //    brightnessValues.Add(float.Parse(status.Brightness));
        //    temperatureValues.Add(float.Parse(status.OutcaseTemparature.GetParsableNumberString()));
        //    statistic.UploadTimestamps = timestamps.ToBase64();
        //    statistic.BrightnessValues = brightnessValues.ToBase64();
        //    statistic.TemperatureValues = temperatureValues.ToBase64();
        //    await _tableStorageService.UpdateAsync(statistic);
        //}

        private async Task<RemoteConfig> GetRemoteConfig(Station station)
        {
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
            var partitionKeyDate = timestamp.ToString(PARTITIONKEY_DATE_PARSER);
            IEnumerable<Images>? result = string.IsNullOrWhiteSpace(stationId)
                ? await _tableStorageService.GetByFilterAsync<Images>($"Timestamp le datetime'{timestamp:yyyy-MM-ddThh:mm:ssZ}'")
                : await _tableStorageService.GetByFilterAsync<Images>($"PartitionKey eq '{stationId}' and Timestamp lt datetime'{timestamp:yyyy-MM-ddThh:mm:ssZ}'");

            int count = 0;
            foreach (Images image in result)
            {
                await _tableStorageService.DeleteAsync<Images>(image.PartitionKey, image.RowKey);
                count++;
            }
            return count;
        }
    }
}
