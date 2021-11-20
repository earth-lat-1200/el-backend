using Azure;
using Azure.Storage.Files.Shares;
using EarthLat.Backend.Core.Extensions;
using EarthLat.Backend.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace EarthLat.Backend.Core.FileStorage
{
    public class FileStorageService : IFileStorage
    {
        private readonly ShareClient _client;
        private readonly ILogger<IFileStorage> _logger;
        private readonly string _connectionString;
        private readonly string _shareName;

        public FileStorageService(
            ILogger<IFileStorage> logger, 
            string connectionString, 
            string shareName)
        {
            connectionString.ThrowIfIsNullEmptyOrWhitespace(nameof(connectionString));
            shareName.ThrowIfIsNullEmptyOrWhitespace(nameof(shareName));

            _logger = logger;
            _connectionString = connectionString;
            _shareName = shareName;

            _client = new ShareClient(_connectionString, _shareName);
            _client.CreateIfNotExists();
        }

        public void CreateDirectory(string directoryName)
        {
            directoryName.ThrowIfIsNullEmptyOrWhitespace(nameof(directoryName));

            _logger?.LogInformation($"Try to create directory {directoryName}");
            var directory = _client.GetDirectoryClient(directoryName);
            directory.CreateIfNotExists();
        }

        public void DeleteDirectory(string directoryName)
        {
            directoryName.ThrowIfIsNullEmptyOrWhitespace(nameof(directoryName));

            _logger?.LogInformation($"Try to delete directory {directoryName}");
            var directory = _client.GetDirectoryClient(directoryName);
            directory.DeleteIfExists();
        }

        public byte[] Download(string directoryName, string fileName)
        {
            directoryName.ThrowIfIsNullEmptyOrWhitespace(nameof(directoryName));
            fileName.ThrowIfIsNullEmptyOrWhitespace(nameof(fileName));

            var directory = _client.GetDirectoryClient(directoryName);

            var fileClient = directory.GetFileClient(fileName);
            var downloadFile = fileClient.Download();
            return downloadFile.GetRawResponse().Content.ToArray();
        }

        public void Upload(string directoryName, byte[] file, string fileName)
        {
            directoryName.ThrowIfIsNullEmptyOrWhitespace(nameof(directoryName));
            fileName.ThrowIfIsNullEmptyOrWhitespace(nameof(fileName));

            var directory = _client.GetDirectoryClient(directoryName);
            var fileClient = directory.GetFileClient(fileName);

            using MemoryStream stream = new(file);
            fileClient.Create(stream.Length);
            fileClient.UploadRange(new HttpRange(0, stream.Length), stream);
        }
    }
}
