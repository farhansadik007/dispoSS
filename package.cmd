@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0store\package.ps1" %*
exit /b %errorlevel%
