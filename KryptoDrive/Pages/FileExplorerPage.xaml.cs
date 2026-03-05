using KryptoDrive.ViewModels;

namespace KryptoDrive.Pages
{
    public partial class FileExplorerPage : ContentPage
    {
        private readonly FileExplorerViewModel _viewModel;

        public FileExplorerPage(FileExplorerViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadFilesAsync();
        }
    }
}
