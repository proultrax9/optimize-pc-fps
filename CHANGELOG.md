# Changelog

## [0.3.0] — 2026-06-24

### Changed — HyperTune-inspired UI redesign
- **New near-black + mint-green theme.** Reworked the palette to a HyperTune-style design system: near-black surfaces (`#0E1011`) layered with charcoal greys and a mint-green accent (`#13F287`). All pages re-theme automatically via the shared brush keys.
- **Bundled fonts** — Inter Display (headings) and Switzer (body), both free fonts, embedded in the app.
- **Custom dark title bar** (WindowChrome) with brand + minimize/maximize/close, replacing the default window chrome.
- **Sidebar navigation** — active item now shows a green pill with a left accent bar; muted group labels.
- **Glossy cards** with subtle sheen + soft shadow; metric values are big Inter Display numbers in mint green; status chips and risk badges recoloured to the new palette.

## [0.2.2] — 2026-06-23

### Fixed — anti-cheat safety (do not break games)
- **GPU "Prefer Maximum Performance" and "Low Latency Mode" are now guidance-only.** They no longer write to the NVIDIA/AMD driver registry. The old approach (`PerfLevelSrc`, the undocumented `RMHdcpKeyglobZero`) could destabilize the GPU driver and crash anti-cheat games — it crashed Apex Legends (EA AntiCheat). The app now tells you to set these in the vendor control panel instead.
- **`system.ini` latency profile is now guidance-only.** The app no longer edits `%WINDIR%\system.ini` (those legacy entries are ignored by modern 64-bit Windows and editing the file can trip anti-cheat).
- **Boost presets no longer include the three risky tweaks above** — Competitive and Extreme now apply only safe, real optimizations.
- Unified advisor-only detection to a single source so the Tweaks page, Boost, and engine always agree.

### Benchmark
- Space Battle benchmark now uses Unity's open-source Spaceship Demo (no third-party logo) and auto-skips its menu into the benchmark.

## [0.2.1] — 2026-06-23

### Fixed — tweaks now perform real, verifiable changes
- **CPU game priority** now actually raises the detected game process to High priority via the Game Watcher (was a no-op message).
- **CPU timer resolution** now calls `NtSetTimerResolution` for a real 0.5 ms timer and releases it on revert (was a no-op message).
- **GPU max-perf / low-latency** detect the correct driver registry subkey instead of a hardcoded NVIDIA index; on AMD/Intel where the registry tweak does not apply, the app reports honestly instead of falsely marking it applied.
- **Background apps**, **network throttling**, **MMCSS**, **priority separation** now revert to the *captured prior value* instead of a guessed default; background-apps revert actually re-enables them.
- Tweaks are marked "applied" only when the underlying system change succeeds.

### Added — real telemetry & hardware-aware score
- **GPU usage and CPU/GPU temperatures** are now read via LibreHardwareMonitor (previously always blank); thermal safety gating is now live.
- **Performance Score** is computed from the real CPU / GPU / RAM of the machine (hardware tier) and the dashboard surfaces the last measured benchmark FPS — instead of a fixed heuristic that ignored CPU and GPU.

### Fixed — responsiveness & stability
- Games page no longer freezes the UI (Steam scan moved off the UI thread); Profiles page no longer re-runs the recursive scan every 3 s.
- Faster startup (heavy WMI initialization deferred out of service constructors).
- Fixed a localization-event memory leak across page navigation and a recurring process-handle leak in the Game Watcher.
- `ProcessRunner` reads stdout/stderr concurrently (deadlock-safe) with a timeout; the 1 s safety timer no longer hits the database on the UI thread while idle.

## [0.2.0] — 2026-06-23

### Added
- **God Mode** power plan on Network page — max CPU, no sleep, display never turns off.
- Folder-based `release/app/` deployment (full self-contained publish tree).

### Changed
- **UI redesign** — refined dark-purple theme, global button/DataGrid styles, card layouts.
- **Rollback page** — readable dark DataGrid (fixes invisible white rows).
- **Network page** — hero God Mode card + polished latency test panel.
- Heavy operations run **async** (navigation, scan, boost, restore, ping, power plan).

## [0.1.3] — 2026-06-23

### Added
- **FPS Optimize GOD PC Setup.exe** — Inno Setup installer (desktop shortcut optional).
- Sidebar logo from bundled `Assets/logo.png`.

### Changed
- **English only** — removed Thai UI and language switcher.

### Fixed
- Application icon uses the GOD PC lightning logo (`god-pc-logo` → `app.ico`).

## [0.1.2] — 2026-06-23

### Fixed
- **Startup crash after Safety Guardian dialog** — `Assets/app.ico` was missing; `MainWindow` failed to load after clicking OK on the crash-watchdog message.
- **Startup order** — main window opens first; boot safety notice uses the main window as owner so the UI stays visible.
- **Crash watchdog loop** — dirty crash flag is cleared after boot rollback; session marking runs after the UI is shown.
- **Unhandled errors** — startup and dispatcher exceptions now show an error dialog instead of failing silently.

## [0.1.1] — 2026-06-22

### Fixed
- SQLite `GuardianDatabase.Open()` did not call `conn.Open()`, preventing the app from starting.

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
