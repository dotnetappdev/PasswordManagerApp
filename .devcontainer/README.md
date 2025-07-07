# .NET 9 MAUI Development Container

This development container provides a complete environment for developing .NET 9 MAUI applications with cross-platform support.

## Features

### 🔧 **Pre-installed Tools**
- **.NET 9 SDK** - Latest .NET SDK with all workloads
- **Java JDK 17** - For Android development
- **Android SDK** - Android platform tools and build tools
- **Git & GitHub CLI** - Version control and GitHub integration
- **VS Code Extensions** - C#, Blazor, and development tools

### 📦 **Workloads Installed**
- `maui` - .NET Multi-platform App UI
- `android` - Android development
- `ios` - iOS development (for macOS hosts)
- `maccatalyst` - Mac Catalyst development
- `macos` - macOS development
- `blazor` - Blazor WebAssembly

### 🗄️ **Database Services**
- **SQL Server 2022** - Available at `sqlserver:1433`
- **PostgreSQL 15** - Available at `postgres:5432`

## Quick Start

### 1. **Open in VS Code**
```bash
# Clone the repository
git clone https://github.com/yourusername/PasswordManagerApp.git
cd PasswordManagerApp

# Open in VS Code
code .

# When prompted, click "Reopen in Container"
```

### 2. **Alternative: Command Palette**
1. Open VS Code
2. Press `Ctrl+Shift+P` (or `Cmd+Shift+P` on Mac)
3. Type "Remote-Containers: Open Folder in Container"
4. Select the project folder

### 3. **Build and Run**
```bash
# Restore packages
dotnet restore

# Build the solution
dotnet build

# Run the API
dotnet run --project PasswordManager.API

# Run the MAUI app (in a new terminal)
dotnet run --project PasswordManager.App
```

## Environment Configuration

### **Connection Strings**
The container includes both SQL Server and PostgreSQL. Update your `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=sqlserver;Database=PasswordManagerDB;User=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=true;MultipleActiveResultSets=true",
    "PostgresConnection": "Host=postgres;Database=PasswordManagerDB;Username=postgres;Password=postgres;Port=5432"
  }
}
```

### **Port Forwarding**
The following ports are automatically forwarded:
- `5000` - Web API (HTTP)
- `5001` - Web API (HTTPS)
- `7001` - API Documentation
- `7002` - MAUI Blazor App
- `1433` - SQL Server
- `5432` - PostgreSQL

## Development Workflow

### **Database Setup**
```bash
# Create and apply migrations
dotnet ef migrations add InitialCreate --project PasswordManager.DAL --startup-project PasswordManager.API

# Update database
dotnet ef database update --project PasswordManager.DAL --startup-project PasswordManager.API
```

### **Running Tests**
```bash
# Run all tests
dotnet test

# Run crypto tests
dotnet run --project PasswordManager.Crypto.Tests
```

### **MAUI Development**
```bash
# List available targets
dotnet build --project PasswordManager.App -t:ListTargets

# Build for Android
dotnet build --project PasswordManager.App -f net9.0-android

# Build for Windows
dotnet build --project PasswordManager.App -f net9.0-windows10.0.19041.0
```

## Troubleshooting

### **Common Issues**

1. **Workload Installation Failed**
   ```bash
   dotnet workload update
   dotnet workload install maui
   ```

2. **Android SDK Issues**
   ```bash
   # Check Android SDK
   sdkmanager --list_installed
   
   # Accept licenses
   sdkmanager --licenses
   ```

3. **Permission Issues**
   ```bash
   # Fix file permissions
   sudo chown -R vscode:vscode /workspaces/PasswordManagerApp
   ```

4. **Database Connection Issues**
   ```bash
   # Check if SQL Server is running
   docker ps
   
   # Restart SQL Server container
   docker restart <container_id>
   ```

### **Useful Commands**

```bash
# Check .NET info
dotnet --info

# List installed workloads
dotnet workload list

# Check Java version
java -version

# Check Android SDK
sdkmanager --list_installed

# Run setup script
./setup.sh
```

## VS Code Extensions

The container includes essential extensions:
- **C# for Visual Studio Code** - IntelliSense and debugging
- **Blazor WASM Companion** - Blazor development support
- **JSON** - JSON editing support
- **PowerShell** - PowerShell scripting
- **Tailwind CSS** - CSS utility classes
- **Prettier** - Code formatting
- **Auto Rename Tag** - HTML/XML tag renaming

## Performance Tips

### **Volume Mounts**
- NuGet packages are cached in a Docker volume
- .NET workloads are preserved between container rebuilds
- Source code is mounted for live editing

### **Memory Usage**
- Recommended: 8GB RAM minimum
- Optimal: 16GB RAM for smooth development
- Docker Desktop memory limit: 4GB minimum

### **Build Performance**
```bash
# Use parallel builds
dotnet build --parallel

# Skip restore if packages are up to date
dotnet build --no-restore
```

## Support

For issues specific to the development container:
1. Check the [GitHub Issues](https://github.com/yourusername/PasswordManagerApp/issues)
2. Review the [Container Logs](#logs)
3. Rebuild the container if needed

### **Rebuilding Container**
```bash
# Command Palette > Remote-Containers: Rebuild Container
# Or press Ctrl+Shift+P and type "rebuild"
```

---

**Happy Coding! 🚀**
