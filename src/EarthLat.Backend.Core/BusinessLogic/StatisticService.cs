using EarthLat.Backend.Core.Dtos;
using EarthLat.Backend.Core.Interfaces;
using EarthLat.Backend.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthLat.Backend.Core.BusinessLogic
{
    public class StatisticService
    {
        private readonly ILogger<StatisticService> logger;
        private readonly ITableStorageService _tableStorageService;

        public StatisticService(ILogger<StatisticService> logger,
            ITableStorageService tableStorageService)
        {
            this.logger = logger;
            _tableStorageService = tableStorageService ?? throw new ArgumentNullException(nameof(tableStorageService));
        }

        public async Task<StationNamesDto> GetStations(string userId)
        {
            _tableStorageService.Init("stations");
            var stations = (await _tableStorageService.GetAllAsync<Station>()).ToList();
            _tableStorageService.Init("users");
            string query = $"RowKey eq '{userId}'";
            var user = (await _tableStorageService
                .GetByFilterAsync<User>(query))
                .FirstOrDefault();
            if (user == null || stations == null)
                return null;
            Console.WriteLine(user.PartitionKey);
            var userStation = stations.Find(x => x.RowKey == user.PartitionKey);
            return new StationNamesDto
            {
                StationNames = stations
                    .Where(x=>x.StationName != null)
                    .Select(x => x.StationName)
                    .ToList(),
                UserStationName = userStation.StationName
            };
        }

        public async Task<User> Authenticate(UserCredentials credentials)
        {
            _tableStorageService.Init("users");
            string query = $"Name eq '{credentials.Username}' and Password eq '{credentials.Password}'";
            var user = (await _tableStorageService.GetByFilterAsync<User>(query)).FirstOrDefault();
            return user;
        }
    }
}
