using EarthLat.Backend.Core.Compression;
using EarthLat.Backend.Core.Dtos;
using EarthLat.Backend.Core.Dtos.ChartDtos;
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
        private readonly int COORDINATES_LENGTH = 25;//1 day + 1 hour
        private readonly object _lock = new();
        private readonly string BAR_CHART_TYPE = "bar";
        private readonly string LINE_CHART_TYPE = "line";

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

        public async Task<ChartDto> GetBroadcastTimesAsync
            (JwtValidator validator, string referenceDate)
        {
            var chartDto = new ChartDto
            {
                ChartType = BAR_CHART_TYPE,
                ChartTitle = "Broadcast times"
            };
            List<DatasetDto> datasets = new();
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
                var timestamps = statistic.UploadTimestamps.FromBase64<List<string>>();
                var startDate = timestamps.FirstOrDefault();
                var endDate = timestamps.LastOrDefault();
                List<AbstractValuesDto> values = new();
                values.Add(new BarChartDatapointDto
                {

                    Start = startDate,
                    End = endDate
                });
                datasets.Add(new DatasetDto
                {
                    StationName = station.Item2,
                    Values = values
                });
            }
            chartDto.Datasets = datasets;
            return chartDto;
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


        public async Task<ChartDto> GetTemperatrueValuesPerHourAsync
            (JwtValidator validator, string referenceDate)
        {
            Console.WriteLine("the pattern indicates: ");
            var chartDto = new ChartDto
            {
                ChartType = LINE_CHART_TYPE,
                ChartTitle = "Temperature Course",
                Description = "C°",
                Min = -20,
                Max = 50
            };
            List<DatasetDto> datasets = new();
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
                Console.WriteLine(statistic);
                if (statistic == null)
                    continue;
                var timestamps = statistic.UploadTimestamps.FromBase64<List<string>>()
                    .ToArray();
                var temperatureValues = statistic.TemperatureValues.FromBase64<List<float>>()
                    .ToArray();
                Console.WriteLine(station.Item2);
                GetAverageDatapointValues(timestamps, temperatureValues, referenceDate);
                datasets.Add(new DatasetDto
                {
                    StationName = station.Item2
                });
            }
            chartDto.Datasets = datasets;
            return chartDto;
        }

        private List<LineChartDatapointDto> GetAverageDatapointValues(string[] timestamps, float[] values, string referenceDate)
        {
            Console.WriteLine("there");
            var toRetrun = new List<LineChartDatapointDto>();
            var startDate = referenceDate.ParseToDate().AddMinutes(-7.5);
            var combinedValues = timestamps
                .Select((item, index) => new LineChartDatapointDto
                {
                    Timestamp = item,
                    Value = values[index]
                });
            IEnumerable<IGrouping<int, LineChartDatapointDto>> query =
                from timestamp in combinedValues
                group timestamp by (int)(timestamp.Timestamp.ParseToDateTime().Subtract(startDate).TotalMinutes / 15);
            foreach (var group in query)
            {
                Console.WriteLine(group.Key);
                foreach (var x in group)
                    Console.WriteLine(x.Timestamp + " " + x.Value);
                Console.WriteLine("------------------");
            }
            //TODO group the average values by 15 minutes
            return toRetrun;
        }
        //public async Task<ChartDto> GetImagesPerHourAsync
        //    (JwtValidator validator, string referenceDate)
        //{
        //    var chartInfoDto = new ChartDto
        //    {
        //        ChartType = LINE_CHART_TYPE,
        //        ChartTitle = "Upload Activity",
        //        Description = "Images per hour",
        //        Min = 0,
        //        Max = 100
        //    };
        //    List<DatasetDto> dtos = new();
        //    var stations = await GetAccessibleStations(validator);
        //    foreach (var station in stations)
        //    {
        //        var query = $"PartitionKey eq '{station.Item1}' and RowKey eq '{referenceDate}'";
        //        Statistic statistic = null;
        //        lock (_lock)
        //        {
        //            _tableStorageService.Init("statistics");
        //            statistic = (_tableStorageService
        //                .GetByFilterAsync<Statistic>(query).Result)
        //                .FirstOrDefault();
        //        }
        //        if (statistic == null)
        //            continue;
        //        var timestamps = statistic.UploadTimestamps.FromBase64<List<long>>()
        //            .Select(x => x.GetDateTime().AddMinutes(
        //                timezoneOffset))
        //            .ToArray();
        //        dtos.Add(new LineChartDatapointDto
        //        {
        //            Name = station.Item2,
        //            Values = GetCoordinatesFromTimestamps(timestamps, referenceDate)
        //        });
        //    }
        //    chartInfoDto.Datasets = dtos;
        //    return chartInfoDto;
        //}
        //private double[] GetNumberOfDatapointValues(DateTime[] timestamps, string referenceDate)
        //{
        //    var startDate = referenceDate.GetStartDate().AddMinutes(-30);
        //    var coordinates = new double[COORDINATES_LENGTH];
        //    foreach (var timestamp in timestamps)
        //    {
        //        var index = (int)timestamp.Subtract(startDate).TotalHours;
        //        coordinates[index]++;
        //    }
        //    return coordinates;
        //}

        //public async Task<ChartDto> GetBrightnessValuesPerHourAsync
        //    (JwtValidator validator, string referenceDate)
        //{
        //    var chartInfoDto = new ChartDto
        //    {
        //        ChartType = LINE_CHART_TYPE,
        //        ChartTitle = "Brightness Course",
        //        Description = "Brightness",
        //        Min = 0,
        //        Max = 5000000
        //    };
        //    List<DatasetDto> dtos = new();
        //    var stations = await GetAccessibleStations(validator);
        //    foreach (var station in stations)
        //    {
        //        var query = $"PartitionKey eq '{station.Item1}' and RowKey eq '{referenceDate}'";
        //        Statistic statistic = null;
        //        lock (_lock)
        //        {
        //            _tableStorageService.Init("statistics");
        //            statistic = (_tableStorageService
        //                .GetByFilterAsync<Statistic>(query).Result)
        //                .FirstOrDefault();
        //        }
        //        if (statistic == null)
        //            continue;
        //        var timestamps = statistic.UploadTimestamps.FromBase64<List<long>>()
        //            .Select(x => x.GetDateTime().AddMinutes(timezoneOffset))
        //            .ToArray();
        //        var brightnessValues = statistic.BrightnessValues.FromBase64<List<float>>()
        //            .ToArray();
        //        dtos.Add(new LineChartDatapointDto
        //        {
        //            Name = station.Item2,
        //            Values = GetCoordinatesFromFloatArray(timestamps, brightnessValues, referenceDate)
        //        });
        //    }
        //    chartInfoDto.Datasets = dtos;
        //    return chartInfoDto;
        //}
    }
}
