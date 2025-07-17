#!/bin/bash

# Setup script for Password Manager development container
# This script configures the environment for .NET 9 and MAUI development

set -e

echo "🚀 Setting up Password Manager development environment..."

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

print_status() {
    echo -e "${GREEN}✅${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}⚠️${NC} $1"
}

print_error() {
    echo -e "${RED}❌${NC} $1"
}

# Check if we're in a dev container
if [ -z "$CODESPACES" ] && [ -z "$DEVCONTAINER" ]; then
    print_warning "This script is designed for dev containers (Codespaces)"
fi

# Verify .NET 9 installation
if dotnet --version | grep -q "9."; then
    print_status ".NET 9 SDK is installed"
else
    print_error ".NET 9 SDK not found"
    exit 1
fi

# Install MAUI workloads (Android only)
echo "📦 Installing MAUI workloads..."
if dotnet workload install maui android --skip-sign-check; then
    print_status "MAUI Android workload installed"
else
    print_error "Failed to install MAUI workloads"
    exit 1
fi

# Verify Android SDK
if [ -d "$ANDROID_SDK_ROOT" ]; then
    print_status "Android SDK configured at $ANDROID_SDK_ROOT"
else
    print_warning "Android SDK not found - MAUI Android builds may not work"
fi

# Restore NuGet packages
echo "📦 Restoring NuGet packages..."
if dotnet restore PasswordManager.sln; then
    print_status "NuGet packages restored successfully"
else
    print_error "Failed to restore NuGet packages"
    exit 1
fi

# Set up git configuration (if not already set)
if [ -z "$(git config --global user.name)" ]; then
    echo "🔧 Setting up git configuration..."
    git config --global user.name "Codespace User"
    git config --global user.email "user@codespace.local"
    print_status "Git configuration set"
fi

# Create useful aliases
echo "🔧 Setting up development aliases..."
cat >> ~/.bashrc << 'EOF'

# Password Manager Development Aliases
alias pm-build='dotnet build PasswordManager.sln'
alias pm-clean='dotnet clean PasswordManager.sln'
alias pm-restore='dotnet restore PasswordManager.sln'
alias pm-api='dotnet run --project PasswordManager.API'
alias pm-web='dotnet run --project PasswordManager.Web'
alias pm-test='dotnet test'
alias pm-info='dev-info'
alias pm-docker='./docker-run.sh'

# Development shortcuts
alias ll='ls -la'
alias la='ls -la'
alias ..='cd ..'
alias ...='cd ../..'

EOF

print_status "Development aliases configured"

# Show environment information
echo ""
echo "🎉 Development environment setup complete!"
echo ""
echo "📋 Environment Information:"
echo "   .NET Version: $(dotnet --version)"
echo "   MAUI Workloads: Android (iOS/macOS excluded)"
echo "   Java Version: $(java -version 2>&1 | head -n 1)"
echo "   Android SDK: $ANDROID_SDK_ROOT"
echo ""
echo "🔧 Available Commands:"
echo "   pm-build    - Build the solution"
echo "   pm-api      - Run the API project"
echo "   pm-web      - Run the Web project"
echo "   pm-docker   - Docker management script"
echo "   pm-info     - Show development info"
echo ""
echo "🌐 Default Ports:"
echo "   5000/5001   - API (HTTP/HTTPS)"
echo "   5002/5003   - Web (HTTP/HTTPS)"
echo "   5432        - PostgreSQL"
echo "   3306        - MySQL"
echo "   1433        - SQL Server"
echo ""
echo "Happy coding! 🚀"
