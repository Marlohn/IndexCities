@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\PlayRoadTool.ps1" -EmptyCity
if errorlevel 1 pause
