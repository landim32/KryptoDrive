using System.Text.Json;
using KryptoDrive.Models;

namespace KryptoDrive.Services
{
    public interface IMediaStorageService
    {
        Task<MediaCatalog> LoadCatalogAsync();
        Task SaveCatalogAsync(MediaCatalog catalog);
        Task<string> StoreEncryptedFileAsync(Stream sourceStream, string originalFileName, string mediaType);
        Task<byte[]> LoadDecryptedFileAsync(string encryptedFileName);
        Task DeleteFileAsync(string encryptedFileName);
        string GetFilesPath();
    }

    public class MediaStorageService : IMediaStorageService
    {
        private readonly ICryptoService _cryptoService;
        private readonly string _vaultPath;
        private readonly string _filesPath;

        public MediaStorageService(ICryptoService cryptoService)
        {
            _cryptoService = cryptoService;
            _vaultPath = Path.Combine(FileSystem.AppDataDirectory, "vault");
            _filesPath = Path.Combine(_vaultPath, "files");
            Directory.CreateDirectory(_filesPath);
        }

        public string GetFilesPath() => _filesPath;

        public async Task<MediaCatalog> LoadCatalogAsync()
        {
            var catalogPath = Path.Combine(_vaultPath, "catalog.enc");
            if (!File.Exists(catalogPath))
                return new MediaCatalog();

            var encryptedData = await File.ReadAllBytesAsync(catalogPath);
            var decryptedData = _cryptoService.Decrypt(encryptedData);
            var json = System.Text.Encoding.UTF8.GetString(decryptedData);
            return JsonSerializer.Deserialize<MediaCatalog>(json) ?? new MediaCatalog();
        }

        public async Task SaveCatalogAsync(MediaCatalog catalog)
        {
            var json = JsonSerializer.Serialize(catalog, new JsonSerializerOptions
            {
                WriteIndented = false
            });
            var data = System.Text.Encoding.UTF8.GetBytes(json);
            var encrypted = _cryptoService.Encrypt(data);
            var catalogPath = Path.Combine(_vaultPath, "catalog.enc");
            await File.WriteAllBytesAsync(catalogPath, encrypted);
        }

        public async Task<string> StoreEncryptedFileAsync(Stream sourceStream, string originalFileName, string mediaType)
        {
            using var ms = new MemoryStream();
            await sourceStream.CopyToAsync(ms);
            var plainData = ms.ToArray();

            var encrypted = _cryptoService.Encrypt(plainData);
            var encryptedFileName = $"{Guid.NewGuid():N}.enc";
            var filePath = Path.Combine(_filesPath, encryptedFileName);
            await File.WriteAllBytesAsync(filePath, encrypted);

            return encryptedFileName;
        }

        public async Task<byte[]> LoadDecryptedFileAsync(string encryptedFileName)
        {
            var filePath = Path.Combine(_filesPath, encryptedFileName);
            var encryptedData = await File.ReadAllBytesAsync(filePath);
            return _cryptoService.Decrypt(encryptedData);
        }

        public Task DeleteFileAsync(string encryptedFileName)
        {
            var filePath = Path.Combine(_filesPath, encryptedFileName);
            if (File.Exists(filePath))
                File.Delete(filePath);
            return Task.CompletedTask;
        }
    }
}
