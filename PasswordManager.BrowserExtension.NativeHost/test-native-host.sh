#!/bin/bash

echo "Testing Password Manager Native Host"
echo "==================================="
echo

# Build the native host first
echo "Building native host..."
cd "$(dirname "$0")"
dotnet build --no-restore
if [ $? -ne 0 ]; then
    echo "Build failed"
    exit 1
fi

echo "Build successful"
echo
echo "Testing connection..."

# Create a test message
TEST_MESSAGE='{"action":"testConnection"}'
MESSAGE_LENGTH=$(echo -n "$TEST_MESSAGE" | wc -c)

# Create the native messaging format (4-byte length + message)
printf "%04x" $MESSAGE_LENGTH | xxd -r -p > /tmp/test_message.bin
echo -n "$TEST_MESSAGE" >> /tmp/test_message.bin

# Run the native host with the test message
echo "Sending test message to native host..."
./bin/Debug/net8.0/PasswordManager.BrowserExtension.NativeHost < /tmp/test_message.bin

# Clean up
rm -f /tmp/test_message.bin

echo
echo "Test completed"