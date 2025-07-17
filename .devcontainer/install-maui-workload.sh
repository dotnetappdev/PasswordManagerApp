#!/bin/bash
# Install .NET MAUI workloads (Android only) for PasswordManager.App
set -e
echo "Installing .NET MAUI Android workload..."
dotnet workload install maui android --skip-sign-check
echo "✅ MAUI Android workload installed. You can now build the .App project with:"
echo "   dotnet build PasswordManager.App/PasswordManager.App.csproj -f net9.0-android"
