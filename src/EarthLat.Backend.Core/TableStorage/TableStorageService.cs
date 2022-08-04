using Azure;
using Azure.Data.Tables;
using EarthLat.Backend.Core.Interfaces;

namespace EarthLat.Backend.Core.TableStorage
{
    public class TableStorageService : ITableStorageService
    {
        private TableClient? _tableClient;
        private string _tableName;
        private readonly string _connectionString;

        public TableStorageService(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException($"'{nameof(connectionString)}' cannot be null or whitespace.", nameof(connectionString));
            }

            _connectionString = connectionString;
            _tableName = string.Empty;
        }

        public void Init(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentException($"'{nameof(tableName)}' cannot be null or whitespace.", nameof(tableName));
            }

            _tableName = tableName;

            _tableClient = new TableClient(_connectionString, _tableName);
            _tableClient.CreateIfNotExists();
        }

        public async Task AddAsync<T>(T entity)
            where T : class, ITableEntity, new()
        {
            ThrowIfNotInitialized();
            await _tableClient.AddEntityAsync(entity);
        }

        public async Task AddOrUpdateAsync<T>(T entity)
            where T : class, ITableEntity, new()
        {
            await _tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace);
        }

        public async Task UpdateAsync<T>(T entity)
            where T : class, ITableEntity, new()
        {
            ThrowIfNotInitialized();
            await _tableClient.UpdateEntityAsync<T>(entity, ETag.All, TableUpdateMode.Replace);
        }

        public async Task DeleteAsync<T>(string partitionKey, string rowKey)
        {
            ThrowIfNotInitialized();
            await _tableClient.DeleteEntityAsync(partitionKey, rowKey);
        }

        public async Task<T> GetAsync<T>(string partitionKey, string rowKey)
            where T : class, ITableEntity, new()
        {
            ThrowIfNotInitialized();
            return (await _tableClient.GetEntityAsync<T>(partitionKey, rowKey)).Value;
        }

        public async Task<IEnumerable<T>> GetAllAsync<T>()
            where T : class, ITableEntity, new()
        {
            ThrowIfNotInitialized();
            var pages = _tableClient.QueryAsync<T>("PartitionKey ne ''");
            var result = new List<T>();
            await foreach (var page in pages)
            {
                result.Add(page);
            }

            return result;
        }

        public async Task<IEnumerable<T>> GetByFilterAsync<T>(string oDataFilter)
            where T : class, ITableEntity, new()
        {
            ThrowIfNotInitialized();
            var pages = _tableClient.QueryAsync<T>(oDataFilter);//TODO figure out why this takes so long and optimize it
            var result = new List<T>();
            await foreach (var page in pages)
            {
                result.Add(page);
            }
            return result;
        }

        private void ThrowIfNotInitialized()
        {
            if (_tableClient is null)
            {
                throw new NullReferenceException("Init not executed and client instance is null.");
            }
        }
    }
}
