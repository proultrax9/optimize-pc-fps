param(
    [string]$Version = "0.3.0",
    [string]$Tag = "v0.3.0"
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$ReleaseDir = Join-Path $Root "release"
$Repo = "proultrax9/optimize-pc-fps"

if (-not (Test-Path $ReleaseDir)) {
    Write-Error "release/ not found. Run build.bat then build-installer.ps1 first."
}

# Publish whichever artifacts exist (installer + portable; .msi is optional).
$candidates = @(
    "FPS Optimize GOD PC Setup.exe",
    "FPS Optimize GOD PC.exe",
    "FPS Optimize GOD PC.msi"
)
$assets = @()
foreach ($f in $candidates) {
    $path = Join-Path $ReleaseDir $f
    if (Test-Path $path) { $assets += $path } else { Write-Warning "Skipping missing asset: $f" }
}
if ($assets.Count -eq 0) {
    Write-Error "No release assets found in release/. Build them first."
}

$notes = @"
## FPS Optimize GOD PC $Tag

### Downloads
- FPS Optimize GOD PC Setup.exe (installer, recommended)
- FPS Optimize GOD PC.exe (portable, self-contained)

### Highlights
- HyperTune-style UI: top-bar shell, radial-gauge Live Monitor, Game Hub
- Themed dialogs replacing native message boxes; rebalanced typography
- Safety Guardian snapshots + game watcher
"@

gh release create $Tag --repo $Repo --title "FPS Optimize GOD PC $Tag" --notes $notes $assets

Write-Host "Published: https://github.com/$Repo/releases/tag/$Tag" -ForegroundColor Green
