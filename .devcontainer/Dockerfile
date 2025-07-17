# Dev Container Dockerfile for Password Manager with .NET 9 and MAUI
# This is optimized for GitHub Codespaces and excludes iOS/macOS support

FROM mcr.microsoft.com/devcontainers/dotnet:1-9.0-bookworm

# Install additional dependencies for MAUI Android development
RUN apt-get update && apt-get install -y \
    wget \
    unzip \
    openjdk-17-jdk \
    gradle \
    build-essential \
    && rm -rf /var/lib/apt/lists/*

# Set JAVA_HOME
ENV JAVA_HOME=/usr/lib/jvm/java-17-openjdk-amd64

# Install Android SDK for MAUI development
ENV ANDROID_SDK_ROOT=/opt/android-sdk
ENV PATH="${PATH}:${ANDROID_SDK_ROOT}/cmdline-tools/latest/bin:${ANDROID_SDK_ROOT}/platform-tools"

# Create android-sdk directory and set permissions
RUN mkdir -p ${ANDROID_SDK_ROOT} && \
    chown -R vscode:vscode ${ANDROID_SDK_ROOT}

# Switch to vscode user for Android SDK installation
USER vscode

# Download and install Android SDK Command Line Tools
RUN mkdir -p ${ANDROID_SDK_ROOT}/cmdline-tools && \
    cd ${ANDROID_SDK_ROOT}/cmdline-tools && \
    wget -q https://dl.google.com/android/repository/commandlinetools-linux-9477386_latest.zip && \
    unzip -q commandlinetools-linux-9477386_latest.zip && \
    mv cmdline-tools latest && \
    rm commandlinetools-linux-9477386_latest.zip

# Accept Android SDK licenses and install required packages
RUN yes | sdkmanager --licenses && \
    sdkmanager "platform-tools" "platforms;android-34" "build-tools;34.0.0"

# Switch back to root for final setup
USER root

# Install .NET MAUI workloads (Android only)

# Set environment variables to exclude iOS/macOS

# Create a script to set up the development environment
RUN echo '#!/bin/bash\n\
echo "=== Password Manager Development Container ==="\n\
echo "✅ .NET 9 SDK installed"\n\
echo "✅ MAUI Android workload installed"\n\
echo "✅ Android SDK configured"\n\
echo "❌ iOS/macOS builds excluded (not supported in Codespaces)"\n\
echo ""\n\
echo "Available commands:"\n\
echo "  dotnet build                    - Build the entire solution"\n\
echo "  dotnet run --project PasswordManager.API     - Run the API"\n\
echo "  dotnet run --project PasswordManager.Web     - Run the Web app"\n\
echo "  ./docker-run.sh build-all      - Build Docker containers"\n\
echo "  ./docker-run.sh run-all         - Run all services"\n\
echo ""\n\
echo "Ports:"\n\
echo "  5000/5001 - API (HTTP/HTTPS)"\n\
echo "  5002/5003 - Web (HTTP/HTTPS)"\n\
echo "  5432      - PostgreSQL"\n\
echo "  3306      - MySQL"\n\
echo "  1433      - SQL Server"\n\
echo ""\n\
' > /usr/local/bin/dev-info && chmod +x /usr/local/bin/dev-info

# Set the default user back to vscode
USER vscode

USER root

# Create a script to set up the development environment
RUN echo '#!/bin/bash\n\
echo "=== Password Manager Development Container ==="\n\
echo "✅ .NET 9 SDK installed"\n\
echo "❌ MAUI Android workload not installed by default"\n\
echo "✅ Android SDK configured"\n\
echo "❌ iOS/macOS builds excluded (not supported in Codespaces)"\n\
echo ""\n\
echo "Available commands:"\n\
echo "  dotnet build                    - Build the entire solution"\n\
echo "  dotnet run --project PasswordManager.API     - Run the API"\n\
echo "  dotnet run --project PasswordManager.Web     - Run the Web app"\n\
echo "  ./docker-run.sh build-all      - Build Docker containers"\n\
echo "  ./docker-run.sh run-all         - Run all services"\n\
echo "  ./install-maui-workload.sh      - Install MAUI workload for .App project"\n\
echo ""\n\
echo "Ports:"\n\
echo "  5000/5001 - API (HTTP/HTTPS)"\n\
echo "  5002/5003 - Web (HTTP/HTTPS)"\n\
echo "  5432      - PostgreSQL"\n\
echo "  3306      - MySQL"\n\
echo "  1433      - SQL Server"\n\
echo ""\n\
' > /usr/local/bin/dev-info && chmod +x /usr/local/bin/dev-info

USER vscode
WORKDIR /workspaces/PasswordManagerApp

# Set the working directory
WORKDIR /workspaces/PasswordManagerApp
