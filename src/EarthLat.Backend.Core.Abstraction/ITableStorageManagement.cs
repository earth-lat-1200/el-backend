namespace EarthLat.Backend.Core.Abstraction
{
    public interface ITableStorageManagement
    {
        void CreateTable(string tableName);
        void EnsureTable(string tableName);
        IEnumerable<string> GetTables();
        string GetTable(string tableName);
        void DeleteTable(string tableName);
    }
}
