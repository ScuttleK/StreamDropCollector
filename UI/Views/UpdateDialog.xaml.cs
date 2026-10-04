using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using Core;
using Core.Managers;

namespace UI.Views
{
    /// <summary>
    /// App-styled replacement for the plain MessageBox/toast update prompts: shows the installed and latest
    /// version with the published changelog, and runs the download in place with a progress bar.
    /// </summary>
    public partial class UpdateDialog : Window
    {
        private readonly bool _updateAvailable;

        private UpdateDialog(bool updateAvailable)
        {
            InitializeComponent();
            _updateAvailable = updateAvailable;

            string installed = FileVersionInfo.GetVersionInfo(Utility.GetExePath()).FileVersion ?? "?";
            string latest = UISettingsManager.Instance.LatestVersion ?? "?";

            if (updateAvailable)
            {
                TitleText.Text = "Update available";
                VersionText.Text = $"v{installed}  →  v{latest}";
                ChangelogList.ItemsSource = UISettingsManager.Instance.ChangelogSinceInstalled;
                ChangelogPanel.Visibility = UISettingsManager.Instance.ChangelogSinceInstalled.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                TitleText.Text = "You're up to date";
                VersionText.Text = $"v{installed} is the latest version.";
                ChangelogPanel.Visibility = Visibility.Collapsed;
                SecondaryButton.Visibility = Visibility.Collapsed;
                PrimaryButton.Content = "OK";
            }
        }

        /// <summary>
        /// Shows the dialog over <paramref name="owner"/>. With an update available, "Update now" downloads and
        /// installs it (the app restarts on success); otherwise it's a simple "up to date" confirmation.
        /// </summary>
        public static void ShowFor(Window? owner, bool updateAvailable)
        {
            UpdateDialog dialog = new UpdateDialog(updateAvailable);
            if (owner != null && owner.IsVisible)
                dialog.Owner = owner;
            else
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            dialog.ShowDialog();
        }

        private void OnHeaderMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void OnSecondaryClick(object sender, RoutedEventArgs e) => Close();

        private async void OnPrimaryClick(object sender, RoutedEventArgs e)
        {
            if (!_updateAvailable)
            {
                Close();
                return;
            }

            PrimaryButton.IsEnabled = false;
            SecondaryButton.IsEnabled = false;
            ProgressPanel.Visibility = Visibility.Visible;

            void OnProgress(object? s, ProgressEventArgs args) => Dispatcher.InvokeAsync(() =>
            {
                DownloadProgressBar.Value = args.Progress;
                ProgressText.Text = $"Downloading update... {args.Progress}%";
            });

            UpdateManager.Instance.DownloadProgress += OnProgress;
            try
            {
                // Exits the app on success; only returns when the update failed (it reports why itself).
                await UpdateManager.Instance.DownloadUpdate();
            }
            finally
            {
                UpdateManager.Instance.DownloadProgress -= OnProgress;
            }

            ProgressText.Text = "The update couldn't be installed. Please try again later.";
            PrimaryButton.IsEnabled = true;
            SecondaryButton.IsEnabled = true;
        }
    }
}
