using EarthLat.Backend.Core.Dtos;
using EarthLat.Backend.Core.Extensions;
using EarthLat.Backend.Core.Interfaces;
using EarthLat.Backend.Core.Models;
using Microsoft.Extensions.Logging;

namespace EarthLat.Backend.Core.BusinessLogic
{
    public class StatisticService
    {
        private readonly ILogger<StatisticService> logger;
        private readonly ITableStorageService _tableStorageService;
        private DateTime serverDate;
        private DateTime clientDate;

        public StatisticService(ILogger<StatisticService> logger,
            ITableStorageService tableStorageService)
        {
            this.logger = logger;
            _tableStorageService = tableStorageService ?? throw new ArgumentNullException(nameof(tableStorageService));
        }

        public async Task<User> Authenticate(UserCredentials credentials)
        {
            _tableStorageService.Init("users");
            string query = $"Name eq '{credentials.Username}' and Password eq '{credentials.Password}'";
            var user = (await _tableStorageService.GetByFilterAsync<User>(query)).FirstOrDefault();
            return user;
        }

        public async Task<StationNamesDto> GetStations(string userId)
        {
            _tableStorageService.Init("users");
            string query = $"RowKey eq '{userId}'";
            var user = (await _tableStorageService
                .GetByFilterAsync<User>(query))
                .FirstOrDefault();
            _tableStorageService.Init("stations");
            var stations = (await _tableStorageService.GetAllAsync<Station>()).ToList();
            if (user == null || stations == null)
                return null;
            var userStation = stations.Find(x => x.RowKey == user.PartitionKey);//TODO add null check
            return new StationNamesDto
            {
                StationNames = stations
                    .Where(x => x.StationName != null)
                    .Select(x => x.StationName)
                    .ToList(),
                UserStationName = userStation.StationName
            };
        }

        public async Task<List<BarChartDto>> GetSendTimes(string userId, string referenceDateTime, string clientDateTime)
        {
            _tableStorageService.Init("users");
            string query = $"RowKey eq '{userId}'";
            var user = (await _tableStorageService
                .GetByFilterAsync<User>(query))
                .FirstOrDefault();
            var stations = GetAccessibleStations(user).Result;
            var (startDate, endDate) = GetStartAndEndDate(referenceDateTime, clientDateTime);
            var toReturn = new List<BarChartDto>();
            _tableStorageService.Init("images");
            foreach(var station in stations)
            {
                query = $"PartitionKey eq '{station.Item1}' " +
                $"and Timestamp ge datetime'{startDate:yyyy-MM-dd}T{startDate:HH:mm:ss}.000Z' " +
                $"and Timestamp lt datetime'{endDate:yyyy-MM-dd}T{endDate:HH:mm:ss}.000Z'";
                var images = (await _tableStorageService
                    .GetByFilterAsync<Images>(query)).OrderBy(x => x.Timestamp).ToList();
                if(images==null||images.Count()<=1)
                    continue;
                var startTimeStamp = images.FirstOrDefault().Timestamp.Value.DateTime;
                var endTimeStamp = images.LastOrDefault().Timestamp.Value.DateTime;
                toReturn.Add(new BarChartDto
                {
                    Name = station.Item2,
                    Start = GetAdjustedSecondsSinceMidnight(startTimeStamp),
                    End = GetAdjustedSecondsSinceMidnight(endTimeStamp)
                });
            }
            foreach(var x in toReturn)
            {
                Console.WriteLine(x.Name);
            }
            return toReturn;
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

        private (DateTime, DateTime) GetStartAndEndDate(string referenceDateTime, string clientDateTime)
        {
            DateTime referenceDate = DateTime.ParseExact(referenceDateTime, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            clientDate = DateTime.ParseExact(clientDateTime, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
            serverDate = DateTime.UtcNow;
            clientDate = serverDate.AddHours(2);
            var secondsOffset = serverDate.Subtract(clientDate).TotalSeconds;
            bool add = serverDate < clientDate;
            var startDateTime = referenceDate.Date.AddSeconds(secondsOffset * (add ? -1 : 1));
            var endDateTime = referenceDate.EndOfDay().AddSeconds(secondsOffset * (add ? -1 : 1));
            return (startDateTime, endDateTime);
        }

        private int GetAdjustedSecondsSinceMidnight(DateTime toAdjust)
        {
            var secondsOffset = serverDate.Subtract(clientDate).TotalSeconds;
            bool subtract = serverDate < clientDate;
            return toAdjust.AddSeconds(secondsOffset * (subtract ? 1 : -1)).GetSecondsSinceMidnight();
        }
    }
}
