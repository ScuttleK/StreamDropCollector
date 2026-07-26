using Core.Interfaces;
using Core.Logging;
using Core.Models;
using Core.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows.Threading;

namespace Core.Managers
{
    /// <summary>
    /// Watch Streak: queue one or more streamers, and once each goes live, watch it for a fixed
    /// duration and claim whatever community-points bonus becomes available. Deliberately kept
    /// separate from <see cref="DropsInventoryManager"/> -- that manager's entire selection loop is
    /// shaped around drop campaigns/rewards, and there's no natural slot in it for "watch this one
    /// specific channel for N minutes, independent of any campaign." This owns its own dedicated
    /// hidden WebView2 host so it never contends with the drops miner's own navigation.
    /// </summary>
    public sealed class WatchStreakManager
    {
        private static readonly Lazy<WatchStreakManager> _instance = new(() => new WatchStreakManager());
        public static WatchStreakManager Instance => _instance.Value;

        public ObservableCollection<WatchStreakEntry> Queue { get; } = new();

        private IWebViewHost? _webView;
        private IGqlService? _gqlService;

        private readonly DispatcherTimer _tickTimer = new() { Interval = TimeSpan.FromSeconds(1) };
        private bool _isTicking;

        private const int WatchDurationSeconds = 5 * 60;
        private int _pollIntervalSeconds = 30 * 60; // default: every 30 minutes
        public int PollIntervalSeconds => _pollIntervalSeconds;
        private DateTime _lastLivePollUtc = DateTime.MinValue;

        private static readonly string _queueCacheFilePath = Path.Combine(
            Environment.ExpandEnvironmentVariables("%APPDATA%"),
            "Stream Drop Collector",
            "WatchStreakQueue.json");

        private WatchStreakManager()
        {
            LoadQueueFromDisk();
            _tickTimer.Tick += async (s, e) => await TickAsync();
            _tickTimer.Start();
        }

        public void AddStreamer(string urlOrLogin)
        {
            string? login = ExtractChannelLogin(urlOrLogin);
            if (string.IsNullOrWhiteSpace(login))
                return;

            if (Queue.Any(q => string.Equals(q.ChannelLogin, login, StringComparison.OrdinalIgnoreCase)))
                return;

            Queue.Add(new WatchStreakEntry(login));
            SaveQueueToDisk();

            // Don't make a freshly-added streamer wait out whatever the poll interval happens to be
            // (up to 3 hours) for its first check -- get it checked on the next tick instead.
            RefreshNow();
        }

        public void RemoveStreamer(WatchStreakEntry entry)
        {
            Queue.Remove(entry);
            SaveQueueToDisk();
        }

        /// <summary>
        /// Changes how often queued streamers are checked for live status. Takes effect on the next
        /// scheduled check; call <see cref="RefreshNow"/> too if you want it to apply immediately.
        /// </summary>
        public void SetPollIntervalSeconds(int seconds)
        {
            if (seconds <= 0 || seconds == _pollIntervalSeconds)
                return;

            _pollIntervalSeconds = seconds;
            SaveQueueToDisk();
        }

        /// <summary>
        /// Forces an immediate live/offline re-check of every queued streamer on the next tick (within
        /// about a second), instead of waiting for the normal poll interval. If a streamer is currently
        /// being watched, that continues uninterrupted first -- the shared hidden host can only do one
        /// thing at a time.
        /// </summary>
        public void RefreshNow()
        {
            _lastLivePollUtc = DateTime.MinValue;
        }

        /// <summary>
        /// Registers the hidden WebView2 host this manager should watch streams in. HiddenWebViewHost is
        /// a WPF Window defined in the UI project, which Core can't reference directly -- so, exactly
        /// like <see cref="DropsInventoryManager.InitializeWebViews"/>, the UI layer owns the instance
        /// (see WatchStreakView.xaml.cs) and hands it in here. Safe to call more than once; only the
        /// first call takes effect.
        /// </summary>
        public void InitializeWebView(IWebViewHost webView)
        {
            if (_webView != null)
                return;

            _webView = webView;
            _gqlService = new TwitchGqlService(webView);
        }

        /// <summary>
        /// Whether Twitch is currently connected, for gating the UI's Watch Streak enable toggle (the
        /// feature is pointless without Twitch connected, since the bonus it watches for is Twitch-only).
        /// Reads DropsInventoryManager's live status - the same one Dashboard's own login validation
        /// maintains - instead of independently re-deriving connectivity (e.g. via a cookie check through
        /// this manager's own WebView2 host), which previously could disagree with Dashboard because that
        /// separate host might still be spinning up its own WebView2 environment even while a real Twitch
        /// session was already live and connected elsewhere.
        /// </summary>
        public bool IsTwitchAccountConnected => DropsInventoryManager.Instance.IsTwitchConnected;

        private static string? ExtractChannelLogin(string input)
        {
            input = input.Trim();
            if (string.IsNullOrEmpty(input))
                return null;

            if (Uri.TryCreate(input, UriKind.Absolute, out Uri? uri) && uri.Host.Contains("twitch.tv", StringComparison.OrdinalIgnoreCase))
            {
                string path = uri.AbsolutePath.Trim('/');
                string login = path.Split('/')[0];
                return string.IsNullOrEmpty(login) ? null : login.ToLowerInvariant();
            }

            return input.TrimStart('@').ToLowerInvariant();
        }

        private async Task TickAsync()
        {
            if (_isTicking || Queue.Count == 0 || _webView == null)
                return;

            _isTicking = true;
            try
            {
                WatchStreakEntry? watching = Queue.FirstOrDefault(q => q.Status == WatchStreakStatus.Watching);
                if (watching != null)
                {
                    // Let an already-in-progress watch finish naturally even if the feature gets turned off
                    // mid-watch, rather than abandoning a bonus that's this close to landing.
                    if (watching.SecondsRemaining > 0)
                    {
                        watching.SecondsRemaining--;
                    }
                    else
                    {
                        await FinishWatchingAsync(watching);
                    }
                    return;
                }

                // Master switch, off by default - don't start anything new (no live-status polling, no
                // watching) while disabled. Checked after the in-progress-watch branch above so toggling
                // off never cuts short a watch that already started.
                if (!UISettingsManager.Instance.WatchStreakEnabled)
                    return;

                if ((DateTime.UtcNow - _lastLivePollUtc).TotalSeconds < _pollIntervalSeconds)
                    return;
                _lastLivePollUtc = DateTime.UtcNow;

                await PollQueueAsync();
            }
            catch (Exception ex)
            {
                AppLogger.Warn("WatchStreak", $"Tick failed: {ex.Message}");
            }
            finally
            {
                _isTicking = false;
            }
        }

        private async Task PollQueueAsync()
        {
            foreach (WatchStreakEntry candidate in Queue.ToList())
            {
                if (candidate.Status == WatchStreakStatus.Watching)
                    continue;

                // Already completed today's Watch Streak -- skip the live check entirely (no network
                // call, no "Checking..." flicker) to save resources. Flips back to eligible on its own
                // once IsLastCompletedToday rolls over past local midnight. Also covers a manual
                // "Refresh Now" click, since that just forces this same poll to run sooner.
                if (candidate.IsLastCompletedToday)
                    continue;

                bool isLive;
                candidate.IsCheckingNow = true;
                try
                {
                    isLive = await _gqlService!.IsChannelLiveAsync(candidate.ChannelLogin);
                }
                catch (Exception ex)
                {
                    AppLogger.Warn("WatchStreak", $"Live check failed for {candidate.ChannelLogin}: {ex.Message}");
                    continue;
                }
                finally
                {
                    candidate.IsCheckingNow = false;
                }

                candidate.LastCheckedAtUtc = DateTimeOffset.UtcNow;

                bool wasLive = candidate.IsCurrentlyLive;
                candidate.IsCurrentlyLive = isLive;

                if (isLive && !wasLive)
                {
                    // Fresh offline -> online transition: a new stream session, eligible again
                    // even if today's date-based throttle hasn't rolled over yet.
                    candidate.CurrentSessionId = Guid.NewGuid().ToString();
                }

                if (!isLive)
                {
                    if (candidate.Status != WatchStreakStatus.Idle || !candidate.LastClaimedAtUtc.HasValue)
                        candidate.Status = WatchStreakStatus.WaitingForLive;
                    continue;
                }

                if (IsEligible(candidate))
                {
                    await StartWatchingAsync(candidate);
                    break; // one shared hidden host -- only watch one channel at a time
                }
            }

            SaveQueueToDisk();
        }

        private static bool IsEligible(WatchStreakEntry entry)
        {
            if (!entry.LastClaimedAtUtc.HasValue)
                return true;

            bool sameDay = entry.LastClaimedAtUtc.Value.UtcDateTime.Date == DateTime.UtcNow.Date;
            bool sameSession = entry.CurrentSessionId != null && entry.CurrentSessionId == entry.LastClaimedSessionId;

            return !(sameDay || sameSession);
        }

        private async Task StartWatchingAsync(WatchStreakEntry entry)
        {
            AppLogger.Info("WatchStreak", $"Starting watch for {entry.ChannelLogin}");

            entry.Status = WatchStreakStatus.Watching;
            entry.SecondsRemaining = WatchDurationSeconds;
            entry.LastError = null;
            entry.LastPointsEarned = null;

            try
            {
                await _webView!.EnsureInitializedAsync();
                await _webView.NavigateAsync($"https://www.twitch.tv/{entry.ChannelLogin}");

                // Opportunistically listen for a ChannelPointsContext response with an available
                // claim for the whole watch window. Twitch won't actually surface a claimable bonus
                // until its own watch-time requirement is met, so this mostly just sits waiting --
                // started here (not awaited) so the countdown keeps ticking independently.
                _ = ListenForClaimAsync(entry);
            }
            catch (Exception ex)
            {
                AppLogger.Warn("WatchStreak", $"Failed to start watching {entry.ChannelLogin}: {ex.Message}");
                entry.Status = WatchStreakStatus.Error;
                entry.LastError = ex.Message;
            }
        }

        private async Task ListenForClaimAsync(WatchStreakEntry entry)
        {
            try
            {
                string? body = await _webView!.CaptureGqlResponseBodyContainingAsync(
                    "ChannelPointsContext", WatchDurationSeconds * 1000 + 30000,
                    isMatch: candidateBody =>
                    {
                        (string? candidateChannelId, string? candidateClaimId) = ParseAvailableClaim(candidateBody);
                        return candidateChannelId != null && candidateClaimId != null;
                    });

                if (body == null)
                    return; // no bonus became available during the watch window

                (string? channelId, string? claimId) = ParseAvailableClaim(body);
                if (channelId == null || claimId == null)
                    return;

                int? pointsEarned = await _gqlService!.ClaimCommunityPointsAsync(channelId, claimId);
                if (pointsEarned.HasValue)
                {
                    RecordStreakCompletion(entry);
                    entry.LastPointsEarned = pointsEarned;
                    entry.LastClaimedAtUtc = DateTimeOffset.UtcNow;
                    entry.LastClaimedSessionId = entry.CurrentSessionId;
                    SaveQueueToDisk();
                }
            }
            catch (Exception ex)
            {
                AppLogger.Warn("WatchStreak", $"Claim listen failed for {entry.ChannelLogin}: {ex.Message}");
            }
        }

        private static (string? channelId, string? claimId) ParseAvailableClaim(string responseBody)
        {
            try
            {
                JsonNode? root = JsonNode.Parse(responseBody);
                IEnumerable<JsonNode?> operations = root is JsonArray array ? array : new[] { root };

                foreach (JsonNode? operation in operations)
                {
                    JsonNode? channel = operation?["data"]?["community"]?["channel"];
                    JsonNode? claim = channel?["self"]?["communityPoints"]?["availableClaim"];
                    if (claim == null)
                        continue;

                    string? claimId = claim["id"]?.GetValue<string>();
                    string? channelId = channel?["id"]?.GetValue<string>();
                    if (claimId != null && channelId != null)
                        return (channelId, claimId);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Warn("WatchStreak", $"Failed to parse ChannelPointsContext response: {ex.Message}");
            }

            return (null, null);
        }

        private async Task FinishWatchingAsync(WatchStreakEntry entry)
        {
            // Give ListenForClaimAsync (running with its own slightly-longer timeout) a moment to
            // land if a claim arrived right at the edge of the window.
            await Task.Delay(2000);

            // If no bonus claim landed this session, the watch still counts as completed -- record it
            // now, using the actual finish time (not the earlier watch-start time), so today's Watch
            // Streak is correctly marked done and isn't immediately re-watched on the next poll.
            if (entry.LastClaimedSessionId != entry.CurrentSessionId)
            {
                RecordStreakCompletion(entry);
                entry.LastPointsEarned = null;
                entry.LastClaimedAtUtc = DateTimeOffset.UtcNow;
            }

            entry.Status = WatchStreakStatus.Idle;
            SaveQueueToDisk();
        }

        /// <summary>
        /// Updates <see cref="WatchStreakEntry.StreakCount"/> for a watch that's about to be recorded as completed
        /// -- call this before overwriting <see cref="WatchStreakEntry.LastClaimedAtUtc"/>, since it needs to
        /// compare against the previous completion date. Mirrors Twitch's own day-based streak: completing on the
        /// calendar day right after the last completion continues the streak, anything else (first-ever
        /// completion, or a gap of a day or more) restarts it at 1.
        /// </summary>
        private static void RecordStreakCompletion(WatchStreakEntry entry)
        {
            DateTime? previousDate = entry.LastClaimedAtUtc?.ToLocalTime().Date;
            DateTime today = DateTime.Now.Date;
            entry.StreakCount = previousDate == today.AddDays(-1) ? entry.StreakCount + 1 : 1;
        }

        private void LoadQueueFromDisk()
        {
            try
            {
                if (!File.Exists(_queueCacheFilePath))
                    return;

                string json = File.ReadAllText(_queueCacheFilePath, Encoding.UTF8);
                WatchStreakCacheFile? loaded = JsonSerializer.Deserialize<WatchStreakCacheFile>(json);
                if (loaded == null)
                    return;

                if (loaded.PollIntervalSeconds > 0)
                    _pollIntervalSeconds = loaded.PollIntervalSeconds;

                foreach (WatchStreakCacheEntry cached in loaded.Entries)
                {
                    if (string.IsNullOrWhiteSpace(cached.ChannelLogin))
                        continue;

                    WatchStreakEntry entry = new(cached.ChannelLogin)
                    {
                        LastClaimedAtUtc = cached.LastClaimedAtUtc,
                        LastPointsEarned = cached.LastPointsEarned,
                        StreakCount = cached.StreakCount,
                        Status = cached.LastClaimedAtUtc.HasValue ? WatchStreakStatus.Idle : WatchStreakStatus.WaitingForLive,
                    };
                    Queue.Add(entry);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Warn("WatchStreak", $"Failed to load queue cache. {ex.Message}");
            }
        }

        private void SaveQueueToDisk()
        {
            try
            {
                WatchStreakCacheFile snapshot = new()
                {
                    PollIntervalSeconds = _pollIntervalSeconds,
                    Entries = Queue
                        .Select(q => new WatchStreakCacheEntry
                        {
                            ChannelLogin = q.ChannelLogin,
                            LastClaimedAtUtc = q.LastClaimedAtUtc,
                            LastPointsEarned = q.LastPointsEarned,
                            StreakCount = q.StreakCount,
                        })
                        .ToList(),
                };

                string? directory = Path.GetDirectoryName(_queueCacheFilePath);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                string json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_queueCacheFilePath, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                AppLogger.Warn("WatchStreak", $"Failed to save queue cache. {ex.Message}");
            }
        }

        private sealed class WatchStreakCacheFile
        {
            public int PollIntervalSeconds { get; set; } = 30 * 60;
            public List<WatchStreakCacheEntry> Entries { get; set; } = new();
        }

        private sealed class WatchStreakCacheEntry
        {
            public string ChannelLogin { get; set; } = string.Empty;
            public DateTimeOffset? LastClaimedAtUtc { get; set; }
            public int? LastPointsEarned { get; set; }
            public int StreakCount { get; set; }
        }
    }
}
