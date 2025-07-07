#!/bin/bash

# .NET 9 MAUI Development Container Setup Script
echo "🚀 Setting up .NET 9 MAUI development environment..."

# Update workloads
echo "📦 Updating .NET workloads..."
dotnet workload update

# Install all required workloads
echo "🔧 Installing .NET workloads..."
dotnet workload install maui
dotnet workload install android
dotnet workload install ios
dotnet workload install maccatalyst
dotnet workload install macos
dotnet workload install blazor

# Restore NuGet packages
echo "📚 Restoring NuGet packages..."
dotnet restore

# Trust development certificates
echo "🔐 Trusting development certificates..."
dotnet dev-certs https --trust

# Display installed workloads
echo "✅ Installed workloads:"
dotnet workload list

# Display .NET info
echo "ℹ️ .NET Information:"
dotnet --info

# Check Android SDK
echo "🤖 Android SDK Information:"
if command -v sdkmanager &> /dev/null; then
    sdkmanager --list_installed
else
    echo "Android SDK not found or not in PATH"
fi

# Check Java
echo "☕ Java Information:"
java -version

echo "🎉 Development environment setup complete!"
echo ""
echo "📋 Quick Start:"
echo "  1. Build the solution: dotnet build"
echo "  2. Run the API: dotnet run --project PasswordManager.API"
echo "  3. Run the MAUI app: dotnet run --project PasswordManager.App"
echo ""
echo "🌐 Useful URLs:"
echo "  - API Documentation: https://localhost:7001/scalar/v1"
echo "  - API Health Check: https://localhost:7001/health"
echo ""
echo "🗄️ Database Connections:"
echo "  - SQL Server: Server=sqlserver;Database=PasswordManagerDB;User=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=true"
echo "  - PostgreSQL: Host=postgres;Database=PasswordManagerDB;Username=postgres;Password=postgres"
