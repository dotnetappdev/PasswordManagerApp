@echo off
REM Setup script for generating HTTPS development certificates for Docker (Windows)

setlocal enabledelayedexpansion

echo ================================================
echo Password Manager - Docker HTTPS Certificate Setup
echo ================================================
echo.

REM Create certs directory if it doesn't exist
set "CERTS_DIR=%~dp0certs"
if not exist "%CERTS_DIR%" mkdir "%CERTS_DIR%"

echo Creating HTTPS development certificate...
echo.

REM Check if dotnet is installed
where dotnet >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo Error: .NET SDK is not installed or not in PATH
    echo Please install .NET 9 SDK from https://dotnet.microsoft.com/download
    exit /b 1
)

REM Check if certificate already exists
if exist "%CERTS_DIR%\aspnetapp.pfx" (
    echo Certificate already exists at: %CERTS_DIR%\aspnetapp.pfx
    set /p "REGENERATE=Do you want to regenerate it? (y/N): "
    if /i not "!REGENERATE!"=="y" (
        echo Using existing certificate.
        exit /b 0
    )
    del "%CERTS_DIR%\aspnetapp.pfx"
)

REM Clean existing dev certs
echo Cleaning existing development certificates...
dotnet dev-certs https --clean

REM Create new dev certificate
echo Generating new development certificate...
dotnet dev-certs https -ep "%CERTS_DIR%\aspnetapp.pfx" -p ""

REM Trust the certificate (optional, for local development)
set /p "TRUST=Do you want to trust this certificate on your local machine? (y/N): "
if /i "!TRUST!"=="y" (
    echo Trusting certificate...
    dotnet dev-certs https --trust
)

echo.
echo ✅ Certificate setup complete!
echo.
echo Certificate location: %CERTS_DIR%\aspnetapp.pfx
echo Certificate password: (empty - no password)
echo.
echo You can now start the Docker containers with:
echo   cd docker
echo   docker-compose up -d
echo.

endlocal
pause
