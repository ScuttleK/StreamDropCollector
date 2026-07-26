using Core.Managers;
using Core.Models;
using System.Windows;
using System.Windows.Controls;

namespace UI.Views
{
    /// <summary>
    /// Interaction logic for WatchStreakView.xaml
    /// </summary>
    public partial class WatchStreakView : System.Windows.Controls.UserControl
    {
        private static readonly Lazy<WatchStreakView> _instance = new(() => new WatchStreakView());
        public static WatchStreakView Instance => _instance.Value;

        private readonly HiddenWebViewHost _webView = new();

        // Guards WatchStreakEnabledCheckBox_CheckedChanged while we set IsChecked from code (constructor,
        // gate refresh) so that doesn't get mistaken for the user toggling it and re-save/re-trigger.
        private bool _isSyncingCheckBoxFromSettings;

        private WatchStreakView()
        {
            InitializeComponent();

            WatchStreakManager.Instance.InitializeWebView(_webView);
            StreamerList.ItemsSource = WatchStreakManager.Instance.Queue;

            WatchStreakManager.Instance.Queue.CollectionChanged += (s, e) => UpdateEmptyHintVisibility();
            UpdateEmptyHintVisibility();

            SelectCurrentPollInterval();

            SetCheckBoxChecked(UISettingsManager.Instance.WatchStreakEnabled);
            RefreshTwitchGate();

            // Instant, event-driven instead of polling a cookie every N seconds - fires the moment
            // Dashboard's own login validation confirms (or loses) a Twitch session.
            DropsInventoryManager.Instance.TwitchConnectionChanged += _ => Dispatcher.InvokeAsync(RefreshTwitchGate);
        }

        private void SetCheckBoxChecked(bool value)
        {
            _isSyncingCheckBoxFromSettings = true;
            try
            {
                WatchStreakEnabledCheckBox.IsChecked = value;
            }
            finally
            {
                _isSyncingCheckBoxFromSettings = false;
            }
        }

        /// <summary>
        /// Updates the enable toggle's clickability and the "Twitch needs to be connected first" hint to
        /// match DropsInventoryManager's live Twitch connection status. Doesn't force the toggle off if
        /// it's already on and Twitch happens to disconnect later - it just becomes un-clickable (grayed
        /// out) until reconnected, rather than silently discarding the user's choice.
        /// </summary>
        private void RefreshTwitchGate()
        {
            bool isConnected = WatchStreakManager.Instance.IsTwitchAccountConnected;

            WatchStreakEnabledCheckBox.IsEnabled = isConnected;
            TwitchRequiredHint.Visibility = isConnected ? Visibility.Collapsed : Visibility.Visible;
        }

        private void WatchStreakEnabledCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (_isSyncingCheckBoxFromSettings)
                return;

            UISettingsManager.Instance.WatchStreakEnabled = WatchStreakEnabledCheckBox.IsChecked == true;
        }

        private void SelectCurrentPollInterval()
        {
            int currentSeconds = WatchStreakManager.Instance.PollIntervalSeconds;

            foreach (object? item in PollIntervalComboBox.Items)
            {
                if (item is ComboBoxItem { Tag: string tag } comboBoxItem &&
                    int.TryParse(tag, out int seconds) && seconds == currentSeconds)
                {
                    PollIntervalComboBox.SelectedItem = comboBoxItem;
                    return;
                }
            }

            // Stored value doesn't match any of the fixed options (e.g. an old default) -- default
            // the dropdown to "Every 30 minutes" without changing the manager's actual setting.
            PollIntervalComboBox.SelectedIndex = 1;
        }

        private void PollIntervalComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PollIntervalComboBox.SelectedItem is ComboBoxItem { Tag: string tag } &&
                int.TryParse(tag, out int seconds))
            {
                WatchStreakManager.Instance.SetPollIntervalSeconds(seconds);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            WatchStreakManager.Instance.RefreshNow();
            RefreshTwitchGate();
        }

        private void UpdateEmptyHintVisibility()
        {
            EmptyQueueHint.Visibility = WatchStreakManager.Instance.Queue.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string input = StreamerUrlInput.Text;
            if (string.IsNullOrWhiteSpace(input))
                return;

            WatchStreakManager.Instance.AddStreamer(input);
            StreamerUrlInput.Text = string.Empty;
        }

        private void StreamerUrlInput_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
                AddButton_Click(sender, e);
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: WatchStreakEntry entry })
                WatchStreakManager.Instance.RemoveStreamer(entry);
        }
    }
}
