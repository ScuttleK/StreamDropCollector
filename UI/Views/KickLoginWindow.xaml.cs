using System.Windows;

namespace UI.Views
{
    /// <summary>
    /// Interaction logic for KickLoginWindow.xaml
    /// </summary>
    public partial class KickLoginWindow : Window
    {
        public KickLoginWindow()
        {
            InitializeComponent();
            Initialize();
        }

        private async void Initialize()
        {
            try
            {
                await Web.EnsureCoreWebView2Async();

                Web.Source = new Uri("https://kick.com");
                Web.NavigationCompleted += Web_NavigationCompleted;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Couldn't start the Kick login browser. This usually means the WebView2 runtime isn't installed or is corrupted.\n\n{ex.Message}",
                    "Kick Login Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Close();
            }
        }

        private async void Web_NavigationCompleted(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            // Only the first load (the login page itself) needs the auto-click - unsubscribe immediately so
            // later navigations (post-login redirect, 2FA, etc.) don't keep re-running this script.
            Web.NavigationCompleted -= Web_NavigationCompleted;
            await Web.ExecuteScriptAsync("document.querySelector('[data-testid=\"login\"]')?.click();");
        }
    }
}
