#!/bin/bash

# Script to set up .NET 9.0 SDK for this repository
# This script should be run once to install .NET 9.0 SDK

echo "Setting up .NET 9.0 SDK..."

# Install .NET 9.0 SDK if not already installed
if ! command -v dotnet &> /dev/null || ! dotnet --version | grep -q "9.0"; then
    echo "Installing .NET 9.0 SDK..."
    curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 9.0
fi

# Add .NET 9.0 to PATH for current session
export PATH="/home/runner/.dotnet:$PATH"

echo "✅ .NET 9.0 SDK setup complete!"
echo "Current .NET version: $(dotnet --version)"

# Test build a core project to verify setup
echo "Testing build with .NET 9.0..."
dotnet build PasswordManager.Models/PasswordManager.Models.csproj

echo "✅ Setup verified successfully!"