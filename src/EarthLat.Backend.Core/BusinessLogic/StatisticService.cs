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
        public async Task<User> Authenticate(UserCredentials credentials)
        {
            _tableStorageService.Init("users");
            string query = $"Name eq '{credentials.Username}' and Password eq '{credentials.Password}'";
            var user = (await _tableStorageService.GetByFilterAsync<User>(query)).FirstOrDefault();
            return user;
        }
    }
}
