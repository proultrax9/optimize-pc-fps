param(
    [string]$Version = "0.1.0",
    [string]$Tag = "v0.1.0"
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$ReleaseDir = Join-Path $Root "release"

if (-not (Test-Path $ReleaseDir)) {
    Write-Error "release/ not found. Run build-exe.bat first."
}

$required = @(
    "FPS Optimize GOD PC.exe",
    "FPS Optimize GOD PC Setup.exe",
    "FPS Optimize GOD PC.msi"
)

foreach ($f in $required) {
    if (-not (Test-Path (Join-Path $ReleaseDir $f))) {
        Write-Warning "Missing: $f (optional for partial publish)"
    }
}

if (Test-Path (Join-Path $Root ".git")) {
    git tag -a $Tag -m "FPS Optimize GOD PC v$Version" 2>$null
}

$notes = @"
## FPS Optimize GOD PC v$Version

### Downloads
- FPS Optimize GOD PC.exe (portable)
- FPS Optimize GOD PC Setup.exe (installer)
- FPS Optimize GOD PC.msi (Windows Installer)

### Highlights
- Merged FPS Unleashed + FPS Unlocker
- Live Monitor with CPU/GPU temp and usage
- Safety Guardian snapshots and game watcher
"@

gh release create $Tag `
    --repo "proultrax9/pc-optimize-fps-god-pc" `
    --title "FPS Optimize GOD PC $Tag" `
    --notes $notes `
    (Join-Path $ReleaseDir "FPS Optimize GOD PC.exe") `
    (Join-Path $ReleaseDir "FPS Optimize GOD PC Setup.exe") `
    (Join-Path $ReleaseDir "FPS Optimize GOD PC.msi")

Write-Host "Published: https://github.com/proultrax9/pc-optimize-fps-god-pc/releases/tag/$Tag" -ForegroundColor Green
