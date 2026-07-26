using System.ComponentModel;

namespace Core.Models
{
    public enum WatchStreakStatus
    {
        WaitingForLive,
        Watching,
        Idle,
        Error
    }

    /// <summary>
    /// One queued streamer for the Watch Streak feature: waits for the channel to go live, watches it
    /// for a fixed duration, and claims whatever community-points bonus becomes available.
    /// </summary>
    public sealed class WatchStreakEntry : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public string ChannelLogin { get; }

        private WatchStreakStatus _status = WatchStreakStatus.WaitingForLive;
        public WatchStreakStatus Status
        {
            get => _status;
            set
            {
                if (_status == value) return;
                _status = value;
                RaiseStatusDependentChanges();
            }
        }

        private int _secondsRemaining;
        public int SecondsRemaining
        {
            get => _secondsRemaining;
            set
            {
                if (_secondsRemaining == value) return;
                _secondsRemaining = value;
                OnPropertyChanged(nameof(SecondsRemaining));
                OnPropertyChanged(nameof(PrimaryStatusText));
            }
        }

        private bool _isCurrentlyLive;
        // Runtime-only (not persisted): also used to detect an offline->online transition so the same
        // continuous stream isn't re-watched twice. Reset on every app restart -- worst case after a
        // restart is one possibly-redundant watch cycle, not worth persisting for.
        public bool IsCurrentlyLive
        {
            get => _isCurrentlyLive;
            set
            {
                if (_isCurrentlyLive == value) return;
                _isCurrentlyLive = value;
                RaiseStatusDependentChanges();
            }
        }

        private DateTimeOffset? _lastCheckedAtUtc;
        public DateTimeOffset? LastCheckedAtUtc
        {
            get => _lastCheckedAtUtc;
            set
            {
                _lastCheckedAtUtc = value;
                RaiseStatusDependentChanges();
            }
        }

        // Persisted across restarts -- the once-a-day throttle needs to survive the app closing.
        private DateTimeOffset? _lastClaimedAtUtc;
        public DateTimeOffset? LastClaimedAtUtc
        {
            get => _lastClaimedAtUtc;
            set
            {
                _lastClaimedAtUtc = value;
                OnPropertyChanged(nameof(LastClaimedAtUtc));
                OnPropertyChanged(nameof(HasCompletedBefore));
                OnPropertyChanged(nameof(LastCompletedHeadlineText));
                OnPropertyChanged(nameof(LastCompletedDetailText));
                OnPropertyChanged(nameof(IsLastCompletedToday));
                OnPropertyChanged(nameof(CompletedTodayStatusText));
                OnPropertyChanged(nameof(IsConfirmedOnline));
                OnPropertyChanged(nameof(IsConfirmedOffline));
                OnPropertyChanged(nameof(PrimaryStatusText));
            }
        }

        private int? _lastPointsEarned;
        public int? LastPointsEarned
        {
            get => _lastPointsEarned;
            set
            {
                _lastPointsEarned = value;
                OnPropertyChanged(nameof(LastPointsEarned));
                OnPropertyChanged(nameof(LastCompletedDetailText));
            }
        }

        private int _streakCount;
        // Consecutive-day count, mirroring Twitch's own "Watch Streak: N" -- see
        // WatchStreakManager.RecordStreakCompletion for how it's advanced/reset.
        public int StreakCount
        {
            get => _streakCount;
            set
            {
                if (_streakCount == value) return;
                _streakCount = value;
                OnPropertyChanged(nameof(StreakCount));
                OnPropertyChanged(nameof(StreakCountText));
            }
        }

        public string StreakCountText => StreakCount > 0 ? $"Watch Streak: {StreakCount}" : "";

        // Runtime-only, see IsCurrentlyLive's comment above.
        public string? CurrentSessionId { get; set; }
        public string? LastClaimedSessionId { get; set; }

        public string? LastError { get; set; }

        /// <summary>
        /// True once we've checked at least once and confirmed the channel is not live (and it isn't
        /// currently being watched/erroring) -- drives the red "Offline" indicator in the UI. Once
        /// today's Watch Streak is already completed the dedicated green "Completed" indicator takes
        /// over instead (see <see cref="IsLastCompletedToday"/>), since we stop checking live status.
        /// </summary>
        public bool IsConfirmedOffline =>
            LastCheckedAtUtc.HasValue && !IsCurrentlyLive && Status != WatchStreakStatus.Watching && Status != WatchStreakStatus.Error && !IsCheckingNow && !IsLastCompletedToday;

        /// <summary>
        /// True when the channel is confirmed live but isn't (or is no longer) being actively watched --
        /// i.e. it already completed its Watch Streak for this session (but not yet "today", e.g. a
        /// stream restart within the same still-live session). Drives the green "Online" indicator in
        /// the UI, parallel to <see cref="IsConfirmedOffline"/>.
        /// </summary>
        public bool IsConfirmedOnline =>
            LastCheckedAtUtc.HasValue && IsCurrentlyLive && Status != WatchStreakStatus.Watching && Status != WatchStreakStatus.Error && !IsCheckingNow && !IsLastCompletedToday;

        public string OfflineLastCheckedText =>
            LastCheckedAtUtc.HasValue ? $"Last checked: {LastCheckedAtUtc.Value.ToLocalTime():g}" : "";

        public string OnlineLastCheckedText =>
            LastCheckedAtUtc.HasValue ? $"Last checked: {LastCheckedAtUtc.Value.ToLocalTime():g}" : "";

        private bool _isCheckingNow;
        /// <summary>
        /// True while a live/offline check for this specific streamer is actually in flight -- drives
        /// the yellow "Checking..." indicator. Adding a streamer triggers an immediate check rather than
        /// waiting for the next scheduled poll, so this should normally only be visible briefly.
        /// </summary>
        public bool IsCheckingNow
        {
            get => _isCheckingNow;
            set
            {
                if (_isCheckingNow == value) return;
                _isCheckingNow = value;
                RaiseStatusDependentChanges();
            }
        }

        /// <summary>
        /// The main status line for everything except "confirmed offline"/"confirmed online"/"checking
        /// now"/"completed today", which have their own dedicated colored indicators instead of sharing
        /// this text.
        /// </summary>
        public string PrimaryStatusText
        {
            get
            {
                if (Status == WatchStreakStatus.Watching)
                    return $"Watching - {SecondsRemaining / 60}:{SecondsRemaining % 60:00} remaining";
                if (Status == WatchStreakStatus.Error)
                    return LastError ?? "Error";
                if (IsCheckingNow)
                    return ""; // shown via the dedicated "Checking..." indicator instead
                if (IsLastCompletedToday)
                    return ""; // shown via the dedicated "Completed" indicator instead -- live checks
                               // are skipped entirely today, so LastCheckedAtUtc never gets set
                if (!LastCheckedAtUtc.HasValue)
                    return "Waiting for first status check...";

                // Both the online and offline confirmed states are shown via their own dedicated
                // indicators (IsConfirmedOnline/IsConfirmedOffline) instead of this generic line.
                return "";
            }
        }

        public bool HasCompletedBefore => LastClaimedAtUtc.HasValue;

        /// <summary>
        /// Bold headline for the "last completed" line -- distinct wording for "completed today" (the
        /// common case, since checks stop for the rest of the day once this is true) versus an older,
        /// historical completion still on display before the next successful check refreshes it.
        /// </summary>
        public string LastCompletedHeadlineText
        {
            get
            {
                if (!LastClaimedAtUtc.HasValue)
                    return "";

                return IsLastCompletedToday
                    ? "Watch Streak has been completed today"
                    : "Streamer Watch Streak Completed";
            }
        }

        /// <summary>
        /// Small muted detail line under <see cref="LastCompletedHeadlineText"/> -- the actual moment
        /// the watch finished (not when it started watching) plus whether a bonus was earned.
        /// </summary>
        public string LastCompletedDetailText
        {
            get
            {
                if (!LastClaimedAtUtc.HasValue)
                    return "";

                string when = LastClaimedAtUtc.Value.ToLocalTime().ToString("g");
                return LastPointsEarned.HasValue
                    ? $"{when} (+{LastPointsEarned} pts)"
                    : $"{when} (no bonus that time)";
            }
        }

        public bool IsLastCompletedToday =>
            LastClaimedAtUtc.HasValue && LastClaimedAtUtc.Value.ToLocalTime().Date == DateTime.Now.Date;

        /// <summary>
        /// Drives the muted text next to the green "Completed" indicator that replaces the normal
        /// Online/Offline "Last checked" line once today's Watch Streak is done.
        /// </summary>
        public string CompletedTodayStatusText =>
            IsLastCompletedToday && LastClaimedAtUtc.HasValue
                ? $"Watch Streak Completed today at {LastClaimedAtUtc.Value.ToLocalTime():g}"
                : "";

        public WatchStreakEntry(string channelLogin)
        {
            ChannelLogin = channelLogin;
        }

        private void RaiseStatusDependentChanges()
        {
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(IsCurrentlyLive));
            OnPropertyChanged(nameof(LastCheckedAtUtc));
            OnPropertyChanged(nameof(IsCheckingNow));
            OnPropertyChanged(nameof(IsConfirmedOffline));
            OnPropertyChanged(nameof(IsConfirmedOnline));
            OnPropertyChanged(nameof(OfflineLastCheckedText));
            OnPropertyChanged(nameof(OnlineLastCheckedText));
            OnPropertyChanged(nameof(PrimaryStatusText));
        }

        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
