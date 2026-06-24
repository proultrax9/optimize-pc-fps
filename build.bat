@echo off
setlocal
cd /d "%~dp0"

set "RELEASE_DIR=release\app"

if exist "%RELEASE_DIR%" rmdir /s /q "%RELEASE_DIR%"
if not exist "release" mkdir "release"
mkdir "%RELEASE_DIR%"

echo Building FPS Optimize GOD PC (Release, win-x64)...
dotnet publish "src\FpsGodPc.App\FpsGodPc.App.csproj" ^
  -c Release ^
  -r win-x64 ^
  --self-contained true ^
  -p:PublishSingleFile=false ^
  -o "%RELEASE_DIR%"

if errorlevel 1 (
  echo Publish failed.
  exit /b 1
)

if not exist "%RELEASE_DIR%\FPS Optimize GOD PC.exe" (
  if exist "%RELEASE_DIR%\FpsGodPc.App.exe" (
    copy /Y "%RELEASE_DIR%\FpsGodPc.App.exe" "%RELEASE_DIR%\FPS Optimize GOD PC.exe" >nul
  ) else (
    echo Build finished but FPS Optimize GOD PC.exe was not found.
    exit /b 1
  )
)

echo.
echo ========================================
echo   FPS Optimize GOD PC v0.3.0 - BUILD OK
echo ========================================
echo   %~dp0%RELEASE_DIR%\FPS Optimize GOD PC.exe
echo ========================================
exit /b 0
