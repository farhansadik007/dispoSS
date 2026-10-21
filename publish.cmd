@echo off
setlocal
cd /d "%~dp0"

set "DOTNET=dotnet"
where dotnet >nul 2>nul
if errorlevel 1 if exist "%ProgramFiles%\dotnet\dotnet.exe" set "DOTNET=%ProgramFiles%\dotnet\dotnet.exe"

if exist "dist\win-x64" rmdir /s /q "dist\win-x64"
mkdir "dist\win-x64"

"%DOTNET%" publish "%~dp0src\DisposableScreenshots\DisposableScreenshots.csproj" ^
  -c Release ^
  -p:DebugType=None ^
  -p:AllowedReferenceRelatedFileExtensions=none ^
  -o "%~dp0dist\win-x64"

exit /b %errorlevel%
