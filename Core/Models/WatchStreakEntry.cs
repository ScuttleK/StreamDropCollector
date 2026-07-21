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
                OnPropertyChanged(nameof(LastCompletedText));
                OnPropertyChanged(nameof(IsLastCompletedToday));
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
                OnPropertyChanged(nameof(LastCompletedText));
            }
        }

        // Runtime-only, see IsCurrentlyLive's comment above.
        public string? CurrentSessionId { get; set; }
        public string? LastClaimedSessionId { get; set; }

        public string? LastError { get; set; }

        /// <summary>
        /// True once we've checked at least once and confirmed the channel is not live (and it isn't
        /// currently being watched/erroring) -- drives the red "Offline" indicator in the UI.
        /// </summary>
        public bool IsConfirmedOffline =>
            LastCheckedAtUtc.HasValue && !IsCurrentlyLive && Status != WatchStreakStatus.Watching && Status != WatchStreakStatus.Error && !IsCheckingNow;

        /// <summary>
        /// True when the channel is confirmed live but isn't (or is no longer) being actively watched --
        /// i.e. it already completed its Watch Streak for today/this session, so re-watching is skipped.
        /// Drives the green "Online" indicator in the UI, parallel to <see cref="IsConfirmedOffline"/>.
        /// </summary>
        public bool IsConfirmedOnline =>
            LastCheckedAtUtc.HasValue && IsCurrentlyLive && Status != WatchStreakStatus.Watching && Status != WatchStreakStatus.Error && !IsCheckingNow;

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
        /// now", which have their own dedicated colored indicators instead of sharing this text.
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
                if (!LastCheckedAtUtc.HasValue)
                    return "Waiting for first status check...";

                // Both the online and offline confirmed states are shown via their own dedicated
                // indicators (IsConfirmedOnline/IsConfirmedOffline) instead of this generic line.
                return "";
            }
        }

        public bool HasCompletedBefore => LastClaimedAtUtc.HasValue;

        public string LastCompletedText
        {
            get
            {
                if (!LastClaimedAtUtc.HasValue)
                    return "";

                string when = LastClaimedAtUtc.Value.ToLocalTime().ToString("g");
                return LastPointsEarned.HasValue
                    ? $"Streamer Watch Streak Completed - {when} (+{LastPointsEarned} pts)"
                    : $"Streamer Watch Streak Completed - {when} (no bonus that time)";
            }
        }

        public bool IsLastCompletedToday =>
            LastClaimedAtUtc.HasValue && LastClaimedAtUtc.Value.ToLocalTime().Date == DateTime.Now.Date;

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
