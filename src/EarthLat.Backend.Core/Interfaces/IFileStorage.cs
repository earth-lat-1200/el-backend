namespace EarthLat.Backend.Core.Interfaces
{
    public interface IFileStorage
    {
        void CreateDirectory(string directoryName);
        void DeleteDirectory(string directoryName);
        void Upload(string directoryName, byte[] file, string fileName);
        byte[] Download(string directoryName, string fileName);
    }
}
