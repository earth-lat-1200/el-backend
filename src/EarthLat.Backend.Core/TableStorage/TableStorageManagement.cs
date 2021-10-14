using Azure.Data.Tables;
using EarthLat.Backend.Core.Abstraction;
using Microsoft.Extensions.Logging;

namespace EarthLat.Backend.Core.TableStorage
{

    public class TableStorageManagement : ITableStorageManagement
    {
        private readonly ILogger<TableStorageManagement> _logger;
        private readonly TableServiceClient _tableServiceClient;

        public TableStorageManagement(ILogger<TableStorageManagement> logger, string connectionString)
        {
            _logger = logger;

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger?.LogError($"'{nameof(connectionString)}' cannot be null or empty.");
                throw new ArgumentException($"'{nameof(connectionString)}' cannot be null or empty.", nameof(connectionString));
            }

            _tableServiceClient = new TableServiceClient(connectionString);
        }

        public void CreateTable(string tableName)
        {
            EnsureTable(tableName);
        }

        public void DeleteTable(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentException($"'{nameof(tableName)}' cannot be null or whitespace.", nameof(tableName));
            }

            _tableServiceClient.DeleteTable(tableName);
        }

        public void EnsureTable(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                _logger?.LogError($"'{nameof(tableName)}' cannot be null or whitespace.");
                throw new ArgumentException($"'{nameof(tableName)}' cannot be null or whitespace.", nameof(tableName));
            }

            _tableServiceClient.CreateTableIfNotExists(tableName);
        }

        public string GetTable(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                _logger?.LogError($"'{nameof(tableName)}' cannot be null or whitespace.");
                throw new ArgumentException($"'{nameof(tableName)}' cannot be null or whitespace.", nameof(tableName));
            }

            return _tableServiceClient?.Query(filter: $"TableName eq '{tableName}'")
                                       .FirstOrDefault()?.Name ?? $"No table with {tableName} found.";
        }

        public IEnumerable<string> GetTables() => _tableServiceClient.Query().Select(q => q.Name).ToArray();
    }
}
