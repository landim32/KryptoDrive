using KryptoDrive.Infra.Interfaces;

namespace KryptoDrive
{
    public partial class App : Application
    {
        private readonly ICryptoService _cryptoService;

        public App(ICryptoService cryptoService)
        {
            InitializeComponent();
            _cryptoService = cryptoService;
            MainPage = new AppShell();
        }

        protected override void OnSleep()
        {
            base.OnSleep();
            _cryptoService.ClearPassword();
        }

        protected override async void OnResume()
        {
            base.OnResume();
            if (!_cryptoService.HasPassword)
            {
                await Shell.Current.GoToAsync("//login");
            }
        }
    }
}
