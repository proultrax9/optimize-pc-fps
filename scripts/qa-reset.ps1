# Reset local app data for manual QA. Close FPS Optimize GOD PC before running.
param(
    [ValidateSet("onboarding", "full")]
    [string]$Mode = "onboarding",
    [switch]$Force
)

$dataRoot = Join-Path $env:LOCALAPPDATA "fps-god-pc"

if (-not (Test-Path $dataRoot)) {
    Write-Host "No data at $dataRoot — next launch is already a first run."
    exit 0
}

if (-not $Force) {
    $prompt = if ($Mode -eq "full") {
        "Delete ALL data under $dataRoot ? (y/N)"
    } else {
        "Delete guardian.db for onboarding reset (keeps tweak state.json)? (y/N)"
    }
    $answer = Read-Host $prompt
    if ($answer -notin @("y", "Y")) {
        Write-Host "Cancelled."
        exit 1
    }
}

if ($Mode -eq "full") {
    Remove-Item -Recurse -Force $dataRoot
    Write-Host "Removed $dataRoot — clean first-run state."
    exit 0
}

$guardianDb = Join-Path $dataRoot "guardian.db"
if (Test-Path $guardianDb) {
    Remove-Item -Force $guardianDb
    Write-Host "Removed guardian.db — onboarding modal will show on next admin launch."
}
else {
    Write-Host "guardian.db not found — onboarding will show on next launch."
}
