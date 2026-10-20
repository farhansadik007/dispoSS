@echo off
setlocal
cd /d "%~dp0"

set "DOTNET=dotnet"
where dotnet >nul 2>nul
if errorlevel 1 if exist "%ProgramFiles%\dotnet\dotnet.exe" set "DOTNET=%ProgramFiles%\dotnet\dotnet.exe"

"%DOTNET%" build "%~dp0DisposableScreenshots.slnx" -c Debug
exit /b %errorlevel%
