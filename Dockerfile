# Multi-stage Dockerfile for Password Manager Application with .NET 9 and MAUI support
# This dockerfile supports building the API, Web, and Android MAUI components
# iOS and macOS builds are not supported on Linux containers

# Use the official .NET 9 SDK image with MAUI workloads
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Build arguments
ARG BUILD_CONFIGURATION=Release
ARG MAUI_TARGET_FRAMEWORK=net9.0-android

# Note: MAUI workloads are not supported on Linux containers
# MAUI projects must be built on Windows or macOS with appropriate SDKs
# This Dockerfile focuses on API and Web projects that can run on Linux

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

# Note: MAUI Android builds are not supported in Linux containers
# For MAUI development, use the dev container or build on Windows/macOS

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
