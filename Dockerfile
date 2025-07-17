# Multi-stage Dockerfile for Password Manager Application with .NET 9 and MAUI support
# This dockerfile supports building the API, Web, and Android MAUI components
# iOS and macOS builds are not supported on Linux containers

# Use the official .NET 9 SDK image with MAUI workloads
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Build arguments
ARG BUILD_CONFIGURATION=Release
ARG MAUI_TARGET_FRAMEWORK=net9.0-android

# Install MAUI workloads (required for MAUI projects)
RUN dotnet workload install maui android

# Install Android SDK (required for MAUI Android builds)
RUN apt-get update && apt-get install -y \
    wget \
    unzip \
    openjdk-17-jdk \
    && rm -rf /var/lib/apt/lists/*

# Set JAVA_HOME
ENV JAVA_HOME=/usr/lib/jvm/java-17-openjdk-amd64

# Install Android SDK
ENV ANDROID_SDK_ROOT=/opt/android-sdk
ENV PATH="${PATH}:${ANDROID_SDK_ROOT}/cmdline-tools/latest/bin:${ANDROID_SDK_ROOT}/platform-tools"

RUN mkdir -p ${ANDROID_SDK_ROOT}/cmdline-tools && \
    cd ${ANDROID_SDK_ROOT}/cmdline-tools && \
    wget https://dl.google.com/android/repository/commandlinetools-linux-9477386_latest.zip && \
    unzip commandlinetools-linux-9477386_latest.zip && \
    mv cmdline-tools latest && \
    rm commandlinetools-linux-9477386_latest.zip

# Accept Android SDK licenses
RUN yes | sdkmanager --licenses

# Copy solution file
COPY PasswordManager.sln .

# Copy project files for dependency resolution
COPY PasswordManager.API/*.csproj ./PasswordManager.API/
COPY PasswordManager.App/*.csproj ./PasswordManager.App/
COPY PasswordManager.Web/*.csproj ./PasswordManager.Web/
COPY PasswordManager.Components.Shared/*.csproj ./PasswordManager.Components.Shared/
COPY PasswordManager.Crypto/*.csproj ./PasswordManager.Crypto/
COPY PasswordManager.DAL/*.csproj ./PasswordManager.DAL/
COPY PasswordManager.DAL.MySql/*.csproj ./PasswordManager.DAL.MySql/
COPY PasswordManager.DAL.Postgres/*.csproj ./PasswordManager.DAL.Postgres/
COPY PasswordManager.DAL.SqlServer/*.csproj ./PasswordManager.DAL.SqlServer/
COPY PasswordManager.DAL.SupaBase/*.csproj ./PasswordManager.DAL.SupaBase/
COPY PasswordManager.Imports/*.csproj ./PasswordManager.Imports/
COPY PasswordManager.Models/*.csproj ./PasswordManager.Models/
COPY PasswordManager.Services/*.csproj ./PasswordManager.Services/
COPY PasswordManagerImports.1Password/*.csproj ./PasswordManagerImports.1Password/
COPY PasswordManagerImports.Bitwarden/*.csproj ./PasswordManagerImports.Bitwarden/

# Restore packages
RUN dotnet restore

# Copy the rest of the source code
COPY . .

# Build stage for API
FROM build AS build-api
RUN dotnet build PasswordManager.API/PasswordManager.API.csproj -c ${BUILD_CONFIGURATION} --no-restore
RUN dotnet publish PasswordManager.API/PasswordManager.API.csproj -c ${BUILD_CONFIGURATION} -o /app/publish/api --no-restore

# Build stage for Web
FROM build AS build-web
RUN dotnet build PasswordManager.Web/PasswordManager.Web.csproj -c ${BUILD_CONFIGURATION} --no-restore
RUN dotnet publish PasswordManager.Web/PasswordManager.Web.csproj -c ${BUILD_CONFIGURATION} -o /app/publish/web --no-restore

# Build stage for MAUI (Android only - iOS/macOS builds not supported on Linux)
FROM build AS build-maui-android
# Set environment to skip iOS/macOS builds
ENV EXCLUDE_IOS_MACOS=true
# Build only Android target
RUN dotnet build PasswordManager.App/PasswordManager.App.csproj -c ${BUILD_CONFIGURATION} -f ${MAUI_TARGET_FRAMEWORK} --no-restore
RUN dotnet publish PasswordManager.App/PasswordManager.App.csproj -c ${BUILD_CONFIGURATION} -f ${MAUI_TARGET_FRAMEWORK} -o /app/publish/maui-android --no-restore

# Runtime stage for API
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime-api
WORKDIR /app
COPY --from=build-api /app/publish/api .
EXPOSE 80
EXPOSE 443
ENTRYPOINT ["dotnet", "PasswordManager.API.dll"]

# Runtime stage for Web
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime-web
WORKDIR /app
COPY --from=build-web /app/publish/web .
EXPOSE 80
EXPOSE 443
ENTRYPOINT ["dotnet", "PasswordManager.Web.dll"]

# Default stage - builds all projects (excluding iOS/macOS MAUI targets)
FROM build AS default
RUN dotnet build -c ${BUILD_CONFIGURATION} --no-restore
