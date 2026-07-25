# StreamDropCollector

**A fully automated, open-source drops miner for Twitch.tv and Kick.com**  
Watch streams in the background, earn campaign rewards, and claim them automatically - all without lifting a finger.

This is a personalized, actively-maintained fork of [tsgsOFFICIAL/StreamDropCollector](https://github.com/tsgsOFFICIAL/StreamDropCollector) - see [Acknowledgments](#acknowledgments) below.

[![.NET](https://img.shields.io/badge/.NET-10.0-blueviolet)](https://dotnet.microsoft.com/download)
[![WPF](https://img.shields.io/badge/WPF-Modern_UI-teal)](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/overview)
[![License](https://img.shields.io/badge/license-MIT-blue)](https://github.com/ScuttleK/StreamDropCollector/blob/master/LICENSE)

## Features

- **Dual-platform support** - mines drops on Twitch and Kick simultaneously
- **Watch Streak** - separately watches a queued list of specific channels and auto-claims the community-points bonus once each goes live, independent of any drop campaign
- **Smart queue** - drag-and-drop campaign ordering, auto-advances to the next on completion
- **Auto-claiming** - detects when a drop hits 100% and claims it immediately
- **Mining Priority** - four selectable modes (Ending Soonest, Live Now First, Highest Completion of Drop, Least Time To Next Reward), each with a tooltip explaining exactly how it decides
- **Priority Queue** - pin exact games above the normal priority sort, with add/remove/reorder controls
- **Game filtering** - per-platform whitelist or blacklist, remembers every game you've ever seen a campaign for
- **Priority retry** - if a campaign's streamers are all offline, skips to the next and rechecks the priority one every hour
- **Lowest quality mode** - sets streams to minimum quality for minimal resource usage
- **Mature content bypass** - handles age gates automatically
- **Live progress tracking** - real-time percentage, watched channel, and campaign end-date urgency display
- **Notifications** - optional alerts for a claimed reward, a drop that's ready to claim, and (opt-in) whenever the app starts actively farming a new stream
- **First-run Quick Setup** - a short guided setup shown once after installing: pick a theme, log in to Twitch/Kick, and set your preferences
- **Self-updating** - checks for new versions on your schedule and installs them with SHA256 verification before anything runs
- **Clean modern UI** - WPF dark/light/system theme, rounded cards, per-platform color coding, drag-to-reorder queue, tray support

## Screenshots

![Dashboard](https://github.com/ScuttleK/StreamDropCollector/blob/master/Github-Assets/Dashboard.png?raw=true)  
_Main dashboard showing live progress for both platforms_

---

![Inventory](https://github.com/ScuttleK/StreamDropCollector/blob/master/Github-Assets/Inventory.png?raw=true)  
_Active campaigns and rewards overview_

---

![Watch Streak](https://github.com/ScuttleK/StreamDropCollector/blob/master/Github-Assets/WatchStreak.png?raw=true)  
_Queue specific channels to auto-claim their Watch Streak bonus once live_

---

## Requirements

- Windows 10/11 (64-bit)
- A Twitch and/or Kick account

## Quick Start

The easiest way to get started is the installer:

1. Download [the latest installer](https://github.com/ScuttleK/StreamDropCollector/releases/latest/download/StreamDropCollector-latest-setup.exe)
2. Run it and follow the setup wizard
3. Log in to Twitch and/or Kick when prompted during first-run Quick Setup
4. Enjoy the free drops!

Prefer a portable copy instead? Grab the [self-contained](https://github.com/ScuttleK/StreamDropCollector/releases/latest/download/StreamDropCollector-latest-self-contained.zip) (no .NET install needed) or [framework-dependent](https://github.com/ScuttleK/StreamDropCollector/releases/latest/download/StreamDropCollector-latest-framework-dependent.zip) (requires the .NET 10 runtime) zip from the same [Releases](https://github.com/ScuttleK/StreamDropCollector/releases) page and run `Stream Drop Collector.exe` directly - no installation step.

## Building from Source

```bash
git clone https://github.com/ScuttleK/StreamDropCollector.git
cd StreamDropCollector
dotnet restore
dotnet publish UI/UI.csproj -c Release -r win-x64 -p:SelfContained=true
```

Executable will be in `UI\bin\Release\net10.0-windows10.0.17763.0\publish\win-x64\`

## Privacy & Safety

- No external APIs or third-party services are used for authentication
- All logins happen inside secure embedded WebView2 browsers (same as Edge/Chrome)
- Your credentials **never** leave your machine, outside the WebView2 engine
- No data is sent anywhere except directly to Twitch.tv, Kick.com, and GitHub (for update checks)

## Important Notes

- This tool is for personal use only
- Respect Twitch and Kick's Terms of Service
- Use at your own risk - automated viewing may violate platform rules in some contexts
- Not affiliated with Twitch or Kick

## Acknowledgments

Stream Drop Collector is a modified, personalized version of [tsgsOFFICIAL/StreamDropCollector](https://github.com/tsgsOFFICIAL/StreamDropCollector) - a huge thank you to **tsgsOFFICIAL** for building and open-sourcing the original project this one is based on. Everything here - the dual-platform mining engine, the reverse-engineered Twitch/Kick integration, the core drops-tracking logic - started there.

This fork has since diverged with its own branding, installer, first-run setup flow, Watch Streak feature, notification system, and ongoing maintenance, but the foundation this was built on deserves full credit.

## License

[MIT License](LICENSE) - see the file for details.
