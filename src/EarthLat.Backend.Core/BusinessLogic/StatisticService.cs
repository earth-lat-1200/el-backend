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

        public StatisticService(ILogger<StatisticService> logger,
            ITableStorageService tableStorageService,
            JwtGenerator jwtGenerator)
        {
            this.logger = logger;
            _tableStorageService = tableStorageService ?? throw new ArgumentNullException(nameof(tableStorageService));
            this.jwtGenerator = jwtGenerator;
        }

        public async Task<UserDto> AuthenticateAsync(UserCredentials credentials)
        {
            _tableStorageService.Init("users");
            string query = $"Name eq '{credentials.Username}' and Password eq '{credentials.Password}'";
            var users = (await _tableStorageService.GetByFilterAsync<User>(query));
            var user = users.FirstOrDefault();
            _tableStorageService.Init("stations");
            var stations = (await _tableStorageService.GetAllAsync<Station>()).ToList();
            if (user == null || stations == null)
                return null;
            var userStation = stations.Find(x => x.RowKey == user.PartitionKey);
            return new UserDto
            {
                Name = user.Name,
                Privilege = user.Privilege,
                Token = jwtGenerator.GenerateJWT(user),
                StationName = (userStation != null) ? userStation.StationName : null
            };
        }

        public async Task<List<BarChartDto>> GetSendTimesAsync
            (JwtValidator validator, string referenceDate, int timezoneOffset)
        {
            List<BarChartDto> dtos = new();
            var stations = await GetAccessibleStations(validator);
            _tableStorageService.Init("statistics");
            foreach (var station in stations)
            {
                var query = $"PartitionKey eq '{station.Item1}' and RowKey eq '{referenceDate}'";
                var statistic = (await _tableStorageService
                    .GetByFilterAsync<Statistic>(query))
                    .FirstOrDefault();
                if (statistic == null)
                    continue;
                var timestamps = statistic.UploadTimestamps.FromBase64<List<long>>();
                var startDate = timestamps.FirstOrDefault().GetDateTimeFromTimestamp();
                var endDate = timestamps.LastOrDefault().GetDateTimeFromTimestamp();
                dtos.Add(new BarChartDto
                {
                    Name = station.Item2,
                    Start = startDate.GetSecondsStartTime(timezoneOffset),
                    End = endDate.GetSecondsStartTime(timezoneOffset)
                });
            }
            return dtos;
        }

        private async Task<List<(string, string)>> GetAccessibleStations(JwtValidator validator)
        {
            _tableStorageService.Init("stations");
            if (validator.Privilege == 0)
            {
                return (await _tableStorageService.GetAllAsync<Station>()).Where(x => x.StationName != null).Select(x => (x.RowKey, x.StationName)).ToList();
            }
            string query = $"RowKey eq '{validator.Station}'";
            var stations = (await _tableStorageService
                .GetByFilterAsync<Station>(query));
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
            _tableStorageService.Init("statistics");
            foreach (var station in stations)
            {
                var query = $"PartitionKey eq '{station.Item1}' and RowKey eq '{referenceDate}'";
                var statistic = (await _tableStorageService
                    .GetByFilterAsync<Statistic>(query))
                    .FirstOrDefault();
                if (statistic == null)
                    continue;
                var timestamps = statistic.UploadTimestamps.FromBase64<List<long>>()
                    .Select(x => x.GetDateTimeFromTimestamp().AddMinutes(timezoneOffset))
                    .ToArray();
                var temperatureValues = statistic.TemperatureValues.FromBase64<List<float>>()
                    .ToArray();
                dtos.Add(new LineChartDto
                {
                    Name = station.Item2,
                    Values = GetCoordinatesFromFloatArray(timestamps, temperatureValues)
                });
            }
            return dtos;
        }

        private double[] GetCoordinatesFromFloatArray(DateTime[] timestamps, float[] floatValues)
        {
            var startDate = timestamps[0].Date.AddDays(-1).AddMinutes(-30);
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
            _tableStorageService.Init("statistics");
            foreach (var station in stations)
            {
                var query = $"PartitionKey eq '{station.Item1}' and RowKey eq '{referenceDate}'";
                var statistic = (await _tableStorageService
                    .GetByFilterAsync<Statistic>(query))
                    .FirstOrDefault();
                if (statistic == null)
                    continue;
                var timestamps = statistic.UploadTimestamps.FromBase64<List<long>>()
                    .Select(x => x.GetDateTimeFromTimestamp().AddMinutes(
                        timezoneOffset))
                    .ToArray();
                dtos.Add(new LineChartDto
                {
                    Name = station.Item2,
                    Values = GetCoordinatesFromTimestamps(timestamps)
                });
            }
            return dtos;
        }
        private double[] GetCoordinatesFromTimestamps(DateTime[] timestamps)
        {
            var startDate = timestamps[0].Date.AddDays(-1).AddMinutes(-30);
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
            _tableStorageService.Init("statistics");
            foreach (var station in stations)
            {
                var query = $"PartitionKey eq '{station.Item1}' and RowKey eq '{referenceDate}'";
                var statistic = (await _tableStorageService
                    .GetByFilterAsync<Statistic>(query))
                    .FirstOrDefault();
                if (statistic == null)
                    continue;
                var timestamps = statistic.UploadTimestamps.FromBase64<List<long>>()
                    .Select(x => x.GetDateTimeFromTimestamp().AddMinutes(timezoneOffset))
                    .ToArray();
                var brightnessValues = statistic.BrightnessValues.FromBase64<List<float>>()
                    .ToArray();
                dtos.Add(new LineChartDto
                {
                    Name = station.Item2,
                    Values = GetCoordinatesFromFloatArray(timestamps, brightnessValues)
                });
            }
            return dtos;
        }
    }
}
