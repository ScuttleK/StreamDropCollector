# StreamDropCollector

**A fully automated, open-source drops miner for Twitch.tv and Kick.com**  
Watch streams in the background, earn campaign rewards, and claim them automatically - all without lifting a finger.

[![.NET](https://img.shields.io/badge/.NET-10.0-blueviolet)](https://dotnet.microsoft.com/download)
[![WPF](https://img.shields.io/badge/WPF-Modern_UI-teal)](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/overview)
[![GitHub license](https://img.shields.io/github/license/Scuttle-ZapAccess/StreamDropCollector)](https://github.com/Scuttle-ZapAccess/StreamDropCollector/blob/master/LICENSE)

## Features

- Dual-platform support: Mines drops on **Twitch** and **Kick** simultaneously
- Smart queue: Drag-and-drop campaign ordering, auto-advances to next on completion
- Auto-claiming: Detects when a drop hits 100% and claims it immediately
- Priority retry: If a campaign's streamers are all offline, skips to the next and rechecks the priority one every hour
- Lowest quality mode: Sets streams to minimum quality for minimal resource usage
- Mature content bypass: Handles age gates automatically
- Live progress tracking: Real-time percentage, watched channel, and campaign end-date urgency display
- Clean modern UI: WPF dark theme with rounded cards, per-platform color coding, drag-to-reorder queue

## Screenshots

![Dashboard](https://github.com/Scuttle-ZapAccess/StreamDropCollector/blob/master/Github-Assets/Dashboard.png?raw=true)  
_Main dashboard showing live progress for both platforms_

---

![Inventory](https://github.com/Scuttle-ZapAccess/StreamDropCollector/blob/master/Github-Assets/Inventory.png?raw=true)  
_Active campaigns and rewards overview_

---

## Requirements

- Windows 10/11 (64-bit)
- A Twitch and/or Kick account

## Quick Start

1. Clone or download this repository
2. Build from source (see below)
3. Run `Stream Drop Collector.exe`
4. Log in to Twitch and Kick when the embedded browsers appear
5. Enjoy the free drops!

## Building from Source

```bash
git clone https://github.com/Scuttle-ZapAccess/StreamDropCollector.git
cd StreamDropCollector
dotnet restore
dotnet publish -c Release -r win-x64 -p:SelfContained=true
```

Executable will be in `UI\bin\Release\net10.0-windows10.0.17763.0\publish\win-x64\`

## Privacy & Safety

- No external APIs or third-party services are used for authentication
- All logins happen inside secure embedded WebView2 browsers (same as Edge/Chrome)
- Your credentials **never** leave your machine, outside the WebView2 engine
- No data is sent anywhere except directly to Twitch.tv and Kick.com

## Important Notes

- This tool is for personal use only
- Respect Twitch and Kick's Terms of Service
- Use at your own risk — automated viewing may violate platform rules in some contexts
- Not affiliated with Twitch or Kick

## License

[MIT License](LICENSE) - see the file for details.
