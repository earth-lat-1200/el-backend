﻿using Azure.Data.Tables;
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
            connectionString.ThrowIfIsNullEmptyOrWhitespace(nameof(connectionString));

            _tableServiceClient = new TableServiceClient(connectionString);
        }

        public void CreateTable(string tableName)
        {
            tableName.ThrowIfIsNullEmptyOrWhitespace(nameof(tableName));
            EnsureTable(tableName);
        }

        public void DeleteTable(string tableName)
        {
            tableName.ThrowIfIsNullEmptyOrWhitespace(nameof(tableName));
            _tableServiceClient.DeleteTable(tableName);
        }

        public void EnsureTable(string tableName)
        {
            tableName.ThrowIfIsNullEmptyOrWhitespace(nameof(tableName));
            _tableServiceClient.CreateTableIfNotExists(tableName);
        }

        public string GetTable(string tableName)
        {
            tableName.ThrowIfIsNullEmptyOrWhitespace(nameof(tableName));

            return _tableServiceClient?.Query(filter: $"TableName eq '{tableName}'")
                                       .FirstOrDefault()?.Name ?? $"No table with {tableName} found.";
        }

        public IEnumerable<string> GetTables() => _tableServiceClient.Query().Select(q => q.Name).ToArray();
    }
}
