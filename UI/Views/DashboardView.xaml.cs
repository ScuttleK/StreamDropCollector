using UserControl = System.Windows.Controls.UserControl;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using DragEventArgs = System.Windows.DragEventArgs;
using DataObject = System.Windows.DataObject;
using DragDropEffects = System.Windows.DragDropEffects;
using Point = System.Windows.Point;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Documents;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using System.Windows.Input;
using System.ComponentModel;
using System.Windows;
using Core.Logging;
using Core.Managers;
using Core.Services;
using Core.Models;
using Core.Enums;

namespace UI.Views
{
    /// <summary>
    /// Interaction logic for DashboardView.xaml
    /// </summary>
    public partial class DashboardView : UserControl, INotifyPropertyChanged
    {
        private readonly System.Timers.Timer _refreshTimer = new(TimeSpan.FromHours(1).TotalMilliseconds);

        private readonly SemaphoreSlim _loadDropsSemaphore = new(1, 1);
        private CancellationTokenSource? _currentLoadCts;
        private readonly object _loadTriggerLock = new();
        private bool _loadScheduled = false;

        private HiddenWebViewHost _twitchWebView = new();
        private HiddenWebViewHost _kickWebView = new();
        private TwitchGqlService? _twitchGqlService;

        private static bool _initialValidationCompleted = false;
        private static bool _isInitialized = false;

        private static readonly Lazy<DashboardView> _instance = new(() => new DashboardView());
        public static DashboardView Instance => _instance.Value;

        // Services
        private readonly TwitchLoginService _twitchService = new();
        private readonly KickLoginService _kickService = new();
        private readonly DropsService _dropsService;

        // Queue collections — split into watching and completed sections
        private readonly ObservableCollection<QueueCampaignItem> _watchingItems = new();
        private readonly ObservableCollection<QueueCampaignItem> _completedItems = new();
        public IReadOnlyCollection<QueueCampaignItem> WatchingItems => _watchingItems;
        public IReadOnlyCollection<QueueCampaignItem> CompletedItems => _completedItems;
        public int WatchingCount => _watchingItems.Count;
        public int CompletedCount => _completedItems.Count;

        private bool _isWatchingExpanded = true;
        public bool IsWatchingExpanded { get => _isWatchingExpanded; set { _isWatchingExpanded = value; OnPropertyChanged(); } }
        private bool _isCompletedExpanded = false;
        public bool IsCompletedExpanded { get => _isCompletedExpanded; set { _isCompletedExpanded = value; OnPropertyChanged(); } }

        // Drag & drop state
        private QueueCampaignItem? _dragItem;
        private FrameworkElement? _dragCardElement;
        private Point _dragOrigin;
        private Point _dragOffsetInCard;
        private bool _isDragging;
        private AdornerLayer? _adornerLayer;
        private DragCardAdorner? _dragAdorner;
        private const double DragThreshold = 6.0;

        // UI Properties
        private string _twitchConnectionStatus = "Not Connected";
        public string TwitchConnectionStatus
        {
            get => _twitchConnectionStatus;
            set
            {
                _twitchConnectionStatus = value;
                OnPropertyChanged();
            }
        }
        private string _twitchConnectionColor = "Red";
        public string TwitchConnectionColor
        {
            get => _twitchConnectionColor;
            set
            {
                _twitchConnectionColor = value;
                OnPropertyChanged();
            }
        }

        private string _kickConnectionStatus = "Not Connected";
        public string KickConnectionStatus
        {
            get => _kickConnectionStatus;
            set
            {
                _kickConnectionStatus = value;
                OnPropertyChanged();
            }
        }
        private string _kickConnectionColor = "Red";
        public string KickConnectionColor
        {
            get => _kickConnectionColor;
            set
            {
                _kickConnectionColor = value;
                OnPropertyChanged();
            }
        }

        private string _minerStatus = "Idle";
        public string MinerStatus
        {
            get => _minerStatus;
            set
            {
                _minerStatus = value;
                OnPropertyChanged();
            }
        }
        private string _minerStatusDetails = "Waiting";
        public string MinerStatusDetails
        {
            get => _minerStatusDetails;
            set
            {
                _minerStatusDetails = value;
                OnPropertyChanged();
            }
        }
        private byte _twitchCampaignProgress = 0;
        public byte TwitchCampaignProgress
        {
            get => _twitchCampaignProgress;
            set
            {
                _twitchCampaignProgress = value;
                OnPropertyChanged();
            }
        }
        private byte _twitchDropProgress = 0;
        public byte TwitchDropProgress
        {
            get => _twitchDropProgress;
            set
            {
                _twitchDropProgress = value;
                OnPropertyChanged();
            }
        }
        private byte _kickCampaignProgress = 0;
        public byte KickCampaignProgress
        {
            get => _kickCampaignProgress;
            set
            {
                _kickCampaignProgress = value;
                OnPropertyChanged();
            }
        }
        private byte _kickDropProgress = 0;
        public byte KickDropProgress
        {
            get => _kickDropProgress;
            set
            {
                _kickDropProgress = value;
                OnPropertyChanged();
            }
        }
        private string _twitchWatchedChannel = string.Empty;
        public string TwitchWatchedChannel
        {
            get => _twitchWatchedChannel;
            set
            {
                _twitchWatchedChannel = value;
                OnPropertyChanged();
            }
        }
        private string _kickWatchedChannel = string.Empty;
        public string KickWatchedChannel
        {
            get => _kickWatchedChannel;
            set
            {
                _kickWatchedChannel = value;
                OnPropertyChanged();
            }
        }
        private string _twitchCampaignName = string.Empty;
        public string TwitchCampaignName
        {
            get => _twitchCampaignName;
            set
            {
                _twitchCampaignName = value;
                OnPropertyChanged();
            }
        }
        private string _kickCampaignName = string.Empty;
        public string KickCampaignName
        {
            get => _kickCampaignName;
            set
            {
                _kickCampaignName = value;
                OnPropertyChanged();
            }
        }
        private string _twitchCampaignImageUrl = string.Empty;
        public string TwitchCampaignImageUrl
        {
            get => _twitchCampaignImageUrl;
            set
            {
                _twitchCampaignImageUrl = value;
                OnPropertyChanged();
            }
        }
        private string _kickCampaignImageUrl = string.Empty;
        public string KickCampaignImageUrl
        {
            get => _kickCampaignImageUrl;
            set
            {
                _kickCampaignImageUrl = value;
                OnPropertyChanged();
            }
        }
        private string _twitchDropName = string.Empty;
        public string TwitchDropName
        {
            get => _twitchDropName;
            set
            {
                _twitchDropName = value;
                OnPropertyChanged();
            }
        }
        private string _twitchDropImageUrl = string.Empty;
        public string TwitchDropImageUrl
        {
            get => _twitchDropImageUrl;
            set
            {
                _twitchDropImageUrl = value;
                OnPropertyChanged();
            }
        }
        private string _kickDropName = string.Empty;
        public string KickDropName
        {
            get => _kickDropName;
            set
            {
                _kickDropName = value;
                OnPropertyChanged();
            }
        }
        private string _kickDropImageUrl = string.Empty;
        public string KickDropImageUrl
        {
            get => _kickDropImageUrl;
            set
            {
                _kickDropImageUrl = value;
                OnPropertyChanged();
            }
        }

        private DateTimeOffset? _twitchCampaignEndsAt;
        public DateTimeOffset? TwitchCampaignEndsAt
        {
            get => _twitchCampaignEndsAt;
            set
            {
                _twitchCampaignEndsAt = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TwitchExpiryText));
                OnPropertyChanged(nameof(TwitchExpiryVisible));
                OnPropertyChanged(nameof(TwitchExpiryIsWarning));
                OnPropertyChanged(nameof(TwitchExpiryIsCritical));
            }
        }
        public bool TwitchExpiryVisible => _twitchCampaignEndsAt.HasValue;
        public string TwitchExpiryText => _twitchCampaignEndsAt.HasValue ? FormatExpiryText(_twitchCampaignEndsAt.Value) : string.Empty;
        public bool TwitchExpiryIsWarning => GetDaysRemaining(_twitchCampaignEndsAt) is >= 3 and < 6;
        public bool TwitchExpiryIsCritical => GetDaysRemaining(_twitchCampaignEndsAt) is >= 0 and < 3;

        private DateTimeOffset? _kickCampaignEndsAt;
        public DateTimeOffset? KickCampaignEndsAt
        {
            get => _kickCampaignEndsAt;
            set
            {
                _kickCampaignEndsAt = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(KickExpiryText));
                OnPropertyChanged(nameof(KickExpiryVisible));
                OnPropertyChanged(nameof(KickExpiryIsWarning));
                OnPropertyChanged(nameof(KickExpiryIsCritical));
            }
        }
        public bool KickExpiryVisible => _kickCampaignEndsAt.HasValue;
        public string KickExpiryText => _kickCampaignEndsAt.HasValue ? FormatExpiryText(_kickCampaignEndsAt.Value) : string.Empty;
        public bool KickExpiryIsWarning => GetDaysRemaining(_kickCampaignEndsAt) is >= 3 and < 6;
        public bool KickExpiryIsCritical => GetDaysRemaining(_kickCampaignEndsAt) is >= 0 and < 3;

        private static double GetDaysRemaining(DateTimeOffset? endsAt) =>
            endsAt.HasValue ? (endsAt.Value - DateTimeOffset.UtcNow).TotalDays : double.MaxValue;
        private static string FormatExpiryText(DateTimeOffset endsAt)
        {
            int days = (int)Math.Max(0, (endsAt.LocalDateTime.Date - DateTime.Now.Date).TotalDays);
            return $"Ends {endsAt.LocalDateTime:MMM d} · {days}d left";
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        /// <remarks>This event is typically raised by the implementation of the INotifyPropertyChanged
        /// interface to notify subscribers that a property value has changed. Handlers receive the name of the property
        /// that changed in the event data. This event is commonly used in data binding scenarios to update UI elements
        /// when underlying data changes.</remarks>
        public event PropertyChangedEventHandler? PropertyChanged;
        /// <summary>
        /// Raises the PropertyChanged event to notify listeners that a property value has changed.
        /// </summary>
        /// <remarks>Use this method to implement the INotifyPropertyChanged interface in classes that
        /// support data binding. Calling this method with the correct property name ensures that UI elements or other
        /// listeners are updated when the property value changes.</remarks>
        /// <param name="name">The name of the property that changed. This value is optional and is automatically provided when called from
        /// a property setter.</param>
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        /// <summary>
        /// Initializes a new instance of the DashboardView class and sets up event handlers for login status changes.
        /// </summary>
        /// <remarks>This constructor sets the data context to the current instance and subscribes to
        /// login status events for both Kick and Twitch platforms. Event handlers are automatically unsubscribed when
        /// the view is unloaded to prevent memory leaks.</remarks>
        private DashboardView()
        {
            InitializeComponent();
            DataContext = this;

            MinerStatus = "Initializing";
            MinerStatusDetails = "Please wait...";

            _twitchService = new TwitchLoginService();
            _kickService = new KickLoginService();

            _dropsService = new DropsService();

            _twitchGqlService = new TwitchGqlService(_twitchWebView);

            _watchingItems.CollectionChanged += (_, _) => OnPropertyChanged(nameof(WatchingCount));
            _completedItems.CollectionChanged += (_, _) => OnPropertyChanged(nameof(CompletedCount));

            // Subscribe to progress updates ===
            DropsInventoryManager.Instance.TwitchProgressChanged += (campPct, dropPct) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    TwitchCampaignProgress = campPct;
                    TwitchDropProgress = dropPct;
                });
            };

            DropsInventoryManager.Instance.KickProgressChanged += (campPct, dropPct) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    KickCampaignProgress = campPct;
                    KickDropProgress = dropPct;
                });
            };

            DropsInventoryManager.Instance.MinerStatusChanged += status =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    switch (status)
                    {
                        case "Idle":
                            MinerStatus = "Idle";
                            MinerStatusDetails = "Waiting for drops";
                            break;
                        case "Starting":
                            MinerStatus = "Starting";
                            MinerStatusDetails = "Finding stream(s) to watch";
                            break;
                        case "Evaluating":
                            MinerStatus = "Evaluating";
                            MinerStatusDetails = "Checking stream(s) for drops eligibility";
                            break;
                        case "Mining":
                            MinerStatus = "Mining";
                            MinerStatusDetails = "Watching stream(s) to earn drops";
                            break;
                    }
                });
            };

            DropsInventoryManager.Instance.KickChannelChanged += channel =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    KickWatchedChannel = channel;
                    var active = _watchingItems.FirstOrDefault(i => i.Platform == Platform.Kick && i.IsActive);
                    if (active != null)
                    {
                        active.StreamChannel = channel;
                        UpdateWatchingStreamer(active, channel);
                    }
                });
            };

            DropsInventoryManager.Instance.TwitchChannelChanged += channel =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    TwitchWatchedChannel = channel;
                    var active = _watchingItems.FirstOrDefault(i => i.Platform == Platform.Twitch && i.IsActive);
                    if (active != null)
                    {
                        active.StreamChannel = channel;
                        UpdateWatchingStreamer(active, channel);
                    }
                });
            };

            DropsInventoryManager.Instance.KickCampaignChanged += (campaign, imageUrl) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    KickCampaignName = campaign;
                    KickCampaignImageUrl = imageUrl ?? string.Empty;
                    var kickActive = _watchingItems.FirstOrDefault(i => i.Platform == Platform.Kick &&
                        string.Equals(i.Name, campaign, StringComparison.OrdinalIgnoreCase));
                    KickCampaignEndsAt = !string.IsNullOrEmpty(campaign) ? kickActive?.Campaign.EndsAt : null;
                    foreach (var item in _watchingItems.Where(i => i.Platform == Platform.Kick))
                    {
                        bool nowActive = !string.IsNullOrEmpty(campaign) &&
                            string.Equals(item.Name, campaign, StringComparison.OrdinalIgnoreCase);
                        item.IsActive = nowActive;
                        if (nowActive && !string.IsNullOrEmpty(KickWatchedChannel))
                        {
                            item.StreamChannel = KickWatchedChannel;
                            UpdateWatchingStreamer(item, KickWatchedChannel);
                        }
                        else if (!nowActive)
                        {
                            item.StreamChannel = string.Empty;
                            foreach (var s in item.StreamerStatuses) s.IsWatching = false;
                        }
                    }
                });
            };

            DropsInventoryManager.Instance.TwitchCampaignChanged += (campaign, imageUrl) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    TwitchCampaignName = campaign;
                    TwitchCampaignImageUrl = imageUrl ?? string.Empty;
                    var twitchActive = _watchingItems.FirstOrDefault(i => i.Platform == Platform.Twitch &&
                        string.Equals(i.Name, campaign, StringComparison.OrdinalIgnoreCase));
                    TwitchCampaignEndsAt = !string.IsNullOrEmpty(campaign) ? twitchActive?.Campaign.EndsAt : null;
                    foreach (var item in _watchingItems.Where(i => i.Platform == Platform.Twitch))
                    {
                        bool nowActive = !string.IsNullOrEmpty(campaign) &&
                            string.Equals(item.Name, campaign, StringComparison.OrdinalIgnoreCase);
                        item.IsActive = nowActive;
                        if (nowActive && !string.IsNullOrEmpty(TwitchWatchedChannel))
                        {
                            item.StreamChannel = TwitchWatchedChannel;
                            UpdateWatchingStreamer(item, TwitchWatchedChannel);
                        }
                        else if (!nowActive)
                        {
                            item.StreamChannel = string.Empty;
                            foreach (var s in item.StreamerStatuses) s.IsWatching = false;
                        }
                    }
                });
            };

            DropsInventoryManager.Instance.KickDropChanged += (drop, imageUrl) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    KickDropName = drop;
                    KickDropImageUrl = imageUrl ?? string.Empty;
                });
            };

            DropsInventoryManager.Instance.TwitchDropChanged += (drop, imageUrl) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    TwitchDropName = drop;
                    TwitchDropImageUrl = imageUrl ?? string.Empty;
                });
            };

            // Re-filter the queue whenever game filter settings change
            UISettingsManager.Instance.GameWhitelistChanged += _ =>
            {
                Dispatcher.InvokeAsync(() =>
                {
                    foreach (var item in _watchingItems.Where(i => !UISettingsManager.Instance.IsCampaignAllowedByWhitelist(i.Campaign)).ToList())
                        _watchingItems.Remove(item);
                    foreach (var item in _completedItems.Where(i => !UISettingsManager.Instance.IsCampaignAllowedByWhitelist(i.Campaign)).ToList())
                        _completedItems.Remove(item);
                    DropsInventoryManager.Instance.SetUserCampaignOrder(_watchingItems.Select(i => i.Id));
                });
            };

            DropsInventoryManager.Instance.CampaignCompleted += campaignId =>
            {
                Dispatcher.InvokeAsync(() =>
                {
                    var item = _watchingItems.FirstOrDefault(i => i.Id == campaignId);
                    if (item != null)
                    {
                        _watchingItems.Remove(item);
                        item.CompletedAt = DateTime.Now;
                        item.WasSkippedOffline = false;
                        _completedItems.Insert(0, item);
                        IsCompletedExpanded = true;
                    }
                });
            };

            DropsInventoryManager.Instance.CampaignsSkippedOffline += skippedIds =>
            {
                Dispatcher.InvokeAsync(() =>
                {
                    foreach (var item in _watchingItems)
                        item.WasSkippedOffline = skippedIds.Contains(item.Id);
                });
            };

            Loaded += async (s, e) => await OnLoadedAsync();
        }

        /// <summary>
        /// Asynchronously refreshes the list of active drops campaigns by retrieving the latest campaigns from the
        /// drops service.
        /// </summary>
        /// <remarks>After calling this method, the active campaigns list is updated to reflect the
        /// current set of active drops campaigns. Any previously stored campaigns are cleared before the new campaigns
        /// are added. This method should be awaited to ensure the refresh completes before accessing the updated
        /// campaigns.</remarks>
        /// <returns>A task that represents the asynchronous refresh operation.</returns>
        public async Task StartAutoRefreshDropsAsync()
        {
            ScheduleDropsLoad();

            _refreshTimer.Elapsed += async (s, e) => await Dispatcher.InvokeAsync(() => ScheduleDropsLoad());
            _refreshTimer.AutoReset = true; // Run forever
            _refreshTimer.Start();
        }
        /// <summary>
        /// Schedules a debounced background load of drops, ensuring that rapid consecutive triggers result in a single
        /// load operation after a delay.
        /// </summary>
        /// <remarks>This method prevents multiple load operations from being scheduled in quick
        /// succession by introducing a 2-second debounce period. It is thread-safe and intended to be called when a
        /// load should be triggered, but only after a period of inactivity. The actual load is performed asynchronously
        /// on the background dispatcher priority.</remarks>
        private void ScheduleDropsLoad()
        {
            // Block all loads until initial validation is done.
            if (!_initialValidationCompleted)
                return;

            lock (_loadTriggerLock)
            {
                if (_loadScheduled) return; // already scheduled
                _loadScheduled = true;
            }

            // Fire once, after 300ms of calm (debounced)
            Dispatcher.InvokeAsync(async () =>
            {
                await Task.Delay(300); // absorb any rapid-fire triggers

                lock (_loadTriggerLock)
                {
                    _loadScheduled = false;
                }

                _ = LoadDropsAsync(); // safe - semaphore still protects concurrency
            }, DispatcherPriority.Background);
        }
        /// <summary>
        /// Asynchronously loads the list of active drops campaigns and updates the miner status properties to reflect
        /// the current loading state.
        /// </summary>
        /// <remarks>If a previous load operation is in progress, it will be canceled before starting a
        /// new one. The method updates status properties to indicate progress and results, including error messages if
        /// loading fails. This method should be called when the application needs to refresh the list of available
        /// campaigns.</remarks>
        /// <returns>A task that represents the asynchronous operation of loading active drops campaigns.</returns>
        private async Task LoadDropsAsync()
        {
            // Cancel any previous in-flight load
            _currentLoadCts?.Cancel();
            AppLogger.Info("Dashboard", "LoadDropsAsync invoked; previous load cancellation requested if active.");

            // Wait if another load is already running
            await _loadDropsSemaphore.WaitAsync();
            try
            {
                await DropsInventoryManager.Instance.PauseWatchingAsync();
                AppLogger.Info("Dashboard", "Watcher paused for campaign refresh.");

                using CancellationTokenSource cts = new CancellationTokenSource();
                _currentLoadCts = cts;

                if (_kickService.Status != ConnectionStatus.Connected && _twitchService.Status != ConnectionStatus.Connected)
                {
                    AppLogger.Warn("Dashboard", "Campaign load skipped: neither Twitch nor Kick is connected.");
                    MinerStatus = "Need login";
                    MinerStatusDetails = "Please login to Twitch and/or Kick to load campaigns.";
                    return;
                }

                MinerStatus = "Loading Campaigns";
                MinerStatusDetails = "Fetching latest drops...";

                // Save current order so hourly refresh preserves user's custom arrangement
                var existingWatchingIds = _watchingItems.Select(i => i.Id).ToList();
                var previousCompletedAt = _completedItems.ToDictionary(i => i.Id, i => i.CompletedAt);
                _watchingItems.Clear();
                _completedItems.Clear();

                IReadOnlyList<DropsCampaign> allCampaigns = await _dropsService.GetAllActiveCampaignsAsync(_kickWebView, _kickService.Status, _twitchWebView, _twitchService.Status, _twitchGqlService, cts.Token);
                AppLogger.Info("Dashboard", $"Campaign load completed. totalCampaigns={allCampaigns.Count}, twitchStatus={_twitchService.Status}, kickStatus={_kickService.Status}");

                var allowed = allCampaigns
                    .Where(c => UISettingsManager.Instance.IsCampaignAllowedByWhitelist(c))
                    .Select(c => new QueueCampaignItem(c))
                    .ToList();

                // Watching: preserve user order for known campaigns; new campaigns go last (generals very last)
                foreach (var item in allowed
                    .Where(i => !i.AllRewardsClaimed)
                    .OrderBy(i =>
                    {
                        int idx = existingWatchingIds.IndexOf(i.Id);
                        if (idx >= 0) return idx;
                        return i.Campaign.IsGeneralDrop ? int.MaxValue : int.MaxValue - 1;
                    }))
                    _watchingItems.Add(item);

                // Completed: preserve completion timestamps across refreshes
                foreach (var item in allowed.Where(i => i.AllRewardsClaimed))
                {
                    item.CompletedAt = previousCompletedAt.TryGetValue(item.Id, out var ts) ? ts : DateTime.Now;
                    _completedItems.Add(item);
                }

                DropsInventoryManager.Instance.UpdateCampaigns(allCampaigns, _twitchGqlService, startWatching: false);
                DropsInventoryManager.Instance.SetUserCampaignOrder(_watchingItems.Select(i => i.Id));

                // Restore pinned state (miner persists a pin per platform across refreshes)
                foreach (string pinnedId in DropsInventoryManager.Instance.PinnedCampaignIds)
                {
                    var pinnedItem = _watchingItems.FirstOrDefault(i => i.Id == pinnedId);
                    if (pinnedItem != null) pinnedItem.IsPinned = true;
                }

                MinerStatus = "Idle";
                MinerStatusDetails = $"{_watchingItems.Count} watching, {_completedItems.Count} completed";
                SyncMinerStateToQueue();
                _ = RefreshStreamerLiveStatusAsync();
            }
            catch (OperationCanceledException ex) when (_currentLoadCts?.IsCancellationRequested == true)
            {
                // Expected when a new load cancels the old one
                AppLogger.Info("Dashboard", $"LoadDropsAsync canceled due to superseding refresh request. {ex.Message}");
                return;
            }
            catch (Exception ex)
            {
                MinerStatus = "Failed to load campaigns";
                MinerStatusDetails = ex.Message;
                AppLogger.Error("Dashboard", "LoadDropsAsync failed.", ex);
            }
            finally
            {
                // Resume BEFORE releasing the semaphore, so a second refresh already waiting on it can't
                // start its own PauseWatchingAsync() while this call's resume is still in flight.
                _currentLoadCts = null;
                await DropsInventoryManager.Instance.ResumeWatchingAsync();
                AppLogger.Info("Dashboard", "Watcher resumed after campaign refresh.");
                _loadDropsSemaphore.Release();
            }
        }
        /// <summary>
        /// Asynchronously validates the current Twitch credentials using the associated web view and service.
        /// </summary>
        /// <returns>A task that represents the asynchronous validation operation.</returns>
        private async Task ValidateTwitchCredentialsAsync()
        {
            await _twitchService.ValidateCredentialsAsync(_twitchWebView);
        }
        /// <summary>
        /// Validates the current Kick service credentials asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous validation operation.</returns>
        private async Task ValidateKickCredentialsAsync()
        {
            await _kickService.ValidateCredentialsAsync(_kickWebView);
        }
        /// <summary>
        /// Asynchronously validates the credentials for external services if they are not already connected.
        /// </summary>
        /// <returns>A task that represents the asynchronous validation operation.</returns>
        private async Task ValidateCredentialsAsync()
        {
            if (_twitchService.Status != ConnectionStatus.Connected)
                await ValidateTwitchCredentialsAsync();

            if (_kickService.Status != ConnectionStatus.Connected)
                await ValidateKickCredentialsAsync();
        }

        #region Event Handlers
        /// <summary>
        /// Performs asynchronous validation of Twitch and Kick services when the component is loaded.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task OnLoadedAsync()
        {
            if (!_isInitialized)
            {
                _twitchService.StatusChanged += OnTwitchStatusChanged;
                _kickService.StatusChanged += OnKickStatusChanged;

                _isInitialized = true;

                await ValidateCredentialsAsync();

                _initialValidationCompleted = true;
                DropsInventoryManager.Instance.InitializeWebViews(_twitchWebView, _kickWebView);

                // Load campaigns / drops
                await StartAutoRefreshDropsAsync();
            }
        }
        /// <summary>
        /// Handles changes to the Kick connection status and updates related UI elements accordingly.
        /// </summary>
        /// <remarks>This method updates the Kick connection status message, color indicator, and the
        /// enabled state of the Kick login button based on the provided status. It should be called whenever the
        /// connection status changes to ensure the UI reflects the current state.</remarks>
        /// <param name="status">The new connection status value indicating the current state of the Kick login process.</param>
        private void OnKickStatusChanged(ConnectionStatus status)
        {
            switch (status)
            {
                case ConnectionStatus.NotConnected:
                    KickConnectionStatus = "Not Connected";
                    KickConnectionColor = "Red";
                    KickLoginButton.IsEnabled = true;
                    break;

                case ConnectionStatus.Validating:
                    KickConnectionStatus = "Validating...";
                    KickConnectionColor = "Orange";
                    KickLoginButton.IsEnabled = false;
                    break;

                case ConnectionStatus.Connected:
                    KickConnectionStatus = "Connected";
                    KickConnectionColor = "Lime";
                    KickLoginButton.IsEnabled = false; // disable when already logged in
                    ScheduleDropsLoad();
                    break;
                case ConnectionStatus.Connecting:
                    KickConnectionStatus = "Connecting...";
                    KickConnectionColor = "Yellow";
                    KickLoginButton.IsEnabled = false;
                    break;
            }
        }
        /// <summary>
        /// Updates the Twitch connection status display and related UI elements based on the specified connection
        /// status.
        /// </summary>
        /// <param name="status">The current connection status of the Twitch login process. Determines how the UI reflects the connection
        /// state.</param>
        private void OnTwitchStatusChanged(ConnectionStatus status)
        {
            switch (status)
            {
                case ConnectionStatus.NotConnected:
                    TwitchConnectionStatus = "Not Connected";
                    TwitchConnectionColor = "Red";
                    TwitchLoginButton.IsEnabled = true;
                    break;

                case ConnectionStatus.Validating:
                    TwitchConnectionStatus = "Validating...";
                    TwitchConnectionColor = "Orange";
                    TwitchLoginButton.IsEnabled = false;
                    break;

                case ConnectionStatus.Connected:
                    TwitchConnectionStatus = "Connected";
                    TwitchConnectionColor = "Lime";
                    TwitchLoginButton.IsEnabled = false; // disable when already logged in
                    ScheduleDropsLoad();
                    break;
                case ConnectionStatus.Connecting:
                    TwitchConnectionStatus = "Connecting...";
                    TwitchConnectionColor = "Yellow";
                    TwitchLoginButton.IsEnabled = false;
                    break;
            }
        }
        /// <summary>
        /// Handles the Click event for the Kick login button, displaying the login dialog and saving the session token
        /// if authentication is successful.
        /// </summary>
        /// <param name="sender">The source of the event, typically the Kick login button.</param>
        /// <param name="e">The event data associated with the Click event.</param>
        private void OnKickLoginClick(object sender, RoutedEventArgs e)
        {
            new KickLoginWindow().ShowDialog();
            _ = ValidateKickCredentialsAsync();
        }
        /// <summary>
        /// Handles the Click event for the manual refresh button on the Miner Status card, forcing an immediate
        /// campaign reload instead of waiting for the hourly timer or a platform reconnect. This is the only
        /// recovery path in the UI if a load previously failed or reported "Need login" while both platforms
        /// still show Connected.
        /// </summary>
        private void OnRefreshNowClick(object sender, RoutedEventArgs e)
        {
            AppLogger.Info("Dashboard", "Manual refresh requested by user.");
            ScheduleDropsLoad();
        }
        /// <summary>
        /// Handles the Click event for the Twitch login button, displaying the Twitch login window and initiating
        /// Twitch account validation.
        /// </summary>
        /// <param name="sender">The source of the event, typically the button that was clicked.</param>
        /// <param name="e">The event data associated with the click event.</param>
        private void OnTwitchLoginClick(object sender, RoutedEventArgs e)
        {
            new TwitchLoginWindow().ShowDialog();
            _ = ValidateTwitchCredentialsAsync();
        }

        private static void UpdateWatchingStreamer(QueueCampaignItem item, string channel)
        {
            foreach (var s in item.StreamerStatuses)
            {
                bool isThis = !string.IsNullOrEmpty(channel) &&
                    string.Equals(s.Name, channel, StringComparison.OrdinalIgnoreCase);
                s.IsWatching = isThis;
                s.IsOnline = isThis; // watched = online; all others = offline
            }
        }

        private void SyncMinerStateToQueue()
        {
            // If campaign/channel events fired before the queue was populated (race at startup),
            // the streamer statuses stay null. Re-apply the miner's current state now.
            if (!string.IsNullOrEmpty(KickCampaignName))
            {
                foreach (var item in _watchingItems.Where(i => i.Platform == Platform.Kick))
                {
                    bool active = string.Equals(item.Name, KickCampaignName, StringComparison.OrdinalIgnoreCase);
                    item.IsActive = active;
                    if (!active) { item.StreamChannel = string.Empty; foreach (var s in item.StreamerStatuses) s.IsWatching = false; }
                }
                if (!string.IsNullOrEmpty(KickWatchedChannel))
                {
                    var kickActive = _watchingItems.FirstOrDefault(i => i.Platform == Platform.Kick && i.IsActive);
                    if (kickActive != null) { kickActive.StreamChannel = KickWatchedChannel; UpdateWatchingStreamer(kickActive, KickWatchedChannel); }
                }
            }
            if (!string.IsNullOrEmpty(TwitchCampaignName))
            {
                foreach (var item in _watchingItems.Where(i => i.Platform == Platform.Twitch))
                {
                    bool active = string.Equals(item.Name, TwitchCampaignName, StringComparison.OrdinalIgnoreCase);
                    item.IsActive = active;
                    if (!active) { item.StreamChannel = string.Empty; foreach (var s in item.StreamerStatuses) s.IsWatching = false; }
                }
                if (!string.IsNullOrEmpty(TwitchWatchedChannel))
                {
                    var twitchActive = _watchingItems.FirstOrDefault(i => i.Platform == Platform.Twitch && i.IsActive);
                    if (twitchActive != null) { twitchActive.StreamChannel = TwitchWatchedChannel; UpdateWatchingStreamer(twitchActive, TwitchWatchedChannel); }
                }
            }
        }

        private async Task RefreshStreamerLiveStatusAsync()
        {
            // Twitch: batch GQL query per campaign
            if (_twitchGqlService != null)
            {
                var twitchItems = _watchingItems
                    .Where(i => i.Platform == Platform.Twitch && i.HasSpecificStreamers)
                    .ToList();
                foreach (var item in twitchItems)
                {
                    try
                    {
                        var logins = item.StreamerStatuses.Select(s => s.Name).ToList();
                        if (logins.Count == 0) continue;
                        var liveLogins = await _twitchGqlService.QueryLiveChannelsBySlugAsync(logins, item.Campaign.Slug);
                        var liveSet = new HashSet<string>(liveLogins, StringComparer.OrdinalIgnoreCase);
                        await Dispatcher.InvokeAsync(() =>
                        {
                            foreach (var s in item.StreamerStatuses)
                                s.IsOnline = liveSet.Contains(s.Name);
                        });
                    }
                    catch (Exception ex)
                    {
                        // Reset to "unknown" (null) rather than silently keeping a possibly-stale true/false
                        // reading - otherwise a probe failure looks identical to a confirmed offline streamer.
                        AppLogger.Warn("Dashboard", $"Twitch live-status check failed for campaign '{item.Name}'; marking streamers unknown. {ex.Message}");
                        await Dispatcher.InvokeAsync(() =>
                        {
                            foreach (var s in item.StreamerStatuses)
                                s.IsOnline = null;
                        });
                    }
                }
            }

            // Kick: check each non-watching streamer via the public channel API
            var kickItems = _watchingItems
                .Where(i => i.Platform == Platform.Kick && i.HasSpecificStreamers)
                .ToList();
            if (kickItems.Count == 0) return;

            using var http = new System.Net.Http.HttpClient();
            http.Timeout = TimeSpan.FromSeconds(8);
            http.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/120.0 Safari/537.36");
            http.DefaultRequestHeaders.Add("Accept", "application/json");

            foreach (var item in kickItems)
            {
                foreach (var streamer in item.StreamerStatuses.Where(s => !s.IsWatching).ToList())
                {
                    try
                    {
                        var json = await http.GetStringAsync(
                            $"https://kick.com/api/v2/channels/{streamer.Name}");
                        using var doc = System.Text.Json.JsonDocument.Parse(json);
                        bool isLive = doc.RootElement.TryGetProperty("livestream", out var ls)
                                      && ls.ValueKind != System.Text.Json.JsonValueKind.Null;
                        await Dispatcher.InvokeAsync(() => streamer.IsOnline = isLive);
                    }
                    catch (Exception ex)
                    {
                        // Same rationale as the Twitch branch above - don't let a probe failure look like a
                        // confirmed offline reading.
                        AppLogger.Warn("Dashboard", $"Kick live-status check failed for streamer '{streamer.Name}'; marking unknown. {ex.Message}");
                        await Dispatcher.InvokeAsync(() => streamer.IsOnline = null);
                    }
                }
            }
        }

        private void OnToggleWatchingSection(object sender, RoutedEventArgs e) => IsWatchingExpanded = !IsWatchingExpanded;
        private void OnToggleCompletedSection(object sender, RoutedEventArgs e) => IsCompletedExpanded = !IsCompletedExpanded;

        private void OnActivateCampaignClick(object sender, RoutedEventArgs e)
        {
            if (((System.Windows.Controls.Button)sender).Tag is not QueueCampaignItem item) return;
            // Only clear pins on the same platform - Twitch and Kick each have their own independent pin.
            foreach (var qi in _watchingItems.Where(w => w.Platform == item.Platform)) qi.IsPinned = false;
            item.IsPinned = true;
            DropsInventoryManager.Instance.SwitchCampaignCommand.Execute(item.Campaign);
        }

        private void OnRemoveCampaignClick(object sender, RoutedEventArgs e)
        {
            if (((System.Windows.Controls.Button)sender).Tag is not QueueCampaignItem item) return;
            _watchingItems.Remove(item);
            DropsInventoryManager.Instance.SkipCampaign(item.Id);
            DropsInventoryManager.Instance.SetUserCampaignOrder(_watchingItems.Select(i => i.Id));
        }

        private void OnCardMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            if (sender is FrameworkElement fe && fe.DataContext is QueueCampaignItem item)
            {
                _dragItem = item;
                _dragCardElement = fe;
                _dragOrigin = e.GetPosition(null);
                _dragOffsetInCard = e.GetPosition(fe);
                _isDragging = false;
            }
        }

        private void OnQueueMouseMove(object sender, MouseEventArgs e)
        {
            if (_dragItem == null || e.LeftButton != MouseButtonState.Pressed)
            {
                ResetDrag();
                return;
            }

            Point pos = e.GetPosition(null);
            if (!_isDragging)
            {
                if (Math.Abs(pos.Y - _dragOrigin.Y) < DragThreshold) return;
                _isDragging = true;

                // Snapshot the card BEFORE fading it, then attach adorner
                if (sender is UIElement panel && _dragCardElement != null)
                {
                    _adornerLayer = AdornerLayer.GetAdornerLayer(panel);
                    if (_adornerLayer != null)
                    {
                        _dragAdorner = new DragCardAdorner(panel, _dragCardElement, _dragOffsetInCard);
                        _adornerLayer.Add(_dragAdorner);
                    }
                }
                _dragItem.IsDragging = true;
            }

            _dragAdorner?.UpdatePosition(e.GetPosition(sender as UIElement));

            if (sender is UIElement container)
                LiveReorder(container, e.GetPosition(container));
        }

        private void LiveReorder(UIElement panel, Point pos)
        {
            var hit = VisualTreeHelper.HitTest(panel, pos);
            if (hit == null) return;

            DependencyObject obj = hit.VisualHit;
            while (obj != null && !ReferenceEquals(obj, panel))
            {
                if (obj is FrameworkElement fe && fe.DataContext is QueueCampaignItem target && target != _dragItem)
                {
                    int from = _watchingItems.IndexOf(_dragItem!);
                    int to = _watchingItems.IndexOf(target);
                    if (from < 0 || to < 0 || from == to) break;
                    try
                    {
                        Point origin = fe.TransformToVisual(panel).Transform(new Point(0, 0));
                        double mid = origin.Y + fe.ActualHeight / 2;
                        if ((from < to && pos.Y > mid) || (from > to && pos.Y < mid))
                            _watchingItems.Move(from, to);
                    }
                    catch { /* visual tree still connecting after move */ }
                    break;
                }
                obj = VisualTreeHelper.GetParent(obj);
            }
        }

        private void OnQueueMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging && _dragItem != null)
                DropsInventoryManager.Instance.SetUserCampaignOrder(_watchingItems.Select(i => i.Id));
            ResetDrag();
        }

        private void ResetDrag()
        {
            if (_dragAdorner != null && _adornerLayer != null)
            {
                _adornerLayer.Remove(_dragAdorner);
                _dragAdorner = null;
            }
            _adornerLayer = null;
            if (_dragItem != null) _dragItem.IsDragging = false;
            _dragItem = null;
            _dragCardElement = null;
            _isDragging = false;
        }
        #endregion
    }

    internal class DragCardAdorner : Adorner
    {
        private readonly ImageBrush _snapshot;
        private readonly double _cardWidth;
        private readonly double _cardHeight;
        private readonly Point _mouseOffset;
        private Point _mousePos;

        public DragCardAdorner(UIElement adornedElement, FrameworkElement card, Point mouseOffsetInCard)
            : base(adornedElement)
        {
            IsHitTestVisible = false;
            _mouseOffset = mouseOffsetInCard;
            _cardWidth = card.ActualWidth;
            _cardHeight = card.ActualHeight;

            var rtb = new RenderTargetBitmap(
                Math.Max(1, (int)_cardWidth),
                Math.Max(1, (int)_cardHeight),
                96, 96, PixelFormats.Pbgra32);
            rtb.Render(card);
            rtb.Freeze();
            _snapshot = new ImageBrush(rtb) { Stretch = Stretch.None };
        }

        public void UpdatePosition(Point mousePos)
        {
            _mousePos = mousePos;
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            dc.PushOpacity(0.85);
            dc.DrawRectangle(
                _snapshot, null,
                new Rect(
                    _mousePos.X - _mouseOffset.X,
                    _mousePos.Y - _mouseOffset.Y,
                    _cardWidth, _cardHeight));
            dc.Pop();
        }
    }

    public class QueueCampaignItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public DropsCampaign Campaign { get; }
        public string Id => Campaign.Id;
        public string Name => Campaign.Name;
        public string GameName => Campaign.GameName;
        public string? GameImageUrl => Campaign.GameImageUrl;
        public Platform Platform => Campaign.Platform;
        public IReadOnlyList<DropsReward> Rewards => Campaign.Rewards;
        public bool AllRewardsClaimed => Campaign.AllRewardsClaimed;

        // True when this campaign has a fixed streamer list (not a "watch anyone" general drop)
        public bool HasSpecificStreamers => !Campaign.IsGeneralDrop && Campaign.ConnectUrls.Count > 0;
        public ObservableCollection<StreamerStatus> StreamerStatuses { get; } = new();

        // Max 3 shown, online first. All shown when list is ≤3 (small specific campaigns like Willjum+Sinks).
        public IReadOnlyList<StreamerStatus> DisplayedStreamers { get; private set; } = Array.Empty<StreamerStatus>();
        public int HiddenStreamerCount => Math.Max(0, StreamerStatuses.Count - DisplayedStreamers.Count);
        public bool HasHiddenStreamers => HiddenStreamerCount > 0;

        private void RefreshDisplayedStreamers()
        {
            DisplayedStreamers = StreamerStatuses.Count <= 3
                ? (IReadOnlyList<StreamerStatus>)StreamerStatuses
                : StreamerStatuses
                    .OrderByDescending(s => s.IsOnlineTrue ? 2 : s.IsOnlineFalse ? 1 : 0)
                    .Take(3)
                    .ToList();
            OnPropertyChanged(nameof(DisplayedStreamers));
            OnPropertyChanged(nameof(HiddenStreamerCount));
            OnPropertyChanged(nameof(HasHiddenStreamers));
        }

        private bool _isPinned;
        public bool IsPinned
        {
            get => _isPinned;
            set { if (_isPinned == value) return; _isPinned = value; OnPropertyChanged(); }
        }

        private bool _isDragging;
        public bool IsDragging
        {
            get => _isDragging;
            set { if (_isDragging == value) return; _isDragging = value; OnPropertyChanged(); }
        }

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive == value) return;
                _isActive = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsStreamOffline));
            }
        }

        private string _streamChannel = string.Empty;
        public string StreamChannel
        {
            get => _streamChannel;
            set
            {
                if (_streamChannel == value) return;
                _streamChannel = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsStreamOffline));
            }
        }

        // Only show the "Streamer offline" indicator for general drops — specific-streamer
        // campaigns show per-streamer status in StreamerStatuses instead.
        public bool IsStreamOffline => _isActive && string.IsNullOrEmpty(_streamChannel) && !HasSpecificStreamers;

        private bool _wasSkippedOffline;
        public bool WasSkippedOffline
        {
            get => _wasSkippedOffline;
            set { if (_wasSkippedOffline == value) return; _wasSkippedOffline = value; OnPropertyChanged(); }
        }

        public DateTime? CompletedAt { get; set; }
        public string CompletedAtText => CompletedAt.HasValue ? CompletedAt.Value.ToString("MMM d · HH:mm") : string.Empty;

        public string ExpiryText
        {
            get
            {
                int d = (int)Math.Max(0, (Campaign.EndsAt.LocalDateTime.Date - DateTime.Now.Date).TotalDays);
                return $"Ends {Campaign.EndsAt.LocalDateTime:MMM d} · {d}d left";
            }
        }
        public bool ExpiryIsWarning => (Campaign.EndsAt - DateTimeOffset.UtcNow).TotalDays is >= 3 and < 6;
        public bool ExpiryIsCritical => (Campaign.EndsAt - DateTimeOffset.UtcNow).TotalDays is >= 0 and < 3;

        public QueueCampaignItem(DropsCampaign campaign)
        {
            Campaign = campaign;
            if (!campaign.IsGeneralDrop)
            {
                foreach (var url in campaign.ConnectUrls)
                {
                    var name = GetUsernameFromUrl(url);
                    if (!string.IsNullOrEmpty(name))
                        StreamerStatuses.Add(new StreamerStatus(name));
                }
                foreach (var s in StreamerStatuses)
                    s.PropertyChanged += (_, _) => RefreshDisplayedStreamers();
            }
            RefreshDisplayedStreamers();
        }

        private static string GetUsernameFromUrl(string url)
        {
            try { return new Uri(url).AbsolutePath.Trim('/').ToLowerInvariant(); }
            catch { return string.Empty; }
        }
    }

    public class StreamerStatus : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        public string Name { get; }

        private bool? _isOnline;
        public bool? IsOnline
        {
            get => _isOnline;
            set
            {
                if (_isOnline == value) return;
                _isOnline = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsOnlineTrue));
                OnPropertyChanged(nameof(IsOnlineFalse));
            }
        }

        public bool IsOnlineTrue => _isOnline == true;
        public bool IsOnlineFalse => _isOnline == false;

        private bool _isWatching;
        public bool IsWatching
        {
            get => _isWatching;
            set { if (_isWatching == value) return; _isWatching = value; OnPropertyChanged(); }
        }

        public StreamerStatus(string name) { Name = name; }
    }
}