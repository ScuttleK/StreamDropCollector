# CLAUDE.md — StreamDropCollector UI Redesign Brief

> Context handoff for working on this repo with Claude Code. Place at repo root.
> This file describes a **UI redesign task**. Do not change app logic in `Core/`
> unless explicitly asked — visual work is XAML-only.

## What this project is
- Fork/clone of `tsgsOFFICIAL/StreamDropCollector` — an open-source Twitch + Kick
  drops miner. Watches streams in an embedded WebView2 and auto-claims rewards.
- Stack: **C# / WPF / .NET 10** (`net10.0-windows10.0.17763.0`), Windows-only.
- Two projects: `Core` (logic, services, view models) and `UI` (WPF front end).
- Builds clean with the .NET 10 SDK:
  ```
  dotnet restore
  dotnet build -c Debug
  ```
  One harmless warning exists (`CS0067` on `RelayCommand<T>.CanExecuteChanged`) — ignore.

## The goal
**Full modern redesign of the UI — "sleek dark / minimal" visual direction.**
Refined near-black surfaces, restrained single accent, soft rounded cards,
generous spacing, quiet typography. Not a re-theme of one screen — every view
should feel coherent.

## Existing UI architecture (work WITH this, don't bolt on)
```
UI/
  App.xaml                     # merges theme dictionaries
  MainWindow.xaml              # shell / navigation host
  Themes/
    Colors.xaml                # palette (brush/color resources)
    Dark.xaml                  # dark theme styles
    Light.xaml                 # light theme styles  (app has a light/dark switch)
  Views/
    DashboardView.xaml         # main: live progress cards for Twitch + Kick
    InventoryView.xaml         # active campaigns / rewards
    SettingsView.xaml          # whitelist, toggles, options
    HelpView.xaml
    TwitchLoginWindow.xaml     # WebView2 login
    KickLoginWindow.xaml       # WebView2 login
    HiddenWebViewHost.xaml     # background WebView host (no visible UI)
```
**Approach:** redefine the palette and shared control styles inside
`Themes/Colors.xaml` + `Dark.xaml` so views that consume named brushes via
`DynamicResource` re-skin globally. Then refine each view's layout/spacing on top.
Before editing, check whether the Views consume theme brushes or hardcode colors —
fix hardcoded colors to reference theme resources as you go. Keep `Light.xaml`
working (don't break the theme switch); mirror token names across both.

## Design tokens (sleek dark / minimal)
Fold these into `Colors.xaml`. Keep existing brush KEY NAMES where they already
exist — just change the values — so bindings keep resolving. ARGB hex.

| Role               | Hex          |
|--------------------|--------------|
| Background (base)  | `#FF0D0E11`  |
| Surface (cards)    | `#FF15171C`  |
| Surface hover      | `#FF1C1F26`  |
| Surface muted/track| `#FF20242C`  |
| Stroke / border    | `#FF2A2F38`  |
| Text primary       | `#FFECEEF1`  |
| Text secondary     | `#FFA0A6B0`  |
| Text muted         | `#FF6C7480`  |
| Accent             | `#FF7B6CF6`  |
| Accent hover       | `#FF8C7FFF`  |
| Accent pressed     | `#FF6A5BE0`  |
| On-accent (text)   | `#FFFFFFFF`  |
| Success            | `#FF3FB454`  |
| Danger             | `#FFE5534B`  |
| Twitch (status dot)| `#FF9146FF`  |
| Kick (status dot)  | `#FF53FC18`  |

Style language:
- Cards: `CornerRadius=14`, 1px stroke, `Padding=20`, surface bg.
- Buttons: `CornerRadius=10`, primary = accent fill, ghost = transparent + stroke;
  hover/pressed/disabled triggers.
- ProgressBar: slim (8px), rounded (4px), accent fill on muted track — heavily used
  on the dashboard, make it look good.
- Inputs: bg = base color, 1px stroke, focus = accent border, accent caret.
- Scrollbars: slim (10px), rounded thumb in surface-muted, no arrows.
- Font: `Segoe UI Variable Display, Segoe UI, Inter, sans-serif`. Sizes ~
  Display 26/Bold, Title 16/SemiBold, Body 13, Caption 11.
- Use Twitch/Kick colors ONLY as small status dots/accents, not full theming
  (direction is neutral-minimal, not branded).

A reference ResourceDictionary with all of the above as concrete WPF styles
(`ModernDark.xaml`) was produced separately — reuse its style definitions, but
merge them into the existing theme files rather than keeping a separate dictionary.

## Suggested order of work
1. `Colors.xaml` + `Dark.xaml` — palette + shared control styles (re-skins most things).
2. `DashboardView.xaml` — hero of the app; progress cards, per-platform sections.
3. `InventoryView.xaml` — campaign/reward grid.
4. `SettingsView.xaml` — inputs, checkboxes, toggles.
5. `HelpView.xaml`, then the two login windows (light polish; keep WebView host area clean).
6. Mirror token changes into `Light.xaml` so the theme switch still works.

## Constraints / gotchas
- **Run your own build, not the shipped release.** The upstream app has a
  self-updater ("Github Directory Downloader") that pulls the prebuilt publish
  folder from GitHub and replaces the running app — it can overwrite your XAML.
  Work from the fork, build with `dotnet`, and consider disabling/firewalling the
  updater while iterating.
- Visual work is XAML only — don't touch `Core/` logic or data bindings' names.
- Preserve all `x:Name`, `Binding`, and `Command` references when restyling.
- Keep both Dark and Light themes valid; don't hardcode colors in Views.
```
