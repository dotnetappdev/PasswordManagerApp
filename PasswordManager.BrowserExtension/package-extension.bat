@echo off
REM Password Manager Browser Extension Packaging Script
REM Creates distributable packages for Chrome and Edge browsers

setlocal enabledelayedexpansion

set "SCRIPT_DIR=%~dp0"
set "OUTPUT_DIR=%SCRIPT_DIR%dist"

REM Extract version from manifest.json
for /f "tokens=2 delims=:, " %%a in ('findstr /C:"version" "%SCRIPT_DIR%manifest.json"') do (
    set "VERSION=%%~a"
    goto :version_found
)
:version_found

echo ================================================
echo Password Manager Extension Packaging
echo Version: %VERSION%
echo ================================================
echo.

REM Clean and create output directory
if exist "%OUTPUT_DIR%" rmdir /s /q "%OUTPUT_DIR%"
mkdir "%OUTPUT_DIR%"

REM Create temporary directory
set "TEMP_DIR=%TEMP%\pm-extension-%RANDOM%"
mkdir "%TEMP_DIR%"

echo Step 1: Preparing files...

REM Copy files
copy "%SCRIPT_DIR%manifest.json" "%TEMP_DIR%\" >nul
echo   * Copied: manifest.json

copy "%SCRIPT_DIR%background.js" "%TEMP_DIR%\" >nul
echo   * Copied: background.js

copy "%SCRIPT_DIR%content.js" "%TEMP_DIR%\" >nul
echo   * Copied: content.js

copy "%SCRIPT_DIR%content.css" "%TEMP_DIR%\" >nul
echo   * Copied: content.css

copy "%SCRIPT_DIR%popup.html" "%TEMP_DIR%\" >nul
echo   * Copied: popup.html

copy "%SCRIPT_DIR%popup.js" "%TEMP_DIR%\" >nul
echo   * Copied: popup.js

copy "%SCRIPT_DIR%popup.css" "%TEMP_DIR%\" >nul
echo   * Copied: popup.css

REM Copy icons directory
xcopy "%SCRIPT_DIR%icons" "%TEMP_DIR%\icons\" /E /I /Q >nul
echo   * Copied: icons/

echo.
echo Step 2: Creating ZIP package for distribution...

REM Check if PowerShell is available
where powershell >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    powershell -Command "Compress-Archive -Path '%TEMP_DIR%\*' -DestinationPath '%OUTPUT_DIR%\password-manager-extension-%VERSION%.zip' -Force"
    echo   * Created: password-manager-extension-%VERSION%.zip
) else (
    echo   ! PowerShell not found. Please create ZIP manually from: %TEMP_DIR%
    echo   ! Or install 7-Zip and use: 7z a "%OUTPUT_DIR%\password-manager-extension-%VERSION%.zip" "%TEMP_DIR%\*"
)

REM Clean up temp directory
rmdir /s /q "%TEMP_DIR%"

echo.
echo Step 3: Package information...
echo   Output directory: %OUTPUT_DIR%
echo.

echo ================================================
echo Packaging Complete!
echo ================================================
echo.
echo Package created:
echo    %OUTPUT_DIR%\password-manager-extension-%VERSION%.zip
echo.
echo Next Steps:
echo.
echo For Chrome Web Store:
echo   1. Go to: https://chrome.google.com/webstore/devconsole
echo   2. Upload: password-manager-extension-%VERSION%.zip
echo   3. Fill in store listing details
echo   4. Submit for review
echo.
echo For Microsoft Edge Add-ons:
echo   1. Go to: https://partner.microsoft.com/dashboard/microsoftedge/overview
echo   2. Upload: password-manager-extension-%VERSION%.zip
echo   3. Fill in store listing details
echo   4. Submit for review
echo.
echo For Private Distribution (.crx):
echo   Note: Chrome no longer supports installing .crx files directly
echo   Users must either:
echo   - Install from Chrome Web Store
echo   - Use Developer Mode and load unpacked extension
echo   - Use Enterprise Policy for force-installed extensions
echo.
echo For Development/Testing:
echo   1. Open chrome://extensions/ or edge://extensions/
echo   2. Enable 'Developer mode'
echo   3. Click 'Load unpacked'
echo   4. Select this directory: %SCRIPT_DIR%
echo.

endlocal
pause
