#!/bin/bash

# Password Manager Browser Extension Packaging Script
# Creates distributable packages for Chrome and Edge browsers

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OUTPUT_DIR="$SCRIPT_DIR/dist"

# Check if manifest.json exists
if [ ! -f "$SCRIPT_DIR/manifest.json" ]; then
    echo "Error: manifest.json not found in $SCRIPT_DIR"
    exit 1
fi

# Extract version from manifest.json
VERSION=$(grep '"version"' "$SCRIPT_DIR/manifest.json" | sed 's/.*"version": "\(.*\)".*/\1/')

# Verify version was extracted
if [ -z "$VERSION" ]; then
    echo "Error: Could not extract version from manifest.json"
    exit 1
fi

echo "================================================"
echo "Password Manager Extension Packaging"
echo "Version: $VERSION"
echo "================================================"
echo ""

# Clean and create output directory
rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"

# Files to include in the package
FILES=(
    "manifest.json"
    "background.js"
    "content.js"
    "content.css"
    "popup.html"
    "popup.js"
    "popup.css"
    "icons/"
)

# Create temporary directory for packaging
TEMP_DIR=$(mktemp -d)
trap "rm -rf $TEMP_DIR" EXIT

echo "Step 1: Preparing files..."
for item in "${FILES[@]}"; do
    if [ -d "$SCRIPT_DIR/$item" ]; then
        cp -r "$SCRIPT_DIR/$item" "$TEMP_DIR/"
        echo "  ✓ Copied directory: $item"
    elif [ -f "$SCRIPT_DIR/$item" ]; then
        cp "$SCRIPT_DIR/$item" "$TEMP_DIR/"
        echo "  ✓ Copied file: $item"
    else
        echo "  ✗ Warning: $item not found"
    fi
done

echo ""
echo "Step 2: Creating ZIP package for distribution..."
cd "$TEMP_DIR"

# Create ZIP with suppressed verbose output but show errors
if zip -r "$OUTPUT_DIR/password-manager-extension-$VERSION.zip" . -x "*.git*" "*.DS_Store" -q; then
    echo "  ✓ Created: password-manager-extension-$VERSION.zip"
    
    echo ""
    echo "================================================"
    echo "Packaging Complete!"
    echo "================================================"
    echo ""
    echo "📦 Distribution Package:"
    echo "   $OUTPUT_DIR/password-manager-extension-$VERSION.zip"
    echo "   Package size: $(du -h "$OUTPUT_DIR/password-manager-extension-$VERSION.zip" | cut -f1)"
    echo ""
else
    echo "  ✗ Error: Failed to create ZIP package"
    exit 1
fi

echo "📝 Next Steps:"
echo ""
echo "For Chrome Web Store:"
echo "  1. Go to: https://chrome.google.com/webstore/devconsole"
echo "  2. Upload: password-manager-extension-$VERSION.zip"
echo "  3. Fill in store listing details"
echo "  4. Submit for review"
echo ""
echo "For Microsoft Edge Add-ons:"
echo "  1. Go to: https://partner.microsoft.com/dashboard/microsoftedge/overview"
echo "  2. Upload: password-manager-extension-$VERSION.zip"
echo "  3. Fill in store listing details"
echo "  4. Submit for review"
echo ""
echo "For Private Distribution (.crx):"
echo "  Note: Chrome no longer supports installing .crx files directly"
echo "  Users must either:"
echo "  - Install from Chrome Web Store"
echo "  - Use Developer Mode and load unpacked extension"
echo "  - Use Enterprise Policy for force-installed extensions"
echo ""
echo "For Development/Testing:"
echo "  1. Open chrome://extensions/ or edge://extensions/"
echo "  2. Enable 'Developer mode'"
echo "  3. Click 'Load unpacked'"
echo "  4. Select this directory: $SCRIPT_DIR"
echo ""
