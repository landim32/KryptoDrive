using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KryptoDrive.DTO.DTOs;
using KryptoDrive.Infra.Interfaces;

namespace KryptoDrive.ViewModels
{
    public partial class FileExplorerViewModel : BaseViewModel
    {
        private readonly ICryptoService _cryptoService;
        private readonly IVaultAppService _vaultAppService;
        private MediaCatalogInfo _catalog = new();

        [ObservableProperty]
        private string _currentPath = "/";

        [ObservableProperty]
        private string _searchQuery = string.Empty;

        [ObservableProperty]
        private bool _isAtRoot = true;

        [ObservableProperty]
        private string _itemCountText = string.Empty;

        public ObservableCollection<FileItemInfo> Items { get; } = new();

        public FileExplorerViewModel(ICryptoService cryptoService, IVaultAppService vaultAppService)
        {
            _cryptoService = cryptoService;
            _vaultAppService = vaultAppService;
            Title = "KryptoDrive";
        }

        [RelayCommand]
        public async Task LoadFilesAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                _catalog = await _vaultAppService.GetCatalogAsync();
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
            var fileItems = _vaultAppService.GetFileItems(_catalog, CurrentPath, isSearching ? SearchQuery : null);

            foreach (var item in fileItems)
            {
                Items.Add(item);
            }

            if (isSearching)
            {
                ItemCountText = $"{Items.Count} resultado(s) para \"{SearchQuery}\"";
            }
            else
            {
                var folderCount = fileItems.Count(i => i.IsFolder);
                var fileCount = fileItems.Count(i => !i.IsFolder);
                ItemCountText = $"{folderCount} pasta(s), {fileCount} arquivo(s)";
            }

            IsAtRoot = CurrentPath == "/";
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
        private void NavigateToFolder(FileItemInfo item)
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

            try
            {
                await _vaultAppService.CreateFolderAsync(folderName, CurrentPath);
                _catalog = await _vaultAppService.GetCatalogAsync();
                RefreshItems();
            }
            catch (InvalidOperationException ex)
            {
                await Shell.Current.DisplayAlert("Erro", ex.Message, "OK");
            }
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

                await _vaultAppService.StoreMediaAsync(stream, fileResult.FileName, mediaType, CurrentPath);

                _catalog = await _vaultAppService.GetCatalogAsync();
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
        private async Task OpenFileAsync(FileItemInfo item)
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
        private async Task DeleteFileAsync(FileItemInfo item)
        {
            if (item.IsFolder)
            {
                var confirmFolder = await Shell.Current.DisplayAlert("Confirmar",
                    $"Excluir a pasta \"{item.Name}\"?", "Excluir", "Cancelar");

                if (!confirmFolder) return;

                try
                {
                    await _vaultAppService.DeleteFolderAsync(item.FolderPath);
                    _catalog = await _vaultAppService.GetCatalogAsync();
                    RefreshItems();
                }
                catch (InvalidOperationException ex)
                {
                    await Shell.Current.DisplayAlert("Erro", ex.Message, "OK");
                }
                return;
            }

            var confirm = await Shell.Current.DisplayAlert("Confirmar",
                $"Excluir \"{item.Name}\"? Esta acao nao pode ser desfeita.", "Excluir", "Cancelar");

            if (!confirm) return;

            IsBusy = true;
            try
            {
                await _vaultAppService.DeleteFileAsync(item.FileId!, item.EncryptedFileName);
                _catalog = await _vaultAppService.GetCatalogAsync();
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
        private async Task EditKeywordsAsync(FileItemInfo item)
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

            var keywords = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            await _vaultAppService.UpdateKeywordsAsync(item.FileId, keywords);
            _catalog = await _vaultAppService.GetCatalogAsync();
            RefreshItems();
        }

        [RelayCommand]
        private async Task LockVaultAsync()
        {
            _cryptoService.ClearPassword();
            await Shell.Current.GoToAsync("//login");
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
