using KryptoDrive.Pages;
using KryptoDrive.Services;
using KryptoDrive.ViewModels;
using Microsoft.Extensions.Logging;

namespace KryptoDrive
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Services
            builder.Services.AddSingleton<ICryptoService, CryptoService>();
            builder.Services.AddSingleton<IMediaStorageService, MediaStorageService>();
            builder.Services.AddSingleton<GoogleDriveService>();

            // ViewModels
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<FileExplorerViewModel>();
            builder.Services.AddTransient<MediaViewerViewModel>();

            // Pages
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<FileExplorerPage>();
            builder.Services.AddTransient<MediaViewerPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
