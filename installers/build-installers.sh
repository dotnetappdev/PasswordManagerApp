#!/bin/bash
# Build script for creating installers (Linux/Mac - using Wine for Inno Setup)
# Note: This script requires Wine and Inno Setup installed via Wine

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
OUTPUT_DIR="$SCRIPT_DIR/output"

echo "================================================"
echo "Vault Guard - Installer Build Script"
echo "================================================"
echo ""

# Check if Wine is installed
if ! command -v wine &> /dev/null; then
    echo "Warning: Wine is not installed. Inno Setup requires Windows or Wine."
    echo "Please install Wine to build installers on Linux/Mac."
    echo "Alternative: Build on Windows or use GitHub Actions."
    exit 1
fi

# Create output directory
mkdir -p "$OUTPUT_DIR"

# Function to publish application
publish_app() {
    local project=$1
    local output=$2
    local platform=$3
    
    echo "Publishing $project..."
    cd "$PROJECT_ROOT/$project"
    
    if [ "$project" = "VaultGuard.API" ]; then
        dotnet publish -c Release -o "bin/Release/net9.0/publish"
    else
        dotnet publish -c Release -p:Platform=x64 -o "bin/x64/Release/net9.0-windows10.0.19041.0/win-x64/publish"
    fi
    
    echo "✅ Published $project"
    echo ""
}

# Function to build installer
build_installer() {
    local script=$1
    local name=$2
    
    echo "Building $name installer..."
    cd "$SCRIPT_DIR"
    
    # Use Wine to run Inno Setup compiler
    wine "C:\\Program Files (x86)\\Inno Setup 6\\ISCC.exe" "$script"
    
    echo "✅ Built $name installer"
    echo ""
}

# Main build process
main() {
    echo "Step 1: Publishing applications..."
    echo "-----------------------------------"
    publish_app "VaultGuard.API"
    publish_app "VaultGuard.WinUi"
    
    echo ""
    echo "Step 2: Building installers..."
    echo "------------------------------"
    
    if [ -f "$SCRIPT_DIR/api-installer.iss" ]; then
        build_installer "api-installer.iss" "Web API"
    else
        echo "⚠️  api-installer.iss not found, skipping..."
    fi
    
    if [ -f "$SCRIPT_DIR/winui-installer.iss" ]; then
        build_installer "winui-installer.iss" "WinUI Application"
    else
        echo "⚠️  winui-installer.iss not found, skipping..."
    fi
    
    echo ""
    echo "================================================"
    echo "Build Complete!"
    echo "================================================"
    echo ""
    echo "Installers created in: $OUTPUT_DIR"
    ls -lh "$OUTPUT_DIR"
}

# Run main function
main
