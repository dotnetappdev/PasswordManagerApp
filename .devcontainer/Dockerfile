# Use the official .NET 9.0.6 SDK image
ARG DOTNET_VERSION=9.0.6
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}

# Install basic tools
RUN apt-get update && export DEBIAN_FRONTEND=noninteractive \
    && apt-get install -y \
        curl \
        git \
        unzip \
        sudo \
        bash \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Create a non-root user for Codespaces
ARG USERNAME=codespace
ARG USER_UID=1000
ARG USER_GID=1000

RUN groupadd --gid $USER_GID $USERNAME \
    && useradd --uid $USER_UID --gid $USER_GID -m $USERNAME \
    && echo "$USERNAME ALL=(ALL) NOPASSWD:ALL" > /etc/sudoers.d/$USERNAME \
    && chmod 0440 /etc/sudoers.d/$USERNAME

USER $USERNAME
