# Changelog

## [0.1.0] — 2026-06-22

### Added
- Native WPF (.NET 8) desktop app merging FPS Unleashed + FPS Unlocker
- 12 pages: Dashboard, Benchmark, Scanner, Tweaks, Boost, Profiles, Safety, Cleaner, Rollback, Games, Network, Settings
- EN/TH localization with sidebar language switch
- 31 performance tweaks + 4 boost presets + Expert Guide checklist
- Safety Guardian: SQLite snapshots, spec gate, 15s confirm timer, crash watchdog, boot auto-revert
- Game Watcher background service (auto-apply profile tweaks on game launch)
- PresentMon benchmark support with telemetry fallback
- Live Dashboard telemetry (2s refresh)
- Steam/game path detection for profiles
- GitHub Actions CI workflow
- Portable single-file `FPS Optimize GOD PC.exe`

### Fixed
- MVVM source generator conflicts (ViewModels in separate project)
- Duplicate XAML code-behind classes from legacy migration
- Expert checklist wiring without ViewModels→App circular reference

### Improved (overnight polish)
- Full EN/TH names and descriptions for all 31 tweaks
- Localized status messages (boost, snapshots, safety, PresentMon, pending revert)
- Settings page shows app version
- Rollback page shows localized tweak names and boost labels
- Expert Guide checklist steps and warnings (EN/TH)
- Game Watcher events and profile names localized
- Games page priority and tips localized
- Guardian tweak result messages localized
- MIT LICENSE, `.gitattributes`, git repo initialized
