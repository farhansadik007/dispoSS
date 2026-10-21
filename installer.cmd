@echo off
setlocal
cd /d "%~dp0"

set "ISCC="
if exist "%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe" set "ISCC=%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe"
if not defined ISCC if exist "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
if not defined ISCC if exist "%ProgramFiles%\Inno Setup 6\ISCC.exe" set "ISCC=%ProgramFiles%\Inno Setup 6\ISCC.exe"
if not defined ISCC for %%I in (ISCC.exe) do if not "%%~$PATH:I"=="" set "ISCC=%%~$PATH:I"

if not defined ISCC (
  echo Inno Setup 6 was not found.
  echo Install it with: winget install JRSoftware.InnoSetup
  exit /b 1
)

if not exist "dist\win-x64\Snipt.exe" (
  echo Published executable not found; running publish.cmd first.
  call "%~dp0publish.cmd"
  if errorlevel 1 exit /b %errorlevel%
)

"%ISCC%" "%~dp0installer.iss"
if errorlevel 1 exit /b %errorlevel%

echo.
echo Installer: %~dp0dist\Snipt-Setup.exe
exit /b 0
