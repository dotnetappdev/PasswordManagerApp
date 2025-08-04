@echo off
setlocal enabledelayedexpansion

echo Password Manager Native Host Installer
echo =====================================
echo.

REM Check if running as administrator
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo This script must be run as Administrator.
    echo Right-click on this file and select "Run as administrator"
    pause
    exit /b 1
)

REM Default installation directory
set "INSTALL_DIR=%ProgramFiles%\PasswordManager\NativeHost"

echo Installing Password Manager Native Host...
echo Installation directory: %INSTALL_DIR%
echo.

REM Create installation directory
if not exist "%INSTALL_DIR%" (
    mkdir "%INSTALL_DIR%"
    if !errorLevel! neq 0 (
        echo Failed to create installation directory
        pause
        exit /b 1
    )
)

REM Build the native host
echo Building native messaging host...
dotnet publish -c Release -r win-x64 --self-contained true --single-file -o "%INSTALL_DIR%"
if !errorLevel! neq 0 (
    echo Failed to build native messaging host
    pause
    exit /b 1
)

REM Update the manifest file with correct paths
set "MANIFEST_FILE=%INSTALL_DIR%\com.passwordmanager.native_host.json"
set "EXECUTABLE_PATH=%INSTALL_DIR%\PasswordManager.BrowserExtension.NativeHost.exe"

REM Create the manifest file with correct paths
(
echo {
echo   "name": "com.passwordmanager.native_host",
echo   "description": "Password Manager Native Messaging Host",
echo   "path": "%EXECUTABLE_PATH:\=\\%",
echo   "type": "stdio",
echo   "allowed_origins": [
echo     "chrome-extension://EXTENSION_ID_PLACEHOLDER/"
echo   ]
echo }
) > "%MANIFEST_FILE%"

echo.
echo Native host installed successfully!
echo.
echo Next steps:
echo 1. Install your browser extension and note its Extension ID
echo 2. Edit the manifest file to replace EXTENSION_ID_PLACEHOLDER:
echo    %MANIFEST_FILE%
echo 3. Register the native messaging host for your browser:
echo.
echo For Chrome:
echo reg add "HKEY_CURRENT_USER\Software\Google\Chrome\NativeMessagingHosts\com.passwordmanager.native_host" /ve /t REG_SZ /d "%MANIFEST_FILE%" /f
echo.
echo For Edge:
echo reg add "HKEY_CURRENT_USER\Software\Microsoft\Edge\NativeMessagingHosts\com.passwordmanager.native_host" /ve /t REG_SZ /d "%MANIFEST_FILE%" /f
echo.
echo Press any key to continue...
pause >nul