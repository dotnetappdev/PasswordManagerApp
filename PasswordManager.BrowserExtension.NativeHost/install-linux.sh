#!/bin/bash

echo "Password Manager Native Host Installer"
echo "====================================="
echo

# Default installation directory
INSTALL_DIR="$HOME/.local/bin/passwordmanager-native-host"
MANIFEST_DIR="$HOME/.config/google-chrome/NativeMessagingHosts"

echo "Installing Password Manager Native Host..."
echo "Installation directory: $INSTALL_DIR"
echo "Manifest directory: $MANIFEST_DIR"
echo

# Create installation directory
mkdir -p "$INSTALL_DIR"
if [ $? -ne 0 ]; then
    echo "Failed to create installation directory"
    exit 1
fi

# Create manifest directory
mkdir -p "$MANIFEST_DIR"
if [ $? -ne 0 ]; then
    echo "Failed to create manifest directory"
    exit 1
fi

# Build the native host
echo "Building native messaging host..."
dotnet publish -c Release -r linux-x64 --self-contained true --single-file -o "$INSTALL_DIR"
if [ $? -ne 0 ]; then
    echo "Failed to build native messaging host"
    exit 1
fi

# Make the executable... executable
chmod +x "$INSTALL_DIR/PasswordManager.BrowserExtension.NativeHost"

# Create the manifest file with correct paths
MANIFEST_FILE="$MANIFEST_DIR/com.passwordmanager.native_host.json"
EXECUTABLE_PATH="$INSTALL_DIR/PasswordManager.BrowserExtension.NativeHost"

cat > "$MANIFEST_FILE" << EOF
{
  "name": "com.passwordmanager.native_host",
  "description": "Password Manager Native Messaging Host",
  "path": "$EXECUTABLE_PATH",
  "type": "stdio",
  "allowed_origins": [
    "chrome-extension://EXTENSION_ID_PLACEHOLDER/"
  ]
}
EOF

echo
echo "Native host installed successfully!"
echo
echo "Next steps:"
echo "1. Install your browser extension and note its Extension ID"
echo "2. Edit the manifest file to replace EXTENSION_ID_PLACEHOLDER:"
echo "   $MANIFEST_FILE"
echo "3. The native messaging host is now registered for Chrome"
echo
echo "For Firefox, copy the manifest to:"
echo "   ~/.mozilla/native-messaging-hosts/com.passwordmanager.native_host.json"
echo
echo "For Chromium, copy the manifest to:"
echo "   ~/.config/chromium/NativeMessagingHosts/com.passwordmanager.native_host.json"
echo