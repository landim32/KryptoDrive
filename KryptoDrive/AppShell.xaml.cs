using KryptoDrive.Pages;

namespace KryptoDrive
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute("viewer", typeof(MediaViewerPage));
        }
    }
}
