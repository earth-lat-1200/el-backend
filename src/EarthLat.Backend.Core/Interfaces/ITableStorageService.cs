using Azure.Data.Tables;

namespace EarthLat.Backend.Core.Interfaces
{
    public interface ITableStorageService
    {
        void Init(string tableName);
        Task AddAsync<T>(T entity) where T : class, ITableEntity, new();
        Task AddOrUpdateAsync<T>(T entity) where T : class, ITableEntity, new();
        Task<T> GetAsync<T>(string partitionKey, string rowKey) where T : class, ITableEntity, new();
        Task<IEnumerable<T>> GetAllAsync<T>() where T : class, ITableEntity, new();
        Task<IEnumerable<T>> GetByFilterAsync<T>(string oDataFilter) where T : class, ITableEntity, new();
        Task DeleteAsync<T>(string partitionKey, string rowKey);
        Task UpdateAsync<T>(T entity) where T : class, ITableEntity, new();
    }
}
