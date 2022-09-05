using EarthLat.Backend.Core.Compression;
using EarthLat.Backend.Core.Dtos;
using EarthLat.Backend.Core.Extensions;
using EarthLat.Backend.Core.Interfaces;
using EarthLat.Backend.Core.JWT;
using EarthLat.Backend.Core.Models;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace EarthLat.Backend.Core.BusinessLogic
{
    public class StatisticService
    {
        private readonly ILogger<StatisticService> logger;
        private readonly ITableStorageService _tableStorageService;
        private readonly JwtGenerator jwtGenerator;
        private readonly int COORDINATES_LENGTH = 73;//3 days + 1 hour
        private readonly object _lock = new();

        public StatisticService(ILogger<StatisticService> logger,
            ITableStorageService tableStorageService,
            JwtGenerator jwtGenerator)
        {
            this.logger = logger;
            _tableStorageService = tableStorageService ?? throw new ArgumentNullException(nameof(tableStorageService));
            this.jwtGenerator = jwtGenerator;
        }

        public async Task<string> AuthenticateAsync(UserCredentials credentials)
        {
            string query = $"Name eq '{credentials.Username}' and Password eq '{credentials.Password}'";
            Statistic statistic = null;
            List<User> users = new();
            lock (_lock)
            {
                _tableStorageService.Init("users");
                users = (_tableStorageService.GetByFilterAsync<User>(query).Result).ToList();
            }
            var user = users.FirstOrDefault();
            List<Station> stations = null;
            lock (_lock)
            {
                _tableStorageService.Init("stations");
                stations = (_tableStorageService.GetAllAsync<Station>().Result).ToList();
            }
            if (user == null || stations == null)
                return null;
            var userStation = stations.Find(x => x.RowKey == user.PartitionKey);
            return jwtGenerator.GenerateJWT(user);
        }

        public async Task<List<BarChartDto>> GetSendTimesAsync
            (JwtValidator validator, string referenceDate, int timezoneOffset)
        {
            List<BarChartDto> dtos = new();
            var stations = await GetAccessibleStations(validator);
            foreach (var station in stations)
            {
                var query = $"PartitionKey eq '{station.Item1}' and RowKey eq '{referenceDate}'";
                Statistic statistic = null;
                lock (_lock)
                {
                    _tableStorageService.Init("statistics");
                    statistic = (_tableStorageService
                        .GetByFilterAsync<Statistic>(query).Result)
                        .FirstOrDefault();
                }
                if (statistic == null)
                    continue;
                var timestamps = statistic.UploadTimestamps.FromBase64<List<long>>();
                var startDate = timestamps.FirstOrDefault().GetDateTime();
                var endDate = timestamps.LastOrDefault().GetDateTime();
                dtos.Add(new BarChartDto
                {
                    Name = station.Item2,
                    Start = startDate.GetSecondsSinceStartTime(timezoneOffset, referenceDate),
                    End = endDate.GetSecondsSinceStartTime(timezoneOffset, referenceDate)
                });
            }
            return dtos;
        }

        private async Task<List<(string, string)>> GetAccessibleStations(JwtValidator validator)
        {
            if (validator.Privilege == 0)
            {
                lock (_lock)
                {
                    _tableStorageService.Init("stations");
                    return (_tableStorageService.GetAllAsync<Station>().Result).Where(x => x.StationName != null).Select(x => (x.RowKey, x.StationName)).ToList();
                }
            }
            string query = $"RowKey eq '{validator.Station}'";
            List<Station> stations = null;
            lock (_lock)
            {
                _tableStorageService.Init("stations");
                stations = (_tableStorageService.GetByFilterAsync<Station>(query).Result).ToList();
            }
            if (stations == null)
                return new List<(string, string)>();
            var station = stations.FirstOrDefault();
            return new List<(string, string)> { new(station.RowKey, station.StationName) };
        }


        public async Task<List<LineChartDto>> GetTemperatrueValuesPerHourAsync
            (JwtValidator validator, string referenceDate, int timezoneOffset)
        {
            List<LineChartDto> dtos = new();
            var stations = await GetAccessibleStations(validator);
            foreach (var station in stations)
            {
                var query = $"PartitionKey eq '{station.Item1}' and RowKey eq '{referenceDate}'";
                Statistic statistic = null;
                lock (_lock)
                {
                    _tableStorageService.Init("statistics");
                    statistic = (_tableStorageService
                        .GetByFilterAsync<Statistic>(query).Result)
                        .FirstOrDefault();
                }
                if (statistic == null)
                    continue;
                var timestamps = statistic.UploadTimestamps.FromBase64<List<long>>()
                    .Select(x => x.GetDateTime().AddMinutes(timezoneOffset))
                    .ToArray();
                var temperatureValues = statistic.TemperatureValues.FromBase64<List<float>>()
                    .ToArray();
                dtos.Add(new LineChartDto
                {
                    Name = station.Item2,
                    Values = GetCoordinatesFromFloatArray(timestamps, temperatureValues, referenceDate)
                });
            }
            return dtos;
        }

        private double[] GetCoordinatesFromFloatArray(DateTime[] timestamps, float[] floatValues, string referenceDate)
        {
            var startDate = referenceDate.GetStartDate().AddMinutes(-30);
            var tempCoordinates = new List<double>[COORDINATES_LENGTH];
            for (int i = 0; i < COORDINATES_LENGTH; i++)
            {
                tempCoordinates[i] = new List<double>();
            }
            for (int i = 0; i < timestamps.Length; i++)
            {
                var index = (int)timestamps[i].Subtract(startDate).TotalHours;
                tempCoordinates[index].Add(floatValues[i]);
            }
            var coordinates = new double[COORDINATES_LENGTH];
            for (int i = 0; i < COORDINATES_LENGTH; i++)
            {
                if (tempCoordinates[i].Any())
                {
                    coordinates[i] = tempCoordinates[i].Average();
                }
                else
                {
                    coordinates[i] = 0;
                }
            }
            return coordinates;
        }
        public async Task<List<LineChartDto>> GetImagesPerHourAsync
            (JwtValidator validator, string referenceDate, int timezoneOffset)
        {
            List<LineChartDto> dtos = new();
            var stations = await GetAccessibleStations(validator);
            foreach (var station in stations)
            {
                var query = $"PartitionKey eq '{station.Item1}' and RowKey eq '{referenceDate}'";
                Statistic statistic = null;
                lock (_lock)
                {
                    _tableStorageService.Init("statistics");
                    statistic = (_tableStorageService
                        .GetByFilterAsync<Statistic>(query).Result)
                        .FirstOrDefault();
                }
                if (statistic == null)
                    continue;
                var timestamps = statistic.UploadTimestamps.FromBase64<List<long>>()
                    .Select(x => x.GetDateTime().AddMinutes(
                        timezoneOffset))
                    .ToArray();
                dtos.Add(new LineChartDto
                {
                    Name = station.Item2,
                    Values = GetCoordinatesFromTimestamps(timestamps, referenceDate)
                });
            }
            return dtos;
        }
        private double[] GetCoordinatesFromTimestamps(DateTime[] timestamps, string referenceDate)
        {
            var startDate = referenceDate.GetStartDate().AddMinutes(-30);
            var coordinates = new double[COORDINATES_LENGTH];
            foreach (var timestamp in timestamps)
            {
                var index = (int)timestamp.Subtract(startDate).TotalHours;
                coordinates[index]++;
            }
            return coordinates;
        }

        public async Task<List<LineChartDto>> GetBrightnessValuesPerHourAsync
            (JwtValidator validator, string referenceDate, int timezoneOffset)
        {
            List<LineChartDto> dtos = new();
            var stations = await GetAccessibleStations(validator);
            foreach (var station in stations)
            {
                var query = $"PartitionKey eq '{station.Item1}' and RowKey eq '{referenceDate}'";
                Statistic statistic = null;
                lock (_lock)
                {
                    _tableStorageService.Init("statistics");
                    statistic = (_tableStorageService
                        .GetByFilterAsync<Statistic>(query).Result)
                        .FirstOrDefault();
                }
                if (statistic == null)
                    continue;
                var timestamps = statistic.UploadTimestamps.FromBase64<List<long>>()
                    .Select(x => x.GetDateTime().AddMinutes(timezoneOffset))
                    .ToArray();
                var brightnessValues = statistic.BrightnessValues.FromBase64<List<float>>()
                    .ToArray();
                dtos.Add(new LineChartDto
                {
                    Name = station.Item2,
                    Values = GetCoordinatesFromFloatArray(timestamps, brightnessValues, referenceDate)
                });
            }
            return dtos;
        }
    }
}
