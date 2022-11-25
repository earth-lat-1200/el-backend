using EarthLat.Backend.Core.Compression;
using EarthLat.Backend.Core.Dtos;
using EarthLat.Backend.Core.Dtos.ChartDtos;
using EarthLat.Backend.Core.Extensions;
using EarthLat.Backend.Core.Interfaces;
using EarthLat.Backend.Core.JWT;
using EarthLat.Backend.Core.Models;
using Microsoft.Extensions.Logging;
using System.Data;
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
        private readonly int STATION_ACRONYM_LENGTH = 2;

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
            (JwtValidator validator, string startReferenceDate, string endReferenceDate)
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
                var dataset = new DatasetDto
                {
                    StationName = $"{station.Item2} ({station.Item1[..STATION_ACRONYM_LENGTH]})"
                };
                List<AbstractValuesDto> values = new();
                var referenceDate = startReferenceDate.ParseToDate();
                while (referenceDate <= endReferenceDate.ParseToDate())
                {
                    var formatedReferenceDate = referenceDate.ToString("yyyy-MM-dd");
                    var query = $"PartitionKey eq '{station.Item1}' and RowKey eq '{formatedReferenceDate}'";
                    Statistic statistic = null;
                    lock (_lock)
                    {
                        _tableStorageService.Init("statistics");
                        statistic = (_tableStorageService
                            .GetByFilterAsync<Statistic>(query).Result)
                            .FirstOrDefault();
                    }
                    referenceDate = referenceDate.AddDays(1);
                    if (statistic == null)
                        continue;
                    var timestamps = statistic.UploadTimestamps.FromBase64<List<string>>();
                    if (timestamps.Count < 2)
                        continue;
                    var startDate = timestamps.FirstOrDefault();
                    for (int i = 1; i < timestamps.Count; i++)
                    {
                        if (timestamps[i].ParseToDateTime().Subtract(timestamps[i - 1].ParseToDateTime()).TotalMinutes > 15)
                        {
                            if (startDate == timestamps[i - 1])
                                continue;
                            values.Add(new BarChartDatapointDto
                            {
                                Start = startDate,
                                End = timestamps[i - 1]
                            });
                            startDate = timestamps[i];
                        }
                    }
                    var endDate = timestamps.LastOrDefault();
                    values.Add(new BarChartDatapointDto
                    {
                        Start = startDate,
                        End = endDate
                    });
                }
                if (values.Any())
                {
                    dataset.Values = values;
                    datasets.Add(dataset);
                }
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
            (JwtValidator validator, string startReferenceDate, string endReferenceDate)
        {
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
                var dataset = new DatasetDto
                {
                    StationName = $"{station.Item2} ({station.Item1[..STATION_ACRONYM_LENGTH]})"
                };
                List<AbstractValuesDto> values = new();
                var referenceDate = startReferenceDate.ParseToDate();
                while (referenceDate <= endReferenceDate.ParseToDate())
                {
                    var formatedReferenceDate = referenceDate.ToString("yyyy-MM-dd");
                    var query = $"PartitionKey eq '{station.Item1}' and RowKey eq '{formatedReferenceDate}'";
                    Statistic statistic = null;
                    lock (_lock)
                    {
                        _tableStorageService.Init("statistics");
                        statistic = (_tableStorageService
                            .GetByFilterAsync<Statistic>(query).Result)
                            .FirstOrDefault();
                    }
                    referenceDate = referenceDate.AddDays(1);
                    if (statistic == null)
                        continue;
                    var timestamps = statistic.UploadTimestamps.FromBase64<List<string>>()
                        .ToArray();
                    var temperatureValues = statistic.TemperatureValues.FromBase64<List<float>>()
                        .ToArray();
                    values.AddRange(GetAverageDatapointValues(timestamps, temperatureValues, formatedReferenceDate));
                }
                if (CollectionContainsDatapoints(values))
                {
                    dataset.Values = values;
                    datasets.Add(dataset);
                }
            }
            chartDto.Datasets = datasets;
            return chartDto;
        }

        private List<AbstractValuesDto> GetAverageDatapointValues(string[] timestamps, float[] values, string referenceDate)
        {
            var startDate = referenceDate.ParseToDate();
            var endDate = referenceDate.ParseToDate().AddDays(1);
            var toRetrun = GetEmptyDatapointList(startDate, endDate, 15);
            var combinedValues = timestamps
                .Select((item, index) => new LineChartDatapointDto
                {
                    Timestamp = item,
                    Value = values[index]
                });
            IEnumerable<IGrouping<int, LineChartDatapointDto>> query =
                from timestamp in combinedValues
                group timestamp by (int)(timestamp.Timestamp.ParseToDateTime().Subtract(startDate.AddMinutes(-7.5)).TotalMinutes / 15);
            foreach (var group in query)
            {
                ((LineChartDatapointDto)toRetrun[group.Key]).Value = group.Select(x => x.Value).Average();
            }
            return toRetrun;
        }
        private List<AbstractValuesDto> GetEmptyDatapointList(DateTime startDate, DateTime endDate, int interval)
        {
            var toRetrun = new List<AbstractValuesDto>();
            for (DateTime i = startDate; i <= endDate; i = i.AddMinutes(interval))
            {
                toRetrun.Add(new LineChartDatapointDto
                {
                    Timestamp = i.ToString(SundialLogic.TIMESTAMP_DATE_PARSER)
                });
            }
            return toRetrun;
        }
        private bool CollectionContainsDatapoints(List<AbstractValuesDto> values)
        {
            return values.Select(x => ((LineChartDatapointDto)x).Value).Except(new List<double> { 0 }).Any();
        }
        public async Task<ChartDto> GetImagesPerHourAsync
            (JwtValidator validator, string startReferenceDate, string endReferenceDate)
        {
            var chartDto = new ChartDto
            {
                ChartType = LINE_CHART_TYPE,
                ChartTitle = "Upload Activity",
                Description = "Images per hour",
                Min = 0,
                Max = 100
            };
            List<DatasetDto> datasets = new();
            var stations = await GetAccessibleStations(validator);
            foreach (var station in stations)
            {
                var dataset = new DatasetDto
                {
                    StationName = $"{station.Item2} ({station.Item1[..STATION_ACRONYM_LENGTH]})"
                };
                List<AbstractValuesDto> values = new();
                var referenceDate = startReferenceDate.ParseToDate();
                while (referenceDate <= endReferenceDate.ParseToDate())
                {
                    var formatedReferenceDate = referenceDate.ToString("yyyy-MM-dd");
                    var query = $"PartitionKey eq '{station.Item1}' and RowKey eq '{formatedReferenceDate}'";
                    Statistic statistic = null;
                    lock (_lock)
                    {
                        _tableStorageService.Init("statistics");
                        statistic = (_tableStorageService
                            .GetByFilterAsync<Statistic>(query).Result)
                            .FirstOrDefault();
                    }
                    referenceDate = referenceDate.AddDays(1);
                    if (statistic == null)
                        continue;
                    var timestamps = statistic.UploadTimestamps.FromBase64<List<string>>()
                        .ToArray();
                    values.AddRange(GetNumberOfDatapointValues(timestamps, formatedReferenceDate));
                }
                if (CollectionContainsDatapoints(values))
                {
                    dataset.Values = values;
                    datasets.Add(dataset);
                }
            }
            chartDto.Datasets = datasets;
            return chartDto;
        }
        private List<AbstractValuesDto> GetNumberOfDatapointValues(string[] timestamps, string referenceDate)
        {
            var startDate = referenceDate.ParseToDate();
            var endDate = referenceDate.ParseToDate().AddDays(1);
            var toRetrun = GetEmptyDatapointList(startDate, endDate, 60);
            IEnumerable<IGrouping<int, string>> query =
                from timestamp in timestamps
                group timestamp by (int)(timestamp.ParseToDateTime().Subtract(startDate.AddMinutes(-30)).TotalMinutes / 60);
            foreach (var group in query)
            {
                ((LineChartDatapointDto)toRetrun[group.Key]).Value = group.Count();
            }
            return toRetrun;
        }

        public async Task<ChartDto> GetBrightnessValuesPerHourAsync
            (JwtValidator validator, string startReferenceDate, string endReferenceDate)
        {
            var chartDto = new ChartDto
            {
                ChartType = LINE_CHART_TYPE,
                ChartTitle = "Brightness Course",
                Description = "Brightness",
                Min = 0,
                Max = 5000000
            };
            List<DatasetDto> datasets = new();
            var stations = await GetAccessibleStations(validator);
            foreach (var station in stations)
            {
                var dataset = new DatasetDto
                {
                    StationName = $"{station.Item2} ({station.Item1[..STATION_ACRONYM_LENGTH]})"
                };
                List<AbstractValuesDto> values = new();
                var referenceDate = startReferenceDate.ParseToDate();
                while (referenceDate <= endReferenceDate.ParseToDate())
                {
                    var formatedReferenceDate = referenceDate.ToString("yyyy-MM-dd");
                    var query = $"PartitionKey eq '{station.Item1}' and RowKey eq '{formatedReferenceDate}'";
                    Statistic statistic = null;
                    lock (_lock)
                    {
                        _tableStorageService.Init("statistics");
                        statistic = (_tableStorageService
                            .GetByFilterAsync<Statistic>(query).Result)
                            .FirstOrDefault();
                    }
                    referenceDate = referenceDate.AddDays(1);
                    if (statistic == null)
                        continue;
                    var timestamps = statistic.UploadTimestamps.FromBase64<List<string>>()
                        .ToArray();
                    var brightnessValues = statistic.BrightnessValues.FromBase64<List<float>>()
                        .ToArray();
                    values.AddRange(GetAverageDatapointValues(timestamps, brightnessValues, formatedReferenceDate));
                }
                if (CollectionContainsDatapoints(values))
                {
                    dataset.Values = values;
                    datasets.Add(dataset);
                }
            }
            chartDto.Datasets = datasets;
            return chartDto;
        }
    }
}
