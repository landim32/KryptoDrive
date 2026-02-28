using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KryptoDrive.Services;

namespace KryptoDrive.ViewModels
{
    public partial class LoginViewModel : BaseViewModel
    {
        private readonly ICryptoService _cryptoService;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string _confirmPassword = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _isNewVault;

        [ObservableProperty]
        private bool _showConfirmPassword;

        public LoginViewModel(ICryptoService cryptoService)
        {
            _cryptoService = cryptoService;
            Title = "KryptoDrive";
            IsNewVault = !_cryptoService.IsVaultInitialized();
            ShowConfirmPassword = IsNewVault;
        }

        [RelayCommand]
        private async Task UnlockAsync()
        {
            if (IsBusy) return;

            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Digite a senha.";
                return;
            }

            if (IsNewVault)
            {
                if (Password.Length < 6)
                {
                    ErrorMessage = "A senha deve ter pelo menos 6 caracteres.";
                    return;
                }

                if (Password != ConfirmPassword)
                {
                    ErrorMessage = "As senhas nao coincidem.";
                    return;
                }

                IsBusy = true;
                try
                {
                    await Task.Run(() => _cryptoService.InitializeVault(Password));
                    await Shell.Current.GoToAsync("//explorer");
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Erro ao criar cofre: {ex.Message}";
                }
                finally
                {
                    IsBusy = false;
                    Password = string.Empty;
                    ConfirmPassword = string.Empty;
                }
            }
            else
            {
                IsBusy = true;
                try
                {
                    var valid = await Task.Run(() => _cryptoService.VerifyPassword(Password));
                    if (valid)
                    {
                        await Shell.Current.GoToAsync("//explorer");
                    }
                    else
                    {
                        ErrorMessage = "Senha incorreta.";
                    }
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Erro: {ex.Message}";
                }
                finally
                {
                    IsBusy = false;
                    Password = string.Empty;
                }
            }
        }
    }
}
