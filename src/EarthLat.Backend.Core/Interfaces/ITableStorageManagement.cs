namespace EarthLat.Backend.Core.Interfaces
{
    public interface ITableStorageManagement
    {
        void CreateTable(string tableName);
        void EnsureTable(string tableName);
        IEnumerable<string> GetTables();
        string GetTableAsync(string tableName);
        void DeleteTable(string tableName);
    }
}
