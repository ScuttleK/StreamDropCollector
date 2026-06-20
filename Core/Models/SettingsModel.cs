using Core.Enums;

namespace Core.Models
{
    internal class SettingsModel
    {
        public bool StartWithWindows { get; set; }
        public bool MinimizeToTrayOnStartup { get; set; }
        public bool RunInBackground { get; set; }
        public string? Theme { get; set; }
        public UpdateFrequency UpdateFrequency { get; set; }
        public bool AutoClaimRewards { get; set; }
        public bool NotifyOnDropUnlocked { get; set; }
        public bool NotifyOnReadyToClaim { get; set; }
        public bool NotifyOnAutoClaimed { get; set; }
        public bool VerboseDebugLogging { get; set; }
        public bool UpdateAvailable { get; set; }
        public bool NotifyOnNewUpdateAvailable { get; set; }
        public DateTime? LastUpdateCheck { get; set; }
        public MiningPriorityMode MiningPriorityMode { get; set; } = MiningPriorityMode.AvailabilityThenProgress;
        public List<string> TwitchGameWhitelistSlugs { get; set; } = new List<string>();
        public List<string> KickGameWhitelistSlugs { get; set; } = new List<string>();
        public bool TwitchGameFilterBlacklistMode { get; set; }
        public bool KickGameFilterBlacklistMode { get; set; }
    }
}