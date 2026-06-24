@echo off
setlocal
cd /d "%~dp0"

net session >nul 2>&1
if errorlevel 1 (
  echo Requesting Administrator privileges...
  powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
  exit /b
)

if exist "release\app\FPS Optimize GOD PC.exe" (
  start "" "release\app\FPS Optimize GOD PC.exe"
) else if exist "src\FpsGodPc.App\bin\Release\net8.0-windows10.0.19041.0\win-x64\FPS Optimize GOD PC.exe" (
  start "" "src\FpsGodPc.App\bin\Release\net8.0-windows10.0.19041.0\win-x64\FPS Optimize GOD PC.exe"
) else (
  echo Run build.bat first to create release\app\FPS Optimize GOD PC.exe
  pause
)
