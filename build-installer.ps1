param(
    [switch]$SkipAppBuild
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

function Find-InnoCompiler {
    $candidates = @(
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
    )
    foreach ($path in $candidates) {
        if (Test-Path $path) { return $path }
    }
    return $null
}

if (-not $SkipAppBuild) {
    & "$root\build.bat"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

$exe = Join-Path $root "release\app\FPS Optimize GOD PC.exe"
if (-not (Test-Path $exe)) {
    Write-Error "Published app exe not found: $exe"
}

$iscc = Find-InnoCompiler
if (-not $iscc) {
    Write-Host "Inno Setup 6 not found. Installing via winget..."
    winget install -e --id JRSoftware.InnoSetup --accept-package-agreements --accept-source-agreements
    $iscc = Find-InnoCompiler
}

if (-not $iscc) {
    Write-Error @"
Inno Setup 6 is required to build FPS Optimize GOD PC Setup.exe.
Install from https://jrsoftware.org/isinfo.php then re-run:
  .\build-installer.ps1
"@
}

& $iscc (Join-Path $root "installer\setup.iss")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$setup = Join-Path $root "release\FPS Optimize GOD PC Setup.exe"
if (Test-Path $setup) {
    $sizeKb = [math]::Round((Get-Item $setup).Length / 1KB)
    Write-Host ""
    Write-Host "========================================"
    Write-Host "  FPS Optimize GOD PC Setup.exe - OK"
    Write-Host "  $setup ($sizeKb KB)"
    Write-Host "========================================"
}
