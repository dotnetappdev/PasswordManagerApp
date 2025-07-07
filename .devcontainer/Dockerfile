# Use .NET 9 SDK image
FROM mcr.microsoft.com/dotnet/sdk:9.0

# Install system prerequisites
RUN apt-get update && apt-get install -y \
    # Java JDK for Android development
    openjdk-17-jdk \
    # Android SDK dependencies
    unzip \
    wget \
    curl \
    git \
    # HTTPS and certificate handling
    apt-transport-https \
    ca-certificates \
    gnupg \
    lsb-release \
    # Additional tools for development
    procps \
    file \
    && rm -rf /var/lib/apt/lists/*

# Set JAVA_HOME environment variable (JDK 17 for better compatibility)
ENV JAVA_HOME=/usr/lib/jvm/java-17-openjdk-amd64/
ENV PATH="$JAVA_HOME/bin:$PATH"

# Set Android environment variables
ENV ANDROID_HOME=/opt/android-sdk
ENV ANDROID_SDK_ROOT=/opt/android-sdk
ENV PATH="$PATH:$ANDROID_HOME/cmdline-tools/latest/bin:$ANDROID_HOME/platform-tools"

# Create Android SDK directory
RUN mkdir -p $ANDROID_HOME

# Download and install Android Command Line Tools
RUN wget -q https://dl.google.com/android/repository/commandlinetools-linux-11076708_latest.zip -O /tmp/cmdtools.zip \
    && unzip -q /tmp/cmdtools.zip -d $ANDROID_HOME \
    && mv $ANDROID_HOME/cmdline-tools $ANDROID_HOME/cmdline-tools-temp \
    && mkdir -p $ANDROID_HOME/cmdline-tools/latest \
    && mv $ANDROID_HOME/cmdline-tools-temp/* $ANDROID_HOME/cmdline-tools/latest/ \
    && rm -rf $ANDROID_HOME/cmdline-tools-temp /tmp/cmdtools.zip

# Accept Android SDK licenses and install required packages
RUN yes | sdkmanager --licenses > /dev/null 2>&1 || true \
    && sdkmanager "platform-tools" "platforms;android-34" "build-tools;34.0.0" \
    && sdkmanager "platforms;android-33" "build-tools;33.0.2"

# Install .NET workloads (MAUI and related)
RUN dotnet workload update \
    && dotnet workload install maui \
    && dotnet workload install android \
    && dotnet workload install ios \
    && dotnet workload install maccatalyst \
    && dotnet workload install macos \
    && dotnet workload install blazor

# Set correct permissions for Android SDK
RUN chmod -R 755 $ANDROID_HOME

# Create a non-root user for development
RUN useradd -m -s /bin/bash vscode \
    && usermod -aG sudo vscode \
    && echo "vscode ALL=(ALL) NOPASSWD:ALL" >> /etc/sudoers

# Change ownership of Android SDK to vscode user
RUN chown -R vscode:vscode $ANDROID_HOME

# Switch to vscode user
USER vscode

# Verify installations
RUN dotnet --version \
    && dotnet workload list \
    && java -version
