# WinUI MSIX Packaging Guide

This guide explains how to create and distribute MSIX packages for the Password Manager WinUI application.

## 📋 Table of Contents

- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Creating MSIX Packages](#creating-msix-packages)
- [Signing Packages](#signing-packages)
- [Testing Packages](#testing-packages)
- [Distribution](#distribution)
- [Version Management](#version-management)
- [Troubleshooting](#troubleshooting)

## 🔧 Overview

MSIX is the modern Windows app package format that provides:

- ✅ Clean installation and uninstallation
- ✅ Automatic updates
- ✅ Sandboxed execution
- ✅ Microsoft Store compatibility
- ✅ Enterprise deployment support

## 🎯 Prerequisites

1. **Windows 10 SDK** (10.0.19041.0 or later)
   - Included with Visual Studio
   - Or download: [Windows SDK](https://developer.microsoft.com/windows/downloads/windows-sdk/)

2. **Visual Studio 2024** with the following workloads:
   - .NET desktop development
   - Universal Windows Platform development

3. **Code Signing Certificate** (for production)
   - Self-signed certificate (for testing)
   - Or purchased certificate from a trusted CA

## 📦 Creating MSIX Packages

### Method 1: Visual Studio

1. **Open the solution in Visual Studio**
   ```
   PasswordManager.sln
   ```

2. **Set configuration to Release**
   - Select **Release** configuration
   - Select **x64** platform

3. **Configure MSIX packaging**
   - Right-click **PasswordManager.WinUi** project
   - Select **Publish → Create App Packages**

4. **Choose distribution method**
   - **Microsoft Store**: For Store submission
   - **Sideloading**: For enterprise/manual distribution

5. **Select certificate**
   - Use existing certificate
   - Or create new self-signed certificate

6. **Configure package settings**
   - **Version**: Update version number (e.g., 1.1.0.0)
   - **Architecture**: x64, x86, ARM64 (choose one or all)
   - **Package name**: PasswordManager.WinUi

7. **Generate package**
   - Click **Create**
   - Packages will be created in:
     ```
     PasswordManager.WinUi\AppPackages\
     ```

### Method 2: Command Line

#### Create MSIX Package

```cmd
cd PasswordManager.WinUi

REM Build and package for x64
dotnet publish -c Release -f net9.0-windows10.0.19041.0 -p:Platform=x64 -p:GenerateAppxPackageOnBuild=true -p:AppxPackageSigningEnabled=false

REM Build and package for ARM64
dotnet publish -c Release -f net9.0-windows10.0.19041.0 -p:Platform=ARM64 -p:GenerateAppxPackageOnBuild=true -p:AppxPackageSigningEnabled=false
```

#### Package Location

Packages are created at:
```
PasswordManager.WinUi\bin\{Platform}\Release\net9.0-windows10.0.19041.0\{Platform}\AppPackages\
```

### Method 3: MSBuild

```cmd
msbuild PasswordManager.WinUi.csproj /p:Configuration=Release /p:Platform=x64 /p:AppxBundle=Always /p:GenerateAppxPackageOnBuild=true
```

## 🔐 Signing Packages

### Create Self-Signed Certificate (Testing)

```powershell
# Create certificate
New-SelfSignedCertificate -Type Custom -Subject "CN=PasswordManager" -KeyUsage DigitalSignature -FriendlyName "Password Manager Certificate" -CertStoreLocation "Cert:\CurrentUser\My" -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")

# Export certificate
$pwd = ConvertTo-SecureString -String "YourPassword" -Force -AsPlainText
Export-PfxCertificate -Cert "Cert:\CurrentUser\My\<thumbprint>" -FilePath "PasswordManager.pfx" -Password $pwd

# Export public key for installation
Export-Certificate -Cert "Cert:\CurrentUser\My\<thumbprint>" -FilePath "PasswordManager.cer"
```

### Sign Package with Certificate

```cmd
REM Using SignTool
"C:\Program Files (x86)\Windows Kits\10\bin\10.0.19041.0\x64\signtool.exe" sign /fd SHA256 /a /f PasswordManager.pfx /p YourPassword "PasswordManager.WinUi_1.1.0.0_x64.msix"
```

### Configure in Project

Edit `PasswordManager.WinUi.csproj`:

```xml
<PropertyGroup>
  <AppxPackageSigningEnabled>true</AppxPackageSigningEnabled>
  <PackageCertificateKeyFile>PasswordManager.pfx</PackageCertificateKeyFile>
  <PackageCertificatePassword>YourPassword</PackageCertificatePassword>
</PropertyGroup>
```

## 🧪 Testing Packages

### Install Certificate (First Time)

Before installing the MSIX package, install the certificate:

```powershell
# Install certificate to Trusted Root
Import-Certificate -FilePath "PasswordManager.cer" -CertStoreLocation "Cert:\LocalMachine\Root"
```

Or double-click `PasswordManager.cer` and follow the wizard.

### Install MSIX Package

#### Method 1: Double-Click

1. Navigate to the package location
2. Double-click the `.msix` file
3. Click **Install**

#### Method 2: PowerShell

```powershell
Add-AppxPackage -Path "PasswordManager.WinUi_1.1.0.0_x64.msix"
```

#### Method 3: App Installer

```powershell
# Install via App Installer
Start-Process "ms-appinstaller:?source=C:\path\to\PasswordManager.WinUi_1.1.0.0_x64.msix"
```

### Verify Installation

```powershell
# List installed packages
Get-AppxPackage | Where-Object {$_.Name -like "*PasswordManager*"}

# Get package details
Get-AppxPackage -Name "PasswordManager.WinUi"
```

### Uninstall Package

```powershell
# Remove package
Remove-AppxPackage -Package "PasswordManager.WinUi_1.1.0.0_x64__<publisherid>"

# Or uninstall via Settings → Apps
```

## 🌐 Distribution

### Option 1: Microsoft Store

1. **Create Microsoft Store account**
   - Go to [Partner Center](https://partner.microsoft.com/dashboard)
   - Enroll in the Microsoft Store program

2. **Create app submission**
   - Create new app
   - Upload MSIX package
   - Fill out app details
   - Submit for certification

3. **Benefits:**
   - Automatic updates
   - Trusted distribution
   - Easy discovery
   - Microsoft handles hosting

### Option 2: Enterprise Distribution

**Via Microsoft Intune:**
```powershell
# Add line-of-business app
# Upload MSIX to Intune
# Assign to user groups
```

**Via SCCM:**
- Create application in SCCM
- Add MSIX as deployment type
- Deploy to collections

### Option 3: Web Distribution

Create an `.appinstaller` file:

```xml
<?xml version="1.0" encoding="utf-8"?>
<AppInstaller Uri="https://yoursite.com/PasswordManager.appinstaller" Version="1.1.0.0" xmlns="http://schemas.microsoft.com/appx/appinstaller/2021">
  <MainPackage Name="PasswordManager.WinUi" Publisher="CN=PasswordManager" Version="1.1.0.0" Uri="https://yoursite.com/PasswordManager.WinUi_1.1.0.0_x64.msix" ProcessorArchitecture="x64"/>
  <UpdateSettings>
    <OnLaunch HoursBetweenUpdateChecks="24" />
  </UpdateSettings>
</AppInstaller>
```

Host the `.appinstaller` and `.msix` files on your web server.

Users can install via:
```
https://yoursite.com/PasswordManager.appinstaller
```

### Option 4: Direct Download

Provide direct download link to `.msix` file:
- Users download the file
- Users install certificate (if not from trusted CA)
- Users double-click to install

## 📌 Version Management

### Update Version Number

1. **Edit Package.appxmanifest:**
   ```xml
   <Identity Name="PasswordManager.WinUi"
             Publisher="CN=PasswordManager"
             Version="1.1.0.0" />
   ```

2. **Version format:** Major.Minor.Build.Revision
   - Major: Breaking changes
   - Minor: New features
   - Build: Bug fixes
   - Revision: Hotfixes

### Version Guidelines

- **Update Major**: Major UI changes, data format changes
- **Update Minor**: New features, significant improvements
- **Update Build**: Bug fixes, minor improvements
- **Update Revision**: Critical hotfixes

### Automatic Version Increment

Add to `PasswordManager.WinUi.csproj`:

```xml
<PropertyGroup>
  <ApplicationVersion>$([System.DateTime]::Now.ToString('yyyy.MM.dd.HHmm'))</ApplicationVersion>
</PropertyGroup>
```

## 🔧 Advanced Configuration

### App Capabilities

Edit `Package.appxmanifest` to add capabilities:

```xml
<Capabilities>
  <rescap:Capability Name="runFullTrust" />
  <Capability Name="internetClient" />
  <Capability Name="privateNetworkClientServer" />
</Capabilities>
```

### Bundle Multiple Architectures

```cmd
REM Create bundle with multiple architectures
makeappx bundle /d "AppPackages" /p "PasswordManager_1.1.0.0.msixbundle"
```

### Optimize Package Size

1. **Use Release configuration**
2. **Enable trimming** (if applicable)
3. **Remove unnecessary files**
4. **Compress assets**

```xml
<PropertyGroup>
  <PublishTrimmed>true</PublishTrimmed>
  <TrimMode>link</TrimMode>
</PropertyGroup>
```

## 🔍 Troubleshooting

### Package Installation Fails

**Check certificate:**
```powershell
# Verify certificate is installed
Get-ChildItem Cert:\LocalMachine\Root | Where-Object {$_.Subject -like "*PasswordManager*"}
```

**Enable sideloading:**
```powershell
# Enable developer mode
Start-Process ms-settings:developers
```

### App Won't Start

**View event logs:**
```
Event Viewer → Applications and Services Logs → Microsoft → Windows → AppxPackaging/Operational
```

**Check dependencies:**
```powershell
# Verify .NET runtime
Get-AppxPackage | Where-Object {$_.Name -like "*Microsoft.WindowsAppRuntime*"}
```

### Signing Errors

**Trust certificate:**
```cmd
certutil -addstore "TrustedPeople" PasswordManager.cer
```

**Verify signature:**
```cmd
signtool verify /pa "PasswordManager.WinUi_1.1.0.0_x64.msix"
```

### Update Fails

**Clear cache:**
```powershell
# Remove old package
Remove-AppxPackage -Package "PasswordManager.WinUi_1.0.0.0_x64__<publisherid>"

# Install new package
Add-AppxPackage -Path "PasswordManager.WinUi_1.1.0.0_x64.msix"
```

## 📚 Additional Resources

- [MSIX Documentation](https://docs.microsoft.com/windows/msix/)
- [App Packaging Guide](https://docs.microsoft.com/windows/msix/package/packaging-uwp-apps)
- [Microsoft Store Policies](https://docs.microsoft.com/windows/apps/publish/store-policies)
- [WinUI Documentation](https://docs.microsoft.com/windows/apps/winui/)

## 🆘 Support

For issues with MSIX packaging:

1. Check the [Troubleshooting](#troubleshooting) section
2. Review [Microsoft MSIX Documentation](https://docs.microsoft.com/windows/msix/)
3. Check [GitHub Issues](https://github.com/dotnetappdev/PasswordManagerApp/issues)
4. Create a new issue with:
   - Windows version
   - Package manifest
   - Error logs
   - Steps to reproduce

---

**Note:** MSIX packaging requires Windows 10 version 1809 or later. For older Windows versions, use the Inno Setup installer instead.
