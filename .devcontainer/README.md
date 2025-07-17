# Development Container Setup

This development container is configured for the Password Manager application with .NET 9 and MAUI support, optimized for GitHub Codespaces.

## Features

- ✅ .NET 9 SDK
- ✅ MAUI workloads (Android only)
- ✅ Android SDK for MAUI development
- ✅ PostgreSQL, MySQL, and SQL Server databases
- ✅ VS Code extensions for .NET and MAUI development
- ✅ Docker support
- ❌ iOS/macOS builds excluded (not supported in Codespaces)

## Quick Start

1. **Open in Codespaces**: Click the "Code" button and select "Create codespace on develop1"
2. **Wait for setup**: The container will automatically install dependencies and configure the environment
3. **Start developing**: Use the provided aliases and commands to build and run the application

## Available Commands

### Development Aliases
- `pm-build` - Build the entire solution
- `pm-clean` - Clean build artifacts
- `pm-restore` - Restore NuGet packages
- `pm-api` - Run the API project
- `pm-web` - Run the Web project
- `pm-test` - Run tests
- `pm-docker` - Docker management script
- `pm-info` - Show development environment info

### Docker Commands
- `./docker-run.sh build-all` - Build all Docker containers
- `./docker-run.sh run-all` - Run all services with Docker Compose
- `./docker-run.sh run-ci` - Run in CI mode

## Default Ports

| Service | Port | Description |
|---------|------|-------------|
| API (HTTP) | 5000 | Password Manager API |
| API (HTTPS) | 5001 | Password Manager API (SSL) |
| Web (HTTP) | 5002 | Password Manager Web UI |
| Web (HTTPS) | 5003 | Password Manager Web UI (SSL) |
| PostgreSQL | 5432 | Default database |
| MySQL | 3306 | Alternative database |
| SQL Server | 1433 | Alternative database |

## Database Configuration

The dev container automatically starts a PostgreSQL database with the following credentials:
- **Database**: `passwordmanager_dev`
- **Username**: `dev`
- **Password**: `dev123`
- **Host**: `localhost`
- **Port**: `5432`

## MAUI Development

The container includes Android SDK and MAUI workloads for Android development:
- Android SDK Root: `/opt/android-sdk`
- Target Framework: `net9.0-android`
- iOS/macOS builds are excluded and not supported in Codespaces

## Environment Variables

The following environment variables are set:
- `EXCLUDE_IOS_MACOS=true`
- `DOTNET_MAUI_SKIP_IOS=true`
- `DOTNET_MAUI_SKIP_MACCATALYST=true`
- `ASPNETCORE_ENVIRONMENT=Development`
- `DOTNET_USE_POLLING_FILE_WATCHER=true`
- `DOTNET_RUNNING_IN_CONTAINER=true`

## Troubleshooting

### Build Issues
1. Run `pm-clean` followed by `pm-restore` and `pm-build`
2. Check that all required workloads are installed: `dotnet workload list`

### Database Connection Issues
1. Verify database is running: `docker ps`
2. Check database logs: `docker logs passwordmanager-dev-db`
3. Restart database: `docker-compose -f .devcontainer/docker-compose.yml restart`

### MAUI Build Issues
1. Ensure Android SDK is properly configured: `echo $ANDROID_SDK_ROOT`
2. Check Java installation: `java -version`
3. Verify MAUI workload: `dotnet workload list`

## Support

For issues with the development container, please check:
1. The setup script output during container creation
2. VS Code dev container logs
3. Docker container logs for database services
