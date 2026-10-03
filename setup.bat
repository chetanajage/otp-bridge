@echo off
rem OTP Bridge setup: double-click me. Everything else happens in setup.ps1.
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0setup.ps1"
if errorlevel 1 pause
