using EarthLat.Backend.Core.Compression;
using EarthLat.Backend.Core.Dtos;
using EarthLat.Backend.Core.Extensions;
using EarthLat.Backend.Core.Interfaces;
using EarthLat.Backend.Core.JWT;
using EarthLat.Backend.Core.Models;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Drawing;
using System.Drawing.Imaging;

namespace EarthLat.Backend.Core.BusinessLogic
{
    public class StatisticService
    {
        private readonly ILogger<StatisticService> logger;
        private readonly ITableStorageService _tableStorageService;
        private readonly JwtGenerator generator;
        private readonly int COORDINATES_LENGTH = 25;

        public StatisticService(ILogger<StatisticService> logger,
            ITableStorageService tableStorageService,
            JwtGenerator jwtGenerator)
        {
            this.logger = logger;
            _tableStorageService = tableStorageService ?? throw new ArgumentNullException(nameof(tableStorageService));
            this.generator = jwtGenerator;
        }

        public async Task<UserDto> Authenticate(UserCredentials credentials)
        {
            _tableStorageService.Init("users");
            string query = $"Name eq '{credentials.Username}' and Password eq '{credentials.Password}'";
            var user = (await _tableStorageService.GetByFilterAsync<User>(query)).FirstOrDefault();
            _tableStorageService.Init("stations");
            var stations = (await _tableStorageService.GetAllAsync<Station>()).ToList();
            if (user == null || stations == null)
                return null;
            var userStation = stations.Find(x => x.RowKey == user.PartitionKey);
            return new UserDto
            {
                Name = user.Name,
                Privilege = user.Privilege,
                Token = generator.GenerateJWT(user),
                StationName = (userStation != null) ? userStation.StationName : null
            };
        }

        public async Task<List<BarChartDto>> GetSendTimes(string userId, string referenceDateTime, int timezoneOffset)
        {
            var user = await GetUserById(userId);
            var stations = await GetAccessibleStations(user);
            var (startDate, endDate) = GetStartAndEndDate(referenceDateTime, timezoneOffset);
            var toReturn = new List<BarChartDto>();
            _tableStorageService.Init("images");
            foreach (var station in stations)
            {
                //Console.WriteLine(station.Item2);
                var query = $"PartitionKey eq '{station.Item1}' " +
                $"and Timestamp ge datetime'{startDate:yyyy-MM-dd}T{startDate:HH:mm:ss}.000Z' " +
                $"and Timestamp lt datetime'{endDate:yyyy-MM-dd}T{endDate:HH:mm:ss}.000Z'";
                //var query = $"PartitionKey eq '{station.Item1}' " +
                //    "and RowKey lt '02'";
                var images = (await _tableStorageService
                    .GetByFilterAsync<Images>(query))
                    .OrderBy(x => x.Timestamp)
                    .ToList();
                if (images == null || images.Count() <= 1)
                    continue;
                var startTimeStamp = images.FirstOrDefault().Timestamp.Value.DateTime;
                var endTimeStamp = images.LastOrDefault().Timestamp.Value.DateTime;
                toReturn.Add(new BarChartDto
                {
                    Name = station.Item2,
                    Start = startTimeStamp.GetAdjustedSecondsSinceMidnight(timezoneOffset),
                    End = endTimeStamp.GetAdjustedSecondsSinceMidnight(timezoneOffset)
                });
            }
            return toReturn;
        }

        private async Task<User> GetUserById(string userId)
        {
            _tableStorageService.Init("users");
            string query = $"RowKey eq '{userId}'";
            var user = (await _tableStorageService
                .GetByFilterAsync<User>(query))
                .FirstOrDefault();
            return user;
        }

        private async Task<List<(string, string)>> GetAccessibleStations(User user)
        {
            _tableStorageService.Init("stations");
            if (user.Privilege == 0)
            {
                return (await _tableStorageService.GetAllAsync<Station>()).Where(x => x.StationName != null).Select(x => (x.RowKey, x.StationName)).ToList();
            }
            string query = $"RowKey eq '{user.PartitionKey}'";
            var station = (await _tableStorageService
                .GetByFilterAsync<Station>(query))
                .FirstOrDefault();
            return new List<(string, string)> { new(station.RowKey, station.StationName) };
        }

        private (DateTime, DateTime) GetStartAndEndDate(string referenceDateTime, int timezoneOffset)
        {
            DateTime referenceDate = DateTime.ParseExact(referenceDateTime, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            bool add = timezoneOffset < 0;
            var startDateTime = referenceDate.Date.AddMinutes(timezoneOffset * (add ? 1 : -1));
            var endDateTime = referenceDate.EndOfDay().AddMinutes(timezoneOffset * (add ? 1 : -1));
            return (startDateTime, endDateTime);
        }

        public async Task<List<LineChartDto>> GetImagesPerHour(string userId, string referenceDateTime, int timezoneOffset)
        {
            var user = await GetUserById(userId);
            var stations = GetAccessibleStations(user).Result;
            var (startDate, endDate) = GetStartAndEndDate(referenceDateTime, timezoneOffset);
            startDate = startDate.AddMinutes(-30);//make this into an extension method
            endDate = endDate.AddMinutes(30);
            var toReturn = new List<LineChartDto>();
            _tableStorageService.Init("images");
            foreach (var station in stations)
            {
                var query = $"PartitionKey eq '{station.Item1}' " +
                $"and Timestamp ge datetime'{startDate:yyyy-MM-dd}T{startDate:HH:mm:ss}.000Z' " +
                $"and Timestamp lt datetime'{endDate:yyyy-MM-dd}T{endDate:HH:mm:ss}.000Z'";
                var timestamps = (await _tableStorageService
                    .GetByFilterAsync<Images>(query))
                    .Select(x => x.Timestamp.Value.DateTime)
                    .ToArray();
                if (timestamps == null || timestamps.Length <= 1)
                    continue;
                toReturn.Add(new LineChartDto
                {
                    Name = station.Item2,
                    Values = await GetImagesPerHourCoordinates(timestamps, startDate)
                });
            }
            return toReturn;
        }

        private async Task<double[]> GetImagesPerHourCoordinates(DateTime[] timestamps, DateTime startDate)
        {
            var coordinates = new double[COORDINATES_LENGTH];
            foreach (var timestamp in timestamps)
            {
                var index = (int)timestamp.Subtract(startDate).TotalHours;
                coordinates[index]++;
            }
            return coordinates;
        }

        public async Task<List<LineChartDto>> GetBrightnessValuesPerHour(string userId, string referenceDateTime, int timezoneOffset)
        {
            var user = await GetUserById(userId);
            var stations = GetAccessibleStations(user).Result;
            var (startDate, endDate) = GetStartAndEndDate(referenceDateTime, timezoneOffset);
            var toReturn = new List<LineChartDto>();
            _tableStorageService.Init("images");
            foreach (var station in stations)
            {
                //var query = $"PartitionKey eq '{station.Item1}' " +
                //$"and Timestamp ge datetime'{startDate:yyyy-MM-dd}T{startDate:HH:mm:ss}.000Z' " +
                //$"and Timestamp lt datetime'{endDate:yyyy-MM-dd}T{endDate:HH:mm:ss}.000Z'";
                var query = "PartitionKey eq 'AT001' and RowKey eq '1659938788409'";

                var images = (await _tableStorageService
                    .GetByFilterAsync<Images>(query))
                    .ToArray();
                //if (images == null || images.Length <= 1)
                //    continue;
                toReturn.Add(new LineChartDto
                {
                    Name = station.Item2,
                    Values = await GetBrightnessValuesPerHourCoordinates(images, startDate.AddMinutes(-30))
                });
            }
            return toReturn;
        }

        private async Task<double[]> GetBrightnessValuesPerHourCoordinates(Images[] images, DateTime startDate)
        {
            var tempCoordinates = new List<double>[COORDINATES_LENGTH];
            for (int i = 0; i < COORDINATES_LENGTH; i++)
            {
                tempCoordinates[i] = new List<double>();
            }
            foreach (var image in images)
            {
                var index = (int)image.Timestamp.Value.DateTime.Subtract(startDate).TotalHours;
                var decompressedImage = CompressionHelper.DecompressBytes(image.ImgTotal);
                var brightnessValue = await GetBrightnessValueOfImage(decompressedImage);
                tempCoordinates[index].Add(brightnessValue);
            }
            var coordinates = new double[COORDINATES_LENGTH];
            for (int i = 0; i < COORDINATES_LENGTH; i++)
            {
                coordinates[i] = (tempCoordinates[i].Count > 0) ? tempCoordinates[i].Average() : 0;
            }
            return coordinates;
        }


        private async Task<double> GetBrightnessValueOfImage(byte[] image)
        {
            
            return 1.0;
        }
    }
}
