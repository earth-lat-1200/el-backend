using EarthLat.Backend.Core.Dtos;
using EarthLat.Backend.Core.Extensions;
using EarthLat.Backend.Core.Interfaces;
using EarthLat.Backend.Core.JWT;
using EarthLat.Backend.Core.Models;
using Microsoft.Extensions.Logging;

namespace EarthLat.Backend.Core.BusinessLogic
{
    public class StatisticService
    {
        private readonly ILogger<StatisticService> logger;
        private readonly ITableStorageService _tableStorageService;
        private readonly JwtGenerator generator;

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
                Token=generator.GenerateJWT(user),
                StationName = (userStation!=null) ? userStation.StationName : null
            };
        }

        public async Task<List<BarChartDto>> GetSendTimes(string userId, string referenceDateTime, int timezoneOffset)
        {
            var user = await GetUserById(userId);
            var stations = await GetAccessibleStations(user);
            var (startDate, endDate) = GetStartAndEndDate(referenceDateTime, timezoneOffset);
            var toReturn = new List<BarChartDto>();
            _tableStorageService.Init("images");
            foreach(var station in stations)
            {
                var query = $"PartitionKey eq '{station.Item1}' " +
                $"and Timestamp ge datetime'{startDate:yyyy-MM-dd}T{startDate:HH:mm:ss}.000Z' " +
                $"and Timestamp lt datetime'{endDate:yyyy-MM-dd}T{endDate:HH:mm:ss}.000Z'";
                var images = (await _tableStorageService
                    .GetByFilterAsync<Images>(query))
                    .OrderBy(x => x.Timestamp)
                    .ToList();
                if(images==null||images.Count()<=1)
                    continue;
                var startTimeStamp = images.FirstOrDefault().Timestamp.Value.DateTime;
                var endTimeStamp = images.LastOrDefault().Timestamp.Value.DateTime;
                toReturn.Add(new BarChartDto
                {
                    Name = station.Item2,
                    Start = GetAdjustedSecondsSinceMidnight(startTimeStamp, timezoneOffset),
                    End = GetAdjustedSecondsSinceMidnight(endTimeStamp, timezoneOffset)
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

        private async Task<List<(string,string)>> GetAccessibleStations(User user)
        {
            List<(string, string?)> stations = new();
            _tableStorageService.Init("stations");
            if (user.Privilege == 0)
            {
                stations = (await _tableStorageService.GetAllAsync<Station>()).Where(x => x.StationName != null).Select(x => (x.RowKey, x.StationName)).ToList();
            }
            else
            {
                string query = $"RowKey eq '{user.PartitionKey}'";
                var station = (await _tableStorageService
                    .GetByFilterAsync<Station>(query))
                    .FirstOrDefault();
                stations.Add((station.RowKey, station.StationName));
            }
            return stations;
        }

        private (DateTime, DateTime) GetStartAndEndDate(string referenceDateTime, int timezoneOffset)
        {
            DateTime referenceDate = DateTime.ParseExact(referenceDateTime, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            bool add = timezoneOffset < 0;
            var startDateTime = referenceDate.Date.AddMinutes(timezoneOffset * (add ? 1 : -1));
            var endDateTime = referenceDate.EndOfDay().AddMinutes(timezoneOffset * (add ? 1 : -1));
            return (startDateTime, endDateTime);
        }

        private int GetAdjustedSecondsSinceMidnight(DateTime toAdjust, int timezoneOffset)
        {
            bool subtract = timezoneOffset < 0;
            return toAdjust.AddMinutes(timezoneOffset * (subtract ? -1 : 1)).GetSecondsSinceMidnight();
        }

        public async Task<List<LineChartDto>> GetImagesPerHour(string userId, string referenceDateTime, int timezoneOffset)
        {
            var user = await GetUserById(userId);
            var stations = GetAccessibleStations(user).Result;
            var (startDate, endDate) = GetStartAndEndDate(referenceDateTime, timezoneOffset);
            var toReturn = new List<LineChartDto>();
            _tableStorageService.Init("images");
            foreach (var station in stations)
            {
                var query = $"PartitionKey eq '{station.Item1}' " +
                $"and Timestamp ge datetime'{startDate:yyyy-MM-dd}T{startDate:HH:mm:ss}.000Z' " +
                $"and Timestamp lt datetime'{endDate:yyyy-MM-dd}T{endDate:HH:mm:ss}.000Z'";
                var timestamps = (await _tableStorageService
                    .GetByFilterAsync<Images>(query))
                    .Select(x=>x.Timestamp.Value.DateTime)
                    .ToArray();
                if (timestamps == null || timestamps.Length <= 1)
                    continue;
                toReturn.Add(new LineChartDto
                {
                    Name = station.Item2,
                    Values = await GetCoordinates(timestamps, startDate.AddMinutes(-30))
                });
            }
            return toReturn;
        }

        private async Task<int[]> GetCoordinates(DateTime[] timestamps, DateTime startDate)
        {
            var coordinates = new int[25];
            foreach (var timestamp in timestamps)
            {
                var index = (int)timestamp.Subtract(startDate).TotalHours;
                coordinates[index]++;
            }
            return coordinates;
        }
    }
}
