@echo off
setlocal
cd /d "%~dp0"
if exist "%~dp0.tools\dotnet\dotnet.exe" (
  set "DOTNET_ROOT=%~dp0.tools\dotnet"
  "%~dp0.tools\dotnet\dotnet.exe" run --project src/SaabDiag.Desktop -c Release --no-build --no-restore
) else (
  dotnet run --project src/SaabDiag.Desktop -c Release --no-build --no-restore
)
if errorlevel 1 pause
