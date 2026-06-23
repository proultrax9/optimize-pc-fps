# GitHub Release — FPS Optimize GOD PC

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows x64 machine (or CI artifact from `.github/workflows/build.yml`)
- GitHub repo with push access

## Build release binary

```powershell
cd "FPS Optimize GOD PC"
.\build.bat
```

Output: `release/FPS Optimize GOD PC.exe` (single-file, self-contained, ~76 MB)

## Create a GitHub release

1. Commit and push all changes to `main`.
2. Tag the version:

```powershell
git tag v0.1.0
git push origin v0.1.0
```

3. Create release (GitHub CLI):

```powershell
gh release create v0.1.0 `
  "release/FPS Optimize GOD PC.exe" `
  --title "FPS Optimize GOD PC v0.1.0" `
  --notes "## FPS Optimize GOD PC v0.1.0

Native WPF (.NET 8) Windows optimizer — FPS Unleashed + FPS Unlocker merged.

### Highlights
- 12-page dark purple UI (EN/TH)
- 31 performance tweaks + 4 boost presets + Expert Guide checklist
- Safety Guardian (SQLite snapshots, spec gate, crash watchdog)
- Live CPU/GPU/RAM telemetry on Dashboard
- Portable single-file exe — run as **Administrator**

### Requirements
- Windows 10/11 x64
- Administrator for registry and power-plan tweaks

### Quick start
1. Download \`FPS Optimize GOD PC.exe\`
2. Right-click → **Run as administrator**
   Or use \`Run FPS Optimize GOD PC.bat\` from the repo

### Data paths
- \`%LOCALAPPDATA%\\fps-god-pc\\\` — tweak state
- \`%LOCALAPPDATA%\\fps-god-pc\\guardian.db\` — guardian DB"
```

## CI alternative

Push to `main` triggers the workflow; download the `FPS-Optimize-GOD-PC-win-x64` artifact from the Actions tab and attach it to the release manually.

## Manual QA checklist (when you wake up)

Run as **Administrator** via `Run FPS Optimize GOD PC.bat`.

### Reset state for QA

Close the app first, then:

```powershell
# Show onboarding again (keeps tweak state.json)
.\scripts\qa-reset.ps1 -Mode onboarding

# Full clean first-run (deletes all tweak + guardian data)
.\scripts\qa-reset.ps1 -Mode full
```

### Checklist
- [ ] Sidebar language **th** → all nav labels update
- [ ] Dashboard: metrics refresh every ~2s
- [ ] Scanner: run scan → **Apply recommended boost**
- [ ] Tweaks: toggle moderate tweak → confirm dialog
- [ ] Boost: Expert → checklist window; Extreme → warning
- [ ] Profiles: enable Watcher → status updates; launch game → tweaks apply
- [ ] Safety: guardian tweak moderate → 15s confirm banner (shell)
- [ ] Benchmark: run 30s (with/without PresentMon in `%LOCALAPPDATA%\fps-god-pc\`)
- [ ] Cleaner, Rollback restore point, Network ping
- [ ] Settings: save toggles, paths visible
- [ ] Crash watchdog banner after dirty exit (optional)

## Version bump checklist

- [ ] Update `Version` in `src/FpsGodPc.App/FpsGodPc.App.csproj`
- [ ] Update README.md / README_TH.md version lines
- [ ] Run `dotnet build FpsGodPc.sln` (0 errors)
- [ ] Run `.\build.bat`
- [ ] Smoke test as Administrator (see README)
- [ ] Tag + `gh release create`
