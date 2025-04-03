using Google.Apis.Drive.v3.Data;
using KryptoDrive.Services;

namespace KryptoDrive
{
    public partial class MainPage : ContentPage
    {
        readonly GoogleDriveService _googleDriveService = new();

        public MainPage()
        {
            InitializeComponent();
        }

        private async void ContentPage_Loaded(object sender, EventArgs e)
        {
            await _googleDriveService.Init();
            UpdateButton();
        }

        private async void SignIn_Clicked(object sender, EventArgs e)
        {
            if (SignInButton.Text == "Sign In")
            {
                await _googleDriveService.SignIn();
            }
            else
            {
                await _googleDriveService.SignOut();

            }
            UpdateButton();
        }

        private async void List_Clicked(object sender, EventArgs e)
        {
            //FileListView.ItemSource = await _googleDriveService.ListFiles();
            ListLabel.Text = await _googleDriveService.ListFiles();
        }

        private void UpdateButton()
        {
            if (_googleDriveService.IsSignedIn)
            {
                SignInButton.Text = $"Sign Out ({_googleDriveService.Email})";
                ListButton.IsVisible = true;
            }
            else
            {
                SignInButton.Text = "Sign In";
                ListButton.IsVisible = false;
                ListLabel.Text = String.Empty;
            }
        }
    }

}
