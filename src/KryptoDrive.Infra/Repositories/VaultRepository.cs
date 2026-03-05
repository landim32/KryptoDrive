using System.Text.Json;
using KryptoDrive.Domain.Models;
using KryptoDrive.Infra.Interfaces;

namespace KryptoDrive.Infra.Repositories
{
    public class VaultRepository : IVaultRepository
    {
        private readonly ICryptoService _cryptoService;
        private readonly string _vaultPath;
        private readonly string _filesPath;

        public VaultRepository(ICryptoService cryptoService, string basePath)
        {
            _cryptoService = cryptoService;
            _vaultPath = Path.Combine(basePath, "vault");
            _filesPath = Path.Combine(_vaultPath, "files");
            Directory.CreateDirectory(_filesPath);
        }

        public string GetFilesPath() => _filesPath;

        public async Task<MediaCatalog> GetCatalogAsync()
        {
            var catalogPath = Path.Combine(_vaultPath, "catalog.enc");
            if (!File.Exists(catalogPath))
                return new MediaCatalog();

            var encryptedData = await File.ReadAllBytesAsync(catalogPath);
            var decryptedData = _cryptoService.Decrypt(encryptedData);
            var json = System.Text.Encoding.UTF8.GetString(decryptedData);

            var dto = JsonSerializer.Deserialize<CatalogPersistenceDto>(json);
            if (dto == null) return new MediaCatalog();

            return MapToDomain(dto);
        }

        public async Task SaveCatalogAsync(MediaCatalog catalog)
        {
            var dto = MapToDto(catalog);
            var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions
            {
                WriteIndented = false
            });
            var data = System.Text.Encoding.UTF8.GetBytes(json);
            var encrypted = _cryptoService.Encrypt(data);
            var catalogPath = Path.Combine(_vaultPath, "catalog.enc");
            await File.WriteAllBytesAsync(catalogPath, encrypted);
        }

        public async Task<string> StoreFileAsync(Stream sourceStream, string originalFileName, string mediaType)
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

        public async Task<byte[]> GetDecryptedFileAsync(string encryptedFileName)
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

        #region Serialization Compatibility

        /// <summary>
        /// Internal DTO that preserves the same JSON property names as the original
        /// EncryptedFileInfo/MediaCatalog classes for backward compatibility with
        /// existing catalog.enc files.
        /// </summary>
        private class CatalogPersistenceDto
        {
            public List<FilePersistenceDto> Files { get; set; } = new();
            public List<FolderPersistenceDto> Folders { get; set; } = new();
        }

        private class FilePersistenceDto
        {
            public string Id { get; set; } = string.Empty;
            public string OriginalFileName { get; set; } = string.Empty;
            public string EncryptedFileName { get; set; } = string.Empty;
            public string FolderPath { get; set; } = "/";
            public string MediaType { get; set; } = "photo";
            public string FileExtension { get; set; } = string.Empty;
            public List<string> Keywords { get; set; } = new();
            public DateTime CreatedAt { get; set; }
            public long FileSize { get; set; }
        }

        private class FolderPersistenceDto
        {
            public string Name { get; set; } = string.Empty;
            public string Path { get; set; } = "/";
            public DateTime CreatedAt { get; set; }
        }

        private static MediaCatalog MapToDomain(CatalogPersistenceDto dto)
        {
            var catalog = new MediaCatalog();

            foreach (var f in dto.Files)
            {
                catalog.AddFile(MediaFile.Reconstitute(
                    f.Id, f.OriginalFileName, f.EncryptedFileName,
                    f.FolderPath, f.MediaType, f.FileExtension,
                    f.Keywords, f.CreatedAt, f.FileSize));
            }

            foreach (var f in dto.Folders)
            {
                catalog.AddFolder(SecureFolder.Reconstitute(f.Name, f.Path, f.CreatedAt));
            }

            return catalog;
        }

        private static CatalogPersistenceDto MapToDto(MediaCatalog catalog)
        {
            return new CatalogPersistenceDto
            {
                Files = catalog.Files.Select(f => new FilePersistenceDto
                {
                    Id = f.Id,
                    OriginalFileName = f.OriginalFileName,
                    EncryptedFileName = f.EncryptedFileName,
                    FolderPath = f.FolderPath,
                    MediaType = f.MediaType,
                    FileExtension = f.FileExtension,
                    Keywords = f.Keywords,
                    CreatedAt = f.CreatedAt,
                    FileSize = f.FileSize
                }).ToList(),
                Folders = catalog.Folders.Select(f => new FolderPersistenceDto
                {
                    Name = f.Name,
                    Path = f.Path,
                    CreatedAt = f.CreatedAt
                }).ToList()
            };
        }

        #endregion
    }
}
