@echo off
setlocal
cd /d "%~dp0"

if not exist "release" mkdir "release"

echo Building FPS Optimize GOD PC (Release, win-x64)...
dotnet publish "src\FpsGodPc.App\FpsGodPc.App.csproj" ^
  -c Release ^
  -r win-x64 ^
  --self-contained true ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -o "release"

if errorlevel 1 (
  echo Publish failed.
  exit /b 1
)

if not exist "release\FPS Optimize GOD PC.exe" (
  if exist "release\FpsGodPc.App.exe" (
    copy /Y "release\FpsGodPc.App.exe" "release\FPS Optimize GOD PC.exe" >nul
  ) else (
    echo Build finished but FPS Optimize GOD PC.exe was not found.
    exit /b 1
  )
)

echo.
echo ========================================
echo   FPS Optimize GOD PC v0.1.0 - BUILD OK
echo ========================================
echo   %~dp0release\FPS Optimize GOD PC.exe
echo ========================================
exit /b 0
