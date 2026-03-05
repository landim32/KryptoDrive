using KryptoDrive.DTO.DTOs;

namespace KryptoDrive.Infra.Interfaces
{
    public interface IVaultAppService
    {
        Task<MediaCatalogInfo> GetCatalogAsync();
        Task<MediaFileInfo> StoreMediaAsync(Stream sourceStream, string originalFileName, string mediaType, string folderPath);
        Task DeleteFileAsync(string fileId, string? encryptedFileName);
        Task CreateFolderAsync(string folderName, string parentPath);
        Task DeleteFolderAsync(string folderPath);
        Task UpdateKeywordsAsync(string fileId, IEnumerable<string> keywords);
        Task<byte[]> GetDecryptedFileAsync(string encryptedFileName);
        List<FileItemInfo> GetFileItems(MediaCatalogInfo catalog, string currentPath, string? searchQuery);
    }
}
