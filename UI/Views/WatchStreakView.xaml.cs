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

        private WatchStreakView()
        {
            InitializeComponent();

            WatchStreakManager.Instance.InitializeWebView(_webView);
            StreamerList.ItemsSource = WatchStreakManager.Instance.Queue;

            WatchStreakManager.Instance.Queue.CollectionChanged += (s, e) => UpdateEmptyHintVisibility();
            UpdateEmptyHintVisibility();

            SelectCurrentPollInterval();
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
