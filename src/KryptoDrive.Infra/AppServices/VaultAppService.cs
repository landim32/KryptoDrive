using AutoMapper;
using KryptoDrive.Domain.Models;
using KryptoDrive.DTO.DTOs;
using KryptoDrive.Infra.Interfaces;

namespace KryptoDrive.Infra.AppServices
{
    public class VaultAppService : IVaultAppService
    {
        private readonly IVaultRepository _repository;
        private readonly IMapper _mapper;

        public VaultAppService(IVaultRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<MediaCatalogInfo> GetCatalogAsync()
        {
            var catalog = await _repository.GetCatalogAsync();
            return _mapper.Map<MediaCatalogInfo>(catalog);
        }

        public async Task<MediaFileInfo> StoreMediaAsync(Stream sourceStream, string originalFileName, string mediaType, string folderPath)
        {
            var encryptedFileName = await _repository.StoreFileAsync(sourceStream, originalFileName, mediaType);

            var filePath = Path.Combine(_repository.GetFilesPath(), encryptedFileName);
            var fileSize = new FileInfo(filePath).Length;

            var mediaFile = MediaFile.Create(
                originalFileName,
                encryptedFileName,
                folderPath,
                mediaType,
                Path.GetExtension(originalFileName),
                fileSize);

            var catalog = await _repository.GetCatalogAsync();
            catalog.AddFile(mediaFile);
            await _repository.SaveCatalogAsync(catalog);

            return _mapper.Map<MediaFileInfo>(mediaFile);
        }

        public async Task DeleteFileAsync(string fileId, string? encryptedFileName)
        {
            if (encryptedFileName != null)
                await _repository.DeleteFileAsync(encryptedFileName);

            var catalog = await _repository.GetCatalogAsync();
            catalog.RemoveFile(fileId);
            await _repository.SaveCatalogAsync(catalog);
        }

        public async Task CreateFolderAsync(string folderName, string parentPath)
        {
            var folder = SecureFolder.Create(folderName, parentPath);
            var catalog = await _repository.GetCatalogAsync();
            catalog.AddFolder(folder);
            await _repository.SaveCatalogAsync(catalog);
        }

        public async Task DeleteFolderAsync(string folderPath)
        {
            var catalog = await _repository.GetCatalogAsync();
            catalog.RemoveFolder(folderPath);
            await _repository.SaveCatalogAsync(catalog);
        }

        public async Task UpdateKeywordsAsync(string fileId, IEnumerable<string> keywords)
        {
            var catalog = await _repository.GetCatalogAsync();
            var file = catalog.FindFileById(fileId);
            if (file == null) return;

            file.UpdateKeywords(keywords);
            await _repository.SaveCatalogAsync(catalog);
        }

        public async Task<byte[]> GetDecryptedFileAsync(string encryptedFileName)
        {
            return await _repository.GetDecryptedFileAsync(encryptedFileName);
        }

        public List<FileItemInfo> GetFileItems(MediaCatalogInfo catalog, string currentPath, string? searchQuery)
        {
            var items = new List<FileItemInfo>();
            var isSearching = !string.IsNullOrWhiteSpace(searchQuery);

            if (isSearching)
            {
                var query = searchQuery!.ToLowerInvariant();
                var matchingFiles = catalog.Files.Where(f =>
                    f.OriginalFileName.Contains(query, StringComparison.InvariantCultureIgnoreCase) ||
                    f.Keywords.Any(k => k.Contains(query, StringComparison.InvariantCultureIgnoreCase)));

                foreach (var file in matchingFiles.OrderByDescending(f => f.CreatedAt))
                {
                    items.Add(CreateFileItemInfo(file));
                }
            }
            else
            {
                var subFolders = catalog.Folders
                    .Where(f => GetParentPath(f.Path) == currentPath)
                    .OrderBy(f => f.Name);

                foreach (var folder in subFolders)
                {
                    items.Add(new FileItemInfo
                    {
                        Name = folder.Name,
                        Icon = "\ud83d\udcc1",
                        Subtitle = folder.CreatedAt.ToString("dd/MM/yyyy"),
                        IsFolder = true,
                        FolderPath = folder.Path
                    });
                }

                var files = catalog.Files
                    .Where(f => f.FolderPath == currentPath)
                    .OrderByDescending(f => f.CreatedAt);

                foreach (var file in files)
                {
                    items.Add(CreateFileItemInfo(file));
                }
            }

            return items;
        }

        private static FileItemInfo CreateFileItemInfo(MediaFileInfo file)
        {
            var icon = file.MediaType == "video" ? "\ud83c\udfac" : "\ud83d\uddbc\ufe0f";
            var sizeText = FormatFileSize(file.FileSize);
            var keywords = file.Keywords.Count > 0 ? $" | {string.Join(", ", file.Keywords)}" : "";

            return new FileItemInfo
            {
                Name = file.OriginalFileName,
                Icon = icon,
                Subtitle = $"{file.CreatedAt:dd/MM/yyyy HH:mm} | {sizeText}{keywords}",
                IsFolder = false,
                FileId = file.Id,
                MediaType = file.MediaType,
                EncryptedFileName = file.EncryptedFileName
            };
        }

        public static string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
            return $"{bytes / (1024.0 * 1024 * 1024):F1} GB";
        }

        private static string GetParentPath(string path)
        {
            if (path == "/") return "";
            var trimmed = path.TrimEnd('/');
            var lastSlash = trimmed.LastIndexOf('/');
            return lastSlash <= 0 ? "/" : trimmed[..lastSlash];
        }
    }
}
