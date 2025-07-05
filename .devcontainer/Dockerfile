# Use .NET 9 SDK preview image
FROM mcr.microsoft.com/dotnet/sdk:9.0

# Install prerequisites for MAUI (Java JDK, unzip, wget, etc.)
RUN apt-get update && apt-get install -y \
    openjdk-11-jdk \
    unzip \
    wget \
    apt-transport-https \
    ca-certificates \
    gnupg \
    && rm -rf /var/lib/apt/lists/*

# Set JAVA_HOME environment variable
ENV JAVA_HOME=/usr/lib/jvm/java-11-openjdk-amd64/
ENV PATH="$JAVA_HOME/bin:$PATH"

# Install MAUI workloads
RUN dotnet workload install maui

# Optional: Verify installed workloads
RUN dotnet workload list
