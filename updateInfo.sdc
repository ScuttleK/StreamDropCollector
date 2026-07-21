{
  "version": "1.0.3",
  "type": "Feature",
  "changelog": [
    "Added Watch Streak: a dedicated sidebar page that watches a queued list of specific channels and automatically claims the community-points bonus once each goes live, independent of any drop campaign",
    "Added a proper Windows installer (built with Inno Setup) with a real Welcome / destination / install / finish wizard, Start Menu and optional Desktop shortcuts, and a clean Apps & Features uninstall entry; installs per-user and refuses to install or uninstall while the app is still running",
    "Added a first-run \"Quick Setup\" window shown once after installing — pick a theme, log in to Twitch/Kick with live Connected/Failed status, set mining priority and startup behavior, and choose notification and update-check preferences",
    "Added optional notifications for when a reward has been claimed and for when the app starts actively farming a new stream",
    "Added a \"Browser Extension (Optional)\" card to the Help page"
  ],
  "historic_versions": [
    {
      "version": "1.0.2",
      "type": "Feature",
      "changelog": [
        "Redesigned Mining Priority into four clearly distinct modes (Ending Soonest, Live Now First, Highest Completion of Drop, Least Time To Next Reward) with an info tooltip explaining each one",
        "Fixed a bug where claimed drops could be submitted with stale auth headers due to an incorrect async/await pattern in the claim request",
        "Fixed campaign pinning not being tracked per platform — pinning a Kick campaign could be silently released by Twitch's own selection logic running right after, and vice versa",
        "Fixed several race conditions and unhandled-exception paths in the mining loop that could crash the app or leave it silently stuck after a transient error, with a safety net to auto-recover instead",
        "Fixed a Kick parsing bug where one malformed campaign discarded every valid campaign in that fetch",
        "Fixed the self-updater to download the correct self-contained build (previously pulled a framework-dependent build that could fail without .NET installed) and added SHA256 verification before running an update",
        "Game filtering now remembers every game you've ever seen a campaign for, instead of the list shrinking back down on every relaunch",
        "Added a manual \"Refresh Now\" button on the Dashboard and a \"Check for Updates Now\" button in Settings",
        "The streamer online/offline indicator now distinguishes \"confirmed offline\" from \"couldn't verify\" instead of treating a failed check as offline",
        "New app icon; removed the Discord and ko-fi links"
      ]
    },
    {
      "version": "1.0.1",
      "type": "Feature",
      "changelog": [
        "Added a Priority Queue: pick or type exact game names to always start those drops first when available, ahead of the normal mining-priority sort, with add/remove/reorder controls",
        "Added a \"Minimize to tray\" toggle in Settings — choose whether minimizing hides to the tray or behaves like a normal taskbar minimize",
        "Added diagnostic logging of raw Twitch campaign candidates (id, name, game, status, account-connection state) to help track down drops that don't show up as expected",
        "Fixed the self-updater and in-app GitHub links pointing at the original upstream repository instead of this fork",
        "Changed the default \"check for updates\" frequency to check on every launch"
      ]
    },
    {
      "version": "1.0.0",
      "type": "Release",
      "changelog": [
        "Initial release."
      ]
    }
  ],
  "sha256": "670977b727f24a3f286cd2165f79475e674cf7fa11644ee13bbedc5567b1dd5b"
}
