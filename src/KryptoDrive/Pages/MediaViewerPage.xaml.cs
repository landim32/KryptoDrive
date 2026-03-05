using KryptoDrive.ViewModels;

namespace KryptoDrive.Pages
{
    public partial class MediaViewerPage : ContentPage
    {
        private readonly MediaViewerViewModel _viewModel;

        public MediaViewerPage(MediaViewerViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadMediaAsync();
        }
    }
}
