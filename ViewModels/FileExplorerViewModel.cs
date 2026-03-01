using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KryptoDrive.Models;
using KryptoDrive.Services;

namespace KryptoDrive.ViewModels
{
    public partial class FileExplorerViewModel : BaseViewModel
    {
        private readonly ICryptoService _cryptoService;
        private readonly IMediaStorageService _storageService;
        private MediaCatalog _catalog = new();

        [ObservableProperty]
        private string _currentPath = "/";

        [ObservableProperty]
        private string _searchQuery = string.Empty;

        [ObservableProperty]
        private bool _isAtRoot = true;

        [ObservableProperty]
        private string _itemCountText = string.Empty;

        public ObservableCollection<FileItem> Items { get; } = new();

        public FileExplorerViewModel(ICryptoService cryptoService, IMediaStorageService storageService)
        {
            _cryptoService = cryptoService;
            _storageService = storageService;
            Title = "KryptoDrive";
        }

        [RelayCommand]
        public async Task LoadFilesAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                _catalog = await _storageService.LoadCatalogAsync();
                RefreshItems();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erro", $"Falha ao carregar arquivos: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void RefreshItems()
        {
            Items.Clear();

            var isSearching = !string.IsNullOrWhiteSpace(SearchQuery);

            if (isSearching)
            {
                var query = SearchQuery.ToLowerInvariant();
                var matchingFiles = _catalog.Files.Where(f =>
                    f.OriginalFileName.Contains(query, StringComparison.InvariantCultureIgnoreCase) ||
                    f.Keywords.Any(k => k.Contains(query, StringComparison.InvariantCultureIgnoreCase)));

                foreach (var file in matchingFiles.OrderByDescending(f => f.CreatedAt))
                {
                    Items.Add(CreateFileItem(file));
                }

                ItemCountText = $"{Items.Count} resultado(s) para \"{SearchQuery}\"";
            }
            else
            {
                // Show folders in current path
                var subFolders = _catalog.Folders
                    .Where(f => GetParentPath(f.Path) == CurrentPath)
                    .OrderBy(f => f.Name);

                foreach (var folder in subFolders)
                {
                    Items.Add(new FileItem
                    {
                        Name = folder.Name,
                        Icon = "\ud83d\udcc1",
                        Subtitle = folder.CreatedAt.ToString("dd/MM/yyyy"),
                        IsFolder = true,
                        FolderPath = folder.Path
                    });
                }

                // Show files in current path
                var files = _catalog.Files
                    .Where(f => f.FolderPath == CurrentPath)
                    .OrderByDescending(f => f.CreatedAt);

                foreach (var file in files)
                {
                    Items.Add(CreateFileItem(file));
                }

                var folderCount = subFolders.Count();
                var fileCount = files.Count();
                ItemCountText = $"{folderCount} pasta(s), {fileCount} arquivo(s)";
            }

            IsAtRoot = CurrentPath == "/";
        }

        private static FileItem CreateFileItem(EncryptedFileInfo file)
        {
            var icon = file.MediaType == "video" ? "\ud83c\udfac" : "\ud83d\uddbc\ufe0f";
            var sizeText = FormatFileSize(file.FileSize);
            var keywords = file.Keywords.Count > 0 ? $" | {string.Join(", ", file.Keywords)}" : "";

            return new FileItem
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

        private static string FormatFileSize(long bytes)
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

        [RelayCommand]
        private void Search(string query)
        {
            SearchQuery = query;
            RefreshItems();
        }

        [RelayCommand]
        private void ClearSearch()
        {
            SearchQuery = string.Empty;
            RefreshItems();
        }

        [RelayCommand]
        private void NavigateToFolder(FileItem item)
        {
            if (!item.IsFolder) return;
            CurrentPath = item.FolderPath;
            SearchQuery = string.Empty;
            RefreshItems();
        }

        [RelayCommand]
        private void GoBack()
        {
            if (CurrentPath == "/") return;
            CurrentPath = GetParentPath(CurrentPath);
            RefreshItems();
        }

        [RelayCommand]
        private async Task CreateFolderAsync()
        {
            var folderName = await Shell.Current.DisplayPromptAsync(
                "Nova Pasta",
                "Digite o nome da pasta:",
                "Criar",
                "Cancelar",
                placeholder: "Nome da pasta");

            if (string.IsNullOrWhiteSpace(folderName)) return;

            // Sanitize folder name
            folderName = folderName.Trim().Replace("/", "_").Replace("\\", "_");

            var fullPath = CurrentPath == "/"
                ? $"/{folderName}"
                : $"{CurrentPath}/{folderName}";

            if (_catalog.Folders.Any(f => f.Path == fullPath))
            {
                await Shell.Current.DisplayAlert("Erro", "Ja existe uma pasta com esse nome.", "OK");
                return;
            }

            _catalog.Folders.Add(new SecureFolder
            {
                Name = folderName,
                Path = fullPath,
                CreatedAt = DateTime.UtcNow
            });

            await _storageService.SaveCatalogAsync(_catalog);
            RefreshItems();
        }

        [RelayCommand]
        private async Task TakePhotoAsync()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.Camera>();
                    if (status != PermissionStatus.Granted)
                    {
                        await Shell.Current.DisplayAlert("Permissao", "Permissao de camera negada.", "OK");
                        return;
                    }
                }

                var photo = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions
                {
                    Title = "Tirar Foto"
                });

                if (photo == null) return;

                await StoreMediaAsync(photo, "photo");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erro", $"Falha ao capturar foto: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task RecordVideoAsync()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.Camera>();
                    if (status != PermissionStatus.Granted)
                    {
                        await Shell.Current.DisplayAlert("Permissao", "Permissao de camera negada.", "OK");
                        return;
                    }
                }

                var video = await MediaPicker.Default.CaptureVideoAsync(new MediaPickerOptions
                {
                    Title = "Gravar Video"
                });

                if (video == null) return;

                await StoreMediaAsync(video, "video");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erro", $"Falha ao gravar video: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task ImportFileAsync()
        {
            try
            {
                var fileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.Android, new[] { "image/*", "video/*" } },
                    { DevicePlatform.iOS, new[] { "public.image", "public.movie" } },
                    { DevicePlatform.WinUI, new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".mp4", ".avi", ".mov", ".wmv" } }
                });

                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Selecionar arquivo para criptografar",
                    FileTypes = fileTypes
                });

                if (result == null) return;

                var extension = Path.GetExtension(result.FileName).ToLowerInvariant();
                var mediaType = IsVideoExtension(extension) ? "video" : "photo";

                await StoreMediaAsync(result, mediaType);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erro", $"Falha ao importar: {ex.Message}", "OK");
            }
        }

        private static bool IsVideoExtension(string ext)
        {
            return ext is ".mp4" or ".avi" or ".mov" or ".wmv" or ".mkv" or ".3gp" or ".webm";
        }

        private async Task StoreMediaAsync(FileResult fileResult, string mediaType)
        {
            IsBusy = true;
            try
            {
                using var stream = await fileResult.OpenReadAsync();

                var encryptedFileName = await _storageService.StoreEncryptedFileAsync(
                    stream, fileResult.FileName, mediaType);

                // Get file size from encrypted file
                var filePath = Path.Combine(_storageService.GetFilesPath(), encryptedFileName);
                var fileSize = new FileInfo(filePath).Length;

                var fileInfo = new EncryptedFileInfo
                {
                    OriginalFileName = fileResult.FileName,
                    EncryptedFileName = encryptedFileName,
                    FolderPath = CurrentPath,
                    MediaType = mediaType,
                    FileExtension = Path.GetExtension(fileResult.FileName),
                    CreatedAt = DateTime.UtcNow,
                    FileSize = fileSize
                };

                _catalog.Files.Add(fileInfo);
                await _storageService.SaveCatalogAsync(_catalog);
                RefreshItems();

                await Shell.Current.DisplayAlert("Sucesso",
                    $"Arquivo \"{fileResult.FileName}\" criptografado e armazenado com sucesso.", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erro", $"Falha ao armazenar: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task OpenFileAsync(FileItem item)
        {
            if (item.IsFolder)
            {
                NavigateToFolder(item);
                return;
            }

            if (item.EncryptedFileName == null) return;

            try
            {
                await Shell.Current.GoToAsync("viewer", new Dictionary<string, object>
                {
                    { "FileId", item.FileId! },
                    { "EncryptedFileName", item.EncryptedFileName },
                    { "OriginalFileName", item.Name },
                    { "MediaType", item.MediaType }
                });
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erro", $"Falha ao abrir arquivo: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task DeleteFileAsync(FileItem item)
        {
            if (item.IsFolder)
            {
                var hasChildren = _catalog.Files.Any(f => f.FolderPath.StartsWith(item.FolderPath)) ||
                                  _catalog.Folders.Any(f => f.Path != item.FolderPath && f.Path.StartsWith(item.FolderPath));

                if (hasChildren)
                {
                    await Shell.Current.DisplayAlert("Erro",
                        "A pasta contem arquivos ou subpastas. Remova-os primeiro.", "OK");
                    return;
                }

                var confirmFolder = await Shell.Current.DisplayAlert("Confirmar",
                    $"Excluir a pasta \"{item.Name}\"?", "Excluir", "Cancelar");

                if (!confirmFolder) return;

                _catalog.Folders.RemoveAll(f => f.Path == item.FolderPath);
                await _storageService.SaveCatalogAsync(_catalog);
                RefreshItems();
                return;
            }

            var confirm = await Shell.Current.DisplayAlert("Confirmar",
                $"Excluir \"{item.Name}\"? Esta acao nao pode ser desfeita.", "Excluir", "Cancelar");

            if (!confirm) return;

            IsBusy = true;
            try
            {
                if (item.EncryptedFileName != null)
                    await _storageService.DeleteFileAsync(item.EncryptedFileName);

                _catalog.Files.RemoveAll(f => f.Id == item.FileId);
                await _storageService.SaveCatalogAsync(_catalog);
                RefreshItems();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erro", $"Falha ao excluir: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task EditKeywordsAsync(FileItem item)
        {
            if (item.IsFolder || item.FileId == null) return;

            var file = _catalog.Files.FirstOrDefault(f => f.Id == item.FileId);
            if (file == null) return;

            var currentKeywords = string.Join(", ", file.Keywords);
            var input = await Shell.Current.DisplayPromptAsync(
                "Palavras-chave",
                "Digite as palavras-chave separadas por virgula:",
                "Salvar",
                "Cancelar",
                initialValue: currentKeywords,
                placeholder: "ex: viagem, familia, praia");

            if (input == null) return;

            file.Keywords = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(k => k.Trim())
                .Where(k => !string.IsNullOrEmpty(k))
                .ToList();

            await _storageService.SaveCatalogAsync(_catalog);
            RefreshItems();
        }

        [RelayCommand]
        private async Task LockVaultAsync()
        {
            _cryptoService.ClearPassword();
            await Shell.Current.GoToAsync("//login");
        }
    }
}
