using System.Windows;
using System.Windows.Input;
using Core.Enums;
using Core.Managers;
using Core.Services;

namespace UI.Views
{
    /// <summary>
    /// First-run "Quick Setup" window. Shown once from <see cref="UI.MainWindow"/> when
    /// <see cref="UISettingsManager.HasCompletedOnboarding"/> is false. All controls bind directly
    /// to <see cref="UISettingsManager.Instance"/>, the same instance <see cref="SettingsView"/>
    /// uses, so there is no separate draft state to reconcile.
    /// </summary>
    public partial class OnboardingWindow : Window
    {
        private const int TotalSteps = 4;
        private int _step = 1;

        // Own validation instances, mirroring the exact pattern DashboardView uses
        // (DashboardView.xaml.cs) - a dedicated hidden WebView host per platform, disposed when
        // this window closes.
        private readonly HiddenWebViewHost _twitchWebView = new();
        private readonly HiddenWebViewHost _kickWebView = new();
        private readonly TwitchLoginService _twitchService = new();
        private readonly KickLoginService _kickService = new();

        public OnboardingWindow()
        {
            InitializeComponent();
            DataContext = UISettingsManager.Instance;
            UpdateStep();
        }

        private void OnTitleBarMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void OnCloseButtonClick(object sender, RoutedEventArgs e) => Close();

        private void OnSkipClick(object sender, RoutedEventArgs e) => Close();

        private void OnBackClick(object sender, RoutedEventArgs e)
        {
            if (_step > 1)
            {
                _step--;
                UpdateStep();
            }
        }

        private void OnNextClick(object sender, RoutedEventArgs e)
        {
            if (_step < TotalSteps)
            {
                _step++;
                UpdateStep();
            }
            else
            {
                Close();
            }
        }

        private async void OnTwitchLoginClick(object sender, RoutedEventArgs e)
        {
            new TwitchLoginWindow().ShowDialog();

            TwitchStatusText.Text = "Checking...";
            TwitchStatusText.Foreground = (System.Windows.Media.Brush)FindResource("TextMutedBrush");

            await _twitchService.ValidateCredentialsAsync(_twitchWebView);
            SetConnectionStatus(TwitchStatusText, _twitchService.Status == ConnectionStatus.Connected);
        }

        private async void OnKickLoginClick(object sender, RoutedEventArgs e)
        {
            new KickLoginWindow().ShowDialog();

            KickStatusText.Text = "Checking...";
            KickStatusText.Foreground = (System.Windows.Media.Brush)FindResource("TextMutedBrush");

            await _kickService.ValidateCredentialsAsync(_kickWebView);
            SetConnectionStatus(KickStatusText, _kickService.Status == ConnectionStatus.Connected);
        }

        private void SetConnectionStatus(System.Windows.Controls.TextBlock target, bool connected)
        {
            target.Text = connected ? "Connected" : "Failed to Connect";
            target.Foreground = (System.Windows.Media.Brush)FindResource(connected ? "SuccessBrush" : "DangerBrush");
        }

        private void UpdateStep()
        {
            Step1Panel.Visibility = _step == 1 ? Visibility.Visible : Visibility.Collapsed;
            Step2Panel.Visibility = _step == 2 ? Visibility.Visible : Visibility.Collapsed;
            Step3Panel.Visibility = _step == 3 ? Visibility.Visible : Visibility.Collapsed;
            Step4Panel.Visibility = _step == 4 ? Visibility.Visible : Visibility.Collapsed;

            StepLabel.Text = $"Step {_step}/{TotalSteps}";
            BackButton.IsEnabled = _step > 1;
            NextButton.Content = _step == TotalSteps ? "Get Started" : "Next";
        }

        /// <summary>
        /// However the window closes - Skip, Finish, the X button, or Alt+F4 - it should never be shown again.
        /// </summary>
        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            UISettingsManager.Instance.HasCompletedOnboarding = true;
            _twitchWebView.Dispose();
            _kickWebView.Dispose();
        }
    }
}
