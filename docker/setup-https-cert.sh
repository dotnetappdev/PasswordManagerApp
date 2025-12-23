#!/bin/bash
# Setup script for generating HTTPS development certificates for Docker

set -e

echo "================================================"
echo "Password Manager - Docker HTTPS Certificate Setup"
echo "================================================"
echo ""

# Create certs directory if it doesn't exist
CERTS_DIR="$(dirname "$0")/certs"
mkdir -p "$CERTS_DIR"

echo "Creating HTTPS development certificate..."
echo ""

# Check if dotnet is installed
if ! command -v dotnet &> /dev/null; then
    echo "Error: .NET SDK is not installed or not in PATH"
    echo "Please install .NET 9 SDK from https://dotnet.microsoft.com/download"
    exit 1
fi

# Check if certificate already exists
if [ -f "$CERTS_DIR/aspnetapp.pfx" ]; then
    echo "Certificate already exists at: $CERTS_DIR/aspnetapp.pfx"
    read -p "Do you want to regenerate it? (y/N): " -n 1 -r
    echo ""
    if [[ ! $ASPNETCORE_Kestrel__Certificates__Default__Password =~ ^[Yy]$ ]]; then
        echo "Using existing certificate."
        exit 0
    fi
    rm -f "$CERTS_DIR/aspnetapp.pfx"
fi

# Clean existing dev certs
echo "Cleaning existing development certificates..."
dotnet dev-certs https --clean

# Create new dev certificate
echo "Generating new development certificate..."
dotnet dev-certs https -ep "$CERTS_DIR/aspnetapp.pfx" -p ""

# Trust the certificate (optional, for local development)
read -p "Do you want to trust this certificate on your local machine? (y/N): " -n 1 -r
echo ""
if [[ $REPLY =~ ^[Yy]$ ]]; then
    echo "Trusting certificate..."
    dotnet dev-certs https --trust
fi

# Set appropriate permissions
chmod 644 "$CERTS_DIR/aspnetapp.pfx"

echo ""
echo "✅ Certificate setup complete!"
echo ""
echo "Certificate location: $CERTS_DIR/aspnetapp.pfx"
echo "Certificate password: (empty - no password)"
echo ""
echo "You can now start the Docker containers with:"
echo "  cd docker"
echo "  docker-compose up -d"
echo ""
