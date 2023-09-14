using AutoMapper;
using EarthLat.Backend.Core.Compression;
using EarthLat.Backend.Core.Dtos;
using EarthLat.Backend.Core.Extensions;
using EarthLat.Backend.Core.Interfaces;
using EarthLat.Backend.Core.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Drawing;
using System.Net;

namespace EarthLat.Backend.Core.BusinessLogic
{
    /// <summary>
    /// Logic which handles all the logic cases for sundial management.
    /// </summary>
    public class SundialLogic : ISundialLogic
    {
        private readonly ILogger<ISundialLogic> logger;
        private readonly ITableStorageService _tableStorageService;
        private static readonly string PARTITIONKEY_DATE_PARSER = "yyyy-MM-dd";
        public static readonly string TIMESTAMP_DATE_PARSER = "yyyy-MM-dd HH:mm:ss";
        private static readonly int PIXEL_COUNT = 256;
        private static readonly int TOP_MARGIN = 100;
        private static readonly int LEFT_MARGIN = 100;
        private static readonly int SELECTED_HEIGHT = 150;
        private static readonly int SELECTED_WIDTH = 450;
        private static readonly int IMG_PIXEL_COUNT = SELECTED_HEIGHT * SELECTED_WIDTH;
        private static readonly double EXPONENT = 7831216639287 / 10000000000000;
        private static readonly int MAX_LAT_OFFSET = 180;
        private double priorityMultiplicator { get; set; } = 3;
        private double distanzeMultiplicator { get; set; } = 5;
        private double randomMultiplicator { get; set; } = 8;

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
            stations.RemoveAll(x => x.StationName == null);
            return stations;
        }

        /// <summary>
        /// Gets the lastest images of a station by station identifier.
        /// </summary>
        /// <param name="stationId">The unique station identifier.</param>
        /// <returns></returns>
        public async Task<Images> GetLatestCombinedImagesByIdAsync(string stationId)
        {

            var image = await GetLatestImagesById(stationId);
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

        public async Task<Images> GetLatestImagesById(string stationId)
        {
            _tableStorageService.Init("stations");
            var stations = await _tableStorageService.GetByFilterAsync<Station>($"RowKey eq '{stationId}'");
            var images = new List<Images>();
            if (stations.Any())
            {
                var currentStation = stations.First();
                _tableStorageService.Init("images");
                images = (await _tableStorageService.GetByFilterAsync<Images>($"PartitionKey eq '{stationId}' and RowKey eq '{currentStation.LastImageKey}'")).ToList();
            }
            return images.FirstOrDefault();
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
            var sunlitLikelyhood = await GetSunlitLikelihood(images.ImgTotal, station.RowKey);
            images.SunlitLikelihood = sunlitLikelyhood.ToString();
            await AddImage(station, images, status);
            await UpdateStatistics(station, status);
            return await GetRemoteConfig(station);
        }

        private async Task<float> GetSunlitLikelihood(byte[] image, string stationName)
        {
            try
            {
                var referenceByteArray = CompressionHelper.DecompressBytes(await GetReferenceImage(stationName));
                if (referenceByteArray != null)
                {
                    Bitmap refernceImage = GetBitmapFromBytes(referenceByteArray);
                    Bitmap compareImage = GetBitmapFromBytes(image);
                    var sunlitLikelyhood = CalculateSunlitLikelihood(refernceImage, compareImage);
                    return sunlitLikelyhood;
                }
                return .5f;
            }
            catch (Exception)
            {
                return .5f;
            }
        }
        private async Task<byte[]> GetReferenceImage(string stationName)
        {
            _tableStorageService.Init("stations");
            var query = $"RowKey eq '{stationName}'";
            var station = (await _tableStorageService
                .GetByFilterAsync<Station>(query))
                .FirstOrDefault();
            if (station == null)
            {
                return null;
            }
            return station.ReferenceImage;

        }
        private Bitmap GetBitmapFromBytes(byte[] image)
        {
            Bitmap bm;
            using (var ms = new MemoryStream(image))
            {
                bm = new Bitmap(ms);
            }
            return bm;
        }

        private float CalculateSunlitLikelihood(Bitmap refernceImage, Bitmap compareImage)
        {
            int[] referenceHistogram = GetHistogramFromBitmap(refernceImage);
            int[] compareHistogram = GetHistogramFromBitmap(compareImage);
            var referenceDictionary = GetSortedDictionary(referenceHistogram);
            var compareDictionary = GetSortedDictionary(compareHistogram);
            double total = 0;
            for (int i = 0; i < PIXEL_COUNT; i++)
            {
                var refernceElement = referenceDictionary.ElementAt(i);
                var compareElement = compareDictionary.ElementAt(i);
                var referenceProduct = refernceElement.Key * refernceElement.Value;
                var compareProduct = compareElement.Key * compareElement.Value;
                var difference = compareProduct - referenceProduct;
                var weightedDiffenerce = (difference) * GetWeightFromIndex(i);
                total += weightedDiffenerce;
            }
            total = total / (IMG_PIXEL_COUNT * PIXEL_COUNT);
            float sunlitLikelihood = (float)((total + 1) / 2);
            return sunlitLikelihood;
        }

        private int[] GetHistogramFromBitmap(Bitmap bitmap)
        {
            int[] histogram = new int[PIXEL_COUNT];
            for (int y = TOP_MARGIN; y < TOP_MARGIN + SELECTED_HEIGHT; y++)
            {
                for (int x = LEFT_MARGIN; x < LEFT_MARGIN + SELECTED_WIDTH; x++)
                {
                    Color pixel = bitmap.GetPixel(x, y);
                    int luma = (int)Math.Round(pixel.R * 0.2126 + pixel.G * 0.7152 + pixel.B * 0.0722);
                    histogram[luma]++;
                }
            }
            return histogram;
        }

        private Dictionary<int, int> GetSortedDictionary(int[] array)
        {
            var dictionary = array
                .Select((value, index) => new { value, index })
                .ToDictionary(pair => pair.index, pair => pair.value);
            var sortedDictionary = dictionary.OrderBy(x => x.Value).Reverse().ToDictionary(x => x.Key, x => x.Value);
            return sortedDictionary;
        }

        private float GetWeightFromIndex(int index)
        {
            var weight = (float)Math.Pow(Math.Log10(index + 10), EXPONENT);
            return weight;
        }

        private async Task AddImage(Station station, Images images, Status status)
        {
            station.LastImageKey = images.RowKey;
            _tableStorageService.Init("stations");
            station.ReferenceImage = await GetReferenceImage(station.RowKey);
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
            images.CpuTemparature = status.CpuTemparature.ToString();
            images.CameraTemparature = status.CameraTemparature.ToString();
            images.OutcaseTemparature = status.OutcaseTemparature.ToString();
            images.SwVersion = status.SwVersion;
            images.CaptureTime = status.CaptureTime;
            images.CaptureLat = status.CaptureLat;
            images.Brightness = status.Brightness.ToString();
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

        private async Task UpdateStatistics(Station station, Status status)
        {
            var (statistic, referenceDate) = await GetLatestStatisticAndDate(station, status);
            if (statistic == null)
            {
                await CreateNewStatisticEntry(station, status, referenceDate);
            }
            else
            {
                await UpdateStatisticEntry(statistic, status);
            }
        }

        private async Task<(Statistic, DateTime)> GetLatestStatisticAndDate(Station station, Status status)
        {
            var caputreDateString = status.CaptureLat.Substring(5, 11);
            var referenceDate = DateTime.ParseExact(caputreDateString, "dd MMM yyyy", System.Globalization.CultureInfo.InvariantCulture);
            _tableStorageService.Init("statistics");
            var result = await _tableStorageService.GetByFilterAsync<Statistic>($"PartitionKey eq '{station.RowKey}' and RowKey eq '{referenceDate.ToString(PARTITIONKEY_DATE_PARSER)}'");
            return (result.FirstOrDefault(), referenceDate);
        }

        private async Task CreateNewStatisticEntry(Station station, Status status, DateTime referenceDate)
        {
            var caputreDateString = status.CaptureLat.Substring(5, 20);
            var timestamp = DateTime.ParseExact(caputreDateString, "dd MMM yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
            var timestamps = new List<string> { timestamp.ToString(TIMESTAMP_DATE_PARSER) };
            var brightnessValues = new List<float> { status.Brightness };
            var temperatureValues = new List<float> { status.OutcaseTemparature };
            var statistic = new Statistic
            {
                PartitionKey = station.RowKey,
                RowKey = referenceDate.ToString(PARTITIONKEY_DATE_PARSER),
                UploadTimestamps = timestamps.ToBase64(),
                TemperatureValues = temperatureValues.ToBase64(),
                BrightnessValues = brightnessValues.ToBase64()
            };
            await _tableStorageService.AddAsync(statistic);
        }

        private async Task UpdateStatisticEntry(Statistic statistic, Status status)
        {
            var caputreDateString = status.CaptureLat.Substring(5, 20);
            var timestamp = DateTime.ParseExact(caputreDateString, "dd MMM yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
            var timestamps = statistic.UploadTimestamps.FromBase64<List<string>>();
            var brightnessValues = statistic.BrightnessValues.FromBase64<List<float>>();
            var temperatureValues = statistic.TemperatureValues.FromBase64<List<float>>();
            timestamps.Add(timestamp.ToString(TIMESTAMP_DATE_PARSER));
            brightnessValues.Add(status.Brightness);
            temperatureValues.Add(status.OutcaseTemparature);
            statistic.UploadTimestamps = timestamps.ToBase64();
            statistic.BrightnessValues = brightnessValues.ToBase64();
            statistic.TemperatureValues = temperatureValues.ToBase64();
            await _tableStorageService.UpdateAsync(statistic);
        }

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


        public async Task<string> SetPriorityOfStationById(string stationId, double priority)
        {
            _tableStorageService.Init("stations");
            var stations = await _tableStorageService.GetByFilterAsync<Station>($"RowKey eq '{stationId}'");
            if (stations.Any())
            {
                var currentStation = stations.First();
                currentStation.Priority = priority;
                await (_ = _tableStorageService.UpdateAsync(currentStation));
                return "Priority of Station changed";
            }
            return "Station not found";
        }

        public async Task<PriorityMultiplicatorsDto> GetPriorityMultiplicators()
        {
            return new PriorityMultiplicatorsDto(priorityMultiplicator, distanzeMultiplicator, randomMultiplicator);
        }

        public async Task<PriorityMultiplicatorsDto> SetPriorityMultiplicators(double pm, double dm, double rm)
        {
            priorityMultiplicator = pm;
            distanzeMultiplicator = dm;
            randomMultiplicator = rm;

            return new PriorityMultiplicatorsDto(priorityMultiplicator, distanzeMultiplicator, randomMultiplicator);
        }

        public async Task<string> SetIsOnlineOfStationById(string stationId)
        {
            _tableStorageService.Init("stations");
            var stations = await _tableStorageService.GetByFilterAsync<Station>($"RowKey eq '{stationId}'");
            if (stations.Any())
            {
                var currentStation = stations.First();
                currentStation.IsOnline = !currentStation.IsOnline;
                await(_ = _tableStorageService.UpdateAsync(currentStation));
                if (currentStation.IsOnline)
                {
                    return $"{currentStation.StationName} is now online";
                }
                else
                {
                    return $"{currentStation.StationName} is now offline";
                }
                
            }
            return "Station not found";
        }
    }
}
