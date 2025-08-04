#!/bin/bash

# Install Playwright browsers for UI testing
echo "Installing Playwright browsers for UI testing..."

cd "$(dirname "$0")"

# Install Playwright CLI tool
dotnet tool install --global Microsoft.Playwright.CLI

# Install browsers
playwright install chromium

echo "Playwright setup complete!"