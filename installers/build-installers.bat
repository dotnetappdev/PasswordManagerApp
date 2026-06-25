@echo off
REM Build script for creating installers (Windows)

setlocal enabledelayedexpansion

set "SCRIPT_DIR=%~dp0"
set "PROJECT_ROOT=%SCRIPT_DIR%.."
set "OUTPUT_DIR=%SCRIPT_DIR%output"
set "INNO_SETUP=C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

echo ================================================
echo Vault Guard - Installer Build Script
echo ================================================
echo.

REM Check if Inno Setup is installed
if not exist "%INNO_SETUP%" (
    echo Error: Inno Setup not found at: %INNO_SETUP%
    echo Please install Inno Setup 6.x from: https://jrsoftware.org/isdl.php
    exit /b 1
)

REM Check if dotnet is installed
where dotnet >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo Error: .NET SDK is not installed or not in PATH
    echo Please install .NET 9 SDK from: https://dotnet.microsoft.com/download
    exit /b 1
)

REM Create output directory
if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"

echo Step 1: Publishing applications...
echo -----------------------------------
echo.

REM Publish Web API
echo Publishing Web API...
cd "%PROJECT_ROOT%\VaultGuard.API"
dotnet publish -c Release -o bin\Release\net9.0\publish
if %ERRORLEVEL% neq 0 (
    echo Error: Failed to publish Web API
    exit /b 1
)
echo ✅ Published Web API
echo.

REM Publish WinUI Application
echo Publishing WinUI Application...
cd "%PROJECT_ROOT%\VaultGuard.WinUi"
dotnet publish -c Release -p:Platform=x64 -o bin\x64\Release\net9.0-windows10.0.19041.0\win-x64\publish
if %ERRORLEVEL% neq 0 (
    echo Error: Failed to publish WinUI Application
    exit /b 1
)
echo ✅ Published WinUI Application
echo.

echo.
echo Step 2: Building installers...
echo ------------------------------
echo.

REM Build API Installer
cd "%SCRIPT_DIR%"
if exist "api-installer.iss" (
    echo Building Web API installer...
    "%INNO_SETUP%" "api-installer.iss"
    if %ERRORLEVEL% neq 0 (
        echo Error: Failed to build Web API installer
        exit /b 1
    )
    echo ✅ Built Web API installer
    echo.
) else (
    echo ⚠️  api-installer.iss not found, skipping...
)

REM Build WinUI Installer
if exist "winui-installer.iss" (
    echo Building WinUI Application installer...
    "%INNO_SETUP%" "winui-installer.iss"
    if %ERRORLEVEL% neq 0 (
        echo Error: Failed to build WinUI Application installer
        exit /b 1
    )
    echo ✅ Built WinUI Application installer
    echo.
) else (
    echo ⚠️  winui-installer.iss not found, skipping...
)

echo.
echo ================================================
echo Build Complete!
echo ================================================
echo.
echo Installers created in: %OUTPUT_DIR%
dir "%OUTPUT_DIR%"
echo.

endlocal
pause
