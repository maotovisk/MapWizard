@echo off
setlocal enabledelayedexpansion

if "%~1"=="" (
    echo Version number is required.
    echo Usage: build-win.bat [version]
    exit /b 1
)

set "version=%~1"
set "SCRIPT_DIR=%~dp0"
set "RELEASE_DIR=%SCRIPT_DIR%releases"
set "PUBLISH_DIR=%SCRIPT_DIR%publish"
set "ICON_PATH=%SCRIPT_DIR%Assets\app-icon.ico"

echo.
echo Cleaning up previous build...
dotnet clean "%SCRIPT_DIR%MapWizard.Desktop.csproj"
if errorlevel 1 exit /b %errorlevel%

echo.
echo Compiling MapWizard with dotnet (NativeAOT)...
rem NativeAOT cannot cross-compile between operating systems, so this script
rem must run on Windows. The publish is AOT because MapWizard.Desktop.csproj
rem sets PublishAot=true.
dotnet publish "%SCRIPT_DIR%MapWizard.Desktop.csproj" -c Release --self-contained -r win-x64 -o "%PUBLISH_DIR%"
if errorlevel 1 exit /b %errorlevel%

rem NativeAOT emits debug symbols next to the binary; they are not needed in
rem the installer payload.
del /q "%PUBLISH_DIR%\*.pdb" 2>nul
del /q "%PUBLISH_DIR%\*.dbg" 2>nul

echo.
echo Building Velopack Release v%version%
vpk [win] pack --runtime win-x64 -u MapWizard.Desktop --packTitle "MapWizard" -v %version% -o "%RELEASE_DIR%" -p "%PUBLISH_DIR%" -e "MapWizard.Desktop.exe" -i "%ICON_PATH%"
