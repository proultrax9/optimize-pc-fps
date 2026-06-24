**English** · [ไทย (archived)](README_TH.md)

# FPS Optimize GOD PC

Native **WPF (.NET 8)** Windows desktop optimizer combining **FPS Unleashed** (performance tweaks, boost, scanner) with **FPS Unlocker** (Safety Guardian, live telemetry, snapshots, game watcher).

| | |
|---|---|
| **Product** | FPS Optimize GOD PC |
| **Version** | 0.3.0 |
| **Stack** | WPF · .NET 8 · C# |
| **Platform** | Windows 10 / 11 (x64) |
| **Folder** | `FPS Optimize GOD PC/` |
| **Workspace** | `Optimize PC/FPS Optimize GOD PC/` |

## Requirements

- **Windows 10/11** (x64)
- **[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)** — for building from source
- **Administrator** — required at runtime for registry, power-plan, and system tweaks

## Features

- **Live Monitor** — real-time CPU/GPU %, temperature, RAM, process count
- **Performance** — 31+ tweaks, boost presets, scanner, cleaner, rollback, games, network
- **Safety Guardian** — SQLite snapshots, spec gate, crash watchdog, benchmark, watcher profiles
- **Game Watcher** — auto-applies profile tweaks when a watched game starts (background, 2s poll)
- **PresentMon** — real FPS benchmark when `PresentMon.exe` is in `%LOCALAPPDATA%\fps-god-pc\`
- **i18n** — English
- **Native WPF UI** — single-file self-contained `.exe`, no Node.js or Rust toolchain needed to run

## Screenshots

> Placeholder — add screenshots to `docs/screenshots/` and link them here.

| Dashboard | Tweaks | Safety |
|-----------|--------|--------|
| *(screenshot pending)* | *(screenshot pending)* | *(screenshot pending)* |

## Release files (.exe)

```
Optimize PC/FPS Optimize GOD PC/release/app/                    ← full app folder (run the .exe inside)
Optimize PC/FPS Optimize GOD PC/release/FPS Optimize GOD PC Setup.exe  ← installer
```

Rebuild portable folder: `.\build.bat`  
Rebuild installer: `.\build-installer.ps1`

## Quick start (dev)

```powershell
cd "Optimize PC\FPS Optimize GOD PC"
dotnet build FpsGodPc.sln
dotnet run --project src\FpsGodPc.App\FpsGodPc.App.csproj
```

**Portable:** `Run FPS Optimize GOD PC.bat` (elevates to Administrator) or `release\FPS Optimize GOD PC.exe`  
**Build release:** `.\build.bat` → `release\FPS Optimize GOD PC.exe` (single-file, self-contained win-x64)

> Run as **Administrator** for registry and power-plan tweaks.

## Solution layout

| Project | Role |
|---------|------|
| `FpsGodPc.App` | WPF shell, views, themes |
| `FpsGodPc.ViewModels` | MVVM view models |
| `FpsGodPc.Services` | Windows/system services |
| `FpsGodPc.Core` | Models, shared logic |

## Navigation

| Group | Pages |
|-------|-------|
| **Monitor** | Dashboard (Live), Benchmark |
| **Performance** | Scanner, Tweaks, Boost |
| **Optimize** | Profiles (Watcher), Safety |
| **Tools** | Cleaner, Rollback, Games, Network |

## PresentMon (optional)

Place `PresentMon.exe` in `%LOCALAPPDATA%\fps-god-pc\` for real FPS metrics.

## Data

| Store | Path |
|-------|------|
| App tweaks | `%LOCALAPPDATA%\fps-god-pc\` |
| Guardian DB | `%LOCALAPPDATA%\fps-god-pc\guardian.db` |

## Legacy Tauri build

The previous Tauri 2 + React stack is archived under `archive/tauri-legacy/`. Use the WPF build above for current development.

## GitHub release

See [RELEASE.md](RELEASE.md) for tagging and uploading `FPS Optimize GOD PC.exe`.

CI: `.github/workflows/build.yml` builds on push to `main` and uploads the portable exe as an artifact.

See `CHANGELOG.md` and `RELEASE.md` (includes QA checklist).

## License

MIT — see [LICENSE](LICENSE).
