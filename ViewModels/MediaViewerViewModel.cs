using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KryptoDrive.Services;

namespace KryptoDrive.ViewModels
{
    [QueryProperty(nameof(FileId), "FileId")]
    [QueryProperty(nameof(EncryptedFileName), "EncryptedFileName")]
    [QueryProperty(nameof(OriginalFileName), "OriginalFileName")]
    [QueryProperty(nameof(MediaType), "MediaType")]
    public partial class MediaViewerViewModel : BaseViewModel
    {
        private readonly IMediaStorageService _storageService;

        [ObservableProperty]
        private string _fileId = string.Empty;

        [ObservableProperty]
        private string _encryptedFileName = string.Empty;

        [ObservableProperty]
        private string _originalFileName = string.Empty;

        [ObservableProperty]
        private string _mediaType = "photo";

        [ObservableProperty]
        private ImageSource? _imageSource;

        [ObservableProperty]
        private bool _isPhoto;

        [ObservableProperty]
        private bool _isVideo;

        [ObservableProperty]
        private string _statusText = string.Empty;

        public MediaViewerViewModel(IMediaStorageService storageService)
        {
            _storageService = storageService;
            Title = "Visualizar";
        }

        [RelayCommand]
        public async Task LoadMediaAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusText = "Descriptografando...";

            try
            {
                var decryptedData = await _storageService.LoadDecryptedFileAsync(EncryptedFileName);

                IsPhoto = MediaType == "photo";
                IsVideo = MediaType == "video";
                Title = OriginalFileName;

                if (IsPhoto)
                {
                    ImageSource = ImageSource.FromStream(() => new MemoryStream(decryptedData));
                    StatusText = $"{OriginalFileName} | {FormatFileSize(decryptedData.Length)}";
                }
                else
                {
                    StatusText = $"{OriginalFileName} | {FormatFileSize(decryptedData.Length)} | Toque para reproduzir";
                }
            }
            catch (Exception ex)
            {
                StatusText = $"Erro ao carregar: {ex.Message}";
                await Shell.Current.DisplayAlert("Erro", $"Falha ao descriptografar: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task PlayVideoAsync()
        {
            if (!IsVideo || string.IsNullOrEmpty(EncryptedFileName)) return;

            IsBusy = true;
            StatusText = "Preparando video...";

            try
            {
                var decryptedData = await _storageService.LoadDecryptedFileAsync(EncryptedFileName);

                // Save to temp file for playback
                var tempDir = Path.Combine(FileSystem.CacheDirectory, "temp_playback");
                Directory.CreateDirectory(tempDir);

                var extension = Path.GetExtension(OriginalFileName);
                if (string.IsNullOrEmpty(extension)) extension = ".mp4";

                var tempPath = Path.Combine(tempDir, $"playback{extension}");

                // Clean up any previous temp files
                if (File.Exists(tempPath))
                    File.Delete(tempPath);

                await File.WriteAllBytesAsync(tempPath, decryptedData);

                await Launcher.Default.OpenAsync(new OpenFileRequest
                {
                    File = new ReadOnlyFile(tempPath)
                });

                StatusText = $"{OriginalFileName} | Reproduzindo...";

                // Schedule cleanup after delay
                _ = Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromMinutes(5));
                    try
                    {
                        if (File.Exists(tempPath))
                            File.Delete(tempPath);
                    }
                    catch { /* ignore cleanup errors */ }
                });
            }
            catch (Exception ex)
            {
                StatusText = $"Erro ao reproduzir: {ex.Message}";
                await Shell.Current.DisplayAlert("Erro", $"Falha ao reproduzir video: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }

        private static string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
            return $"{bytes / (1024.0 * 1024 * 1024):F1} GB";
        }
    }
}
