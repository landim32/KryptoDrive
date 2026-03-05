using KryptoDrive.Domain.Models;

namespace KryptoDrive.Infra.Interfaces
{
    public interface IVaultRepository
    {
        Task<MediaCatalog> GetCatalogAsync();
        Task SaveCatalogAsync(MediaCatalog catalog);
        Task<string> StoreFileAsync(Stream sourceStream, string originalFileName, string mediaType);
        Task<byte[]> GetDecryptedFileAsync(string encryptedFileName);
        Task DeleteFileAsync(string encryptedFileName);
        string GetFilesPath();
    }
}
