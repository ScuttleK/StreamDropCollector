using System.Windows;

namespace UI.Views
{
    /// <summary>
    /// Interaction logic for TwitchLoginWindow.xaml
    /// </summary>
    public partial class TwitchLoginWindow : Window
    {
        public TwitchLoginWindow()
        {
            InitializeComponent();
            Initialize();
        }

        private async void Initialize()
        {
            try
            {
                await Web.EnsureCoreWebView2Async();
                Web.Source = new Uri("https://twitch.tv/login");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Couldn't start the Twitch login browser. This usually means the WebView2 runtime isn't installed or is corrupted.\n\n{ex.Message}",
                    "Twitch Login Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Close();
            }
        }
    }
}
