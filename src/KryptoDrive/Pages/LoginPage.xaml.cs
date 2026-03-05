using KryptoDrive.ViewModels;

namespace KryptoDrive.Pages
{
    public partial class LoginPage : ContentPage
    {
        public LoginPage(LoginViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is LoginViewModel vm)
            {
                vm.Password = string.Empty;
                vm.ConfirmPassword = string.Empty;
                vm.ErrorMessage = string.Empty;
            }
        }
    }
}
