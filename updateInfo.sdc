{
  "version": "1.0.3",
  "type": "Feature",
  "changelog": "New: Watch Streak, a dedicated sidebar page that watches a queued list of specific channels and automatically claims the community-points bonus once each goes live, independent of any drop campaign - includes offline/checking status indicators and a poll-frequency setting. New: a proper Windows installer built with Inno Setup - a real Welcome / destination / install / finish wizard, Start Menu shortcut and optional Desktop shortcut, and a clean entry in Apps & Features with working uninstall; installs per-user so it never lands somewhere the app can't write its WebView2 data, and refuses to install or uninstall while the app is still running instead of silently leaving things half-done. New: a first-run 'Quick Setup' window shown once after installing - pick a theme, log in to Twitch/Kick right away with live Connected/Failed status, set mining priority and startup behavior, and choose notification and update-check preferences, all of it just a friendly first pass over settings you can still change anytime. New: optional notifications for when a reward has been claimed and (opt-in) for when the app starts actively farming a new stream, guarded so the latter only fires on an actual change of streamer rather than every recheck cycle. New: a 'Browser Extension (Optional)' card on the Help page linking to the companion Twitch extension.",
  "historic_versions": [
    {
      "version": "1.0.2",
      "type": "Feature",
      "changelog": "Full codebase review and hardening pass. Bug fixes: claimed drops could go out with stale auth headers due to a header-refresh await bug; pinning a campaign wasn't per-platform, so pinning Kick could get silently released by Twitch's own selection pass (and vice versa), and the Activate button un-pinned both platforms at once; switching campaigns while a refresh was in progress silently did nothing despite the UI showing it worked; timer callbacks (recheck/health-check/eligibility checks) had no exception handling and could crash the app or leave mining permanently stuck after a transient error; a stream going offline could get stuck unmonitored for up to an hour; one malformed Kick campaign could discard every valid campaign in that fetch; a Pause/Resume race could leave mining paused until the next hourly tick; several shared timers and counters were unsynchronized across concurrent code paths; a 'minutes before next reward' calculation was silently always zero; login windows showed a blank window with no explanation if the WebView2 runtime was missing; a malformed Kick login-page selector was fixed. Security: the self-updater now verifies a published SHA256 hash before extracting/running a downloaded update; hardened the browser-launch and startup-registry code against shell/argument injection (not previously exploitable, but hardened regardless). New: a manual 'Refresh Now' button on the Dashboard; the online/offline indicator now distinguishes 'confirmed offline' from 'couldn't verify' instead of treating a check failure as offline; a new app icon; removed the Discord and ko-fi links."
    },
    {
      "version": "1.0.1",
      "type": "Feature",
      "changelog": "Added a Priority Queue: pick or type exact game names in Settings and always start those drops first when available, with add/remove/reorder controls. Added a 'Minimize to tray' toggle so minimizing can go to the taskbar instead. Added diagnostic logging for Twitch campaign selection to help track down missing drops. Fixed several Settings UI bugs: dropdown selections not registering, broken rounded corners and click hit-testing on dropdowns, and misaligned Priority Queue row buttons."
    },
    {
      "version": "1.0.0",
      "type": "Release",
      "changelog": "Initial release."
    }
  ],
  "sha256": "670977b727f24a3f286cd2165f79475e674cf7fa11644ee13bbedc5567b1dd5b"
}
