@echo off
setlocal
cd /d "%~dp0"

set "DOTNET=dotnet"
where dotnet >nul 2>nul
if errorlevel 1 if exist "%ProgramFiles%\dotnet\dotnet.exe" set "DOTNET=%ProgramFiles%\dotnet\dotnet.exe"

set "DOTNET_WATCH_SUPPRESS_EMOJIS=1"
set "DOTNET_WATCH_RESTART_ON_RUDE_EDIT=1"
set "DISPOSABLE_SS_DEV=1"

echo Starting Disposable Screenshots in dev mode...
echo C# edits use hot reload. XAML edits rebuild and restart the app.
echo Press Ctrl+R in this window to force a restart.
echo.

"%DOTNET%" watch run --project "%~dp0src\DisposableScreenshots\DisposableScreenshots.csproj" --non-interactive
