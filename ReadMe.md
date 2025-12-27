# Password Manager

[![API Build](https://github.com/dotnetappdev/PasswordManagerApp/workflows/API%20Build/badge.svg)](https://github.com/dotnetappdev/PasswordManagerApp/actions/workflows/build-api.yml)
[![Web App Build](https://github.com/dotnetappdev/PasswordManagerApp/workflows/Web%20App%20Build/badge.svg)](https://github.com/dotnetappdev/PasswordManagerApp/actions/workflows/build-web.yml)
[![Unit Tests](https://github.com/dotnetappdev/PasswordManagerApp/workflows/Unit%20Tests/badge.svg)](https://github.com/dotnetappdev/PasswordManagerApp/actions/workflows/run-tests.yml)
[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![MAUI](https://img.shields.io/badge/MAUI-Cross--Platform-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/apps/maui)
[![Web API](https://img.shields.io/badge/Web%20API-ASP.NET%20Core-512BD4?style=for-the-badge&logo=dotnet)](https://docs.microsoft.com/aspnet/core/web-api/)
[![Open Source](https://img.shields.io/badge/Open%20Source-MIT-green?style=for-the-badge)](LICENSE)
[![Blazor](https://img.shields.io/badge/Blazor-Web%20UI-512BD4?style=for-the-badge&logo=blazor)](https://blazor.net/)

## Quick Start

- **[Setup Guide](SETUP.md)** - Complete setup and configuration guide
- **[Getting Started](GETTING_STARTED.md)** - Installation and setup guide
- **[User Guide](USER_GUIDE.md)** - Complete user documentation
- **[Development Guide](DEVELOPMENT.md)** - Developer setup and contributing
- **[UI Design System](UI_DESIGN_SYSTEM.md)** - Consistent styling guide for all platforms

A modern, secure, and cross-platform password manager built with .NET 9, .NET MAUI, Blazor Server, and ASP.NET Core Web API. Keep your passwords, credit cards, secure notes, and WiFi credentials safe and organized across all your devices with cloud synchronization capabilities.

## 🚀 Deployment Options

### Docker Deployment
Run the entire stack with Docker for easy deployment and scalability:
- **[Docker Setup Guide](docker/README.md)** - Complete Docker deployment guide
- Pre-configured with SQL Server 2022 Developer Edition
- HTTPS certificate support with automated setup
- Health checks and automatic restart policies
- One-command deployment: `docker-compose up -d`

### Windows Installers
Professional installers for Windows using open-source Inno Setup:
- **[Installer Documentation](installers/README.md)** - Complete installer guide
- **Web API Installer** - Windows service with database configuration wizard
- **WinUI App Installer** - Desktop application with API setup wizard
- Automated certificate generation and firewall configuration
- Interactive database connection setup

### MSIX Packages
Modern Windows app packages for Microsoft Store and enterprise distribution:
- **[MSIX Packaging Guide](installers/MSIX_PACKAGING_GUIDE.md)** - Complete MSIX guide
- Clean installation and uninstallation
- Automatic updates support
- Microsoft Store ready
- Enterprise deployment compatible

## Documentation

### Project Components
| Component | Description | Documentation |
|-----------|-------------|---------------|
| **Blazor Web App** | Web application with theme support | [PasswordManager.Web](PasswordManager.Web/README.md) |
| **WinUI Desktop App** | Native Windows application | [PasswordManager.WinUi](PasswordManager.WinUi/README.md) |
| **Uno Platform Mobile App** | Cross-platform iOS, Android, and WebAssembly app | [UNO_PLATFORM_GUIDE.md](UNO_PLATFORM_GUIDE.md) |
| **Browser Extension** | Chrome/Edge/Firefox extension with multiple connection modes | [README](PasswordManager.BrowserExtension/README.md) · [Installation](PasswordManager.BrowserExtension/INSTALLATION.md) · [Packaging](PasswordManager.BrowserExtension/PACKAGING.md) |
| **Shared Components** | Cross-platform UI components | [PasswordManager.Components.Shared](PasswordManager.Components.Shared/README.md) |
| **Cryptography** | Security and encryption details | [PasswordManager.Crypto](PasswordManager.Crypto/README.md) |

### Database Providers
| Provider | Description | Documentation |
|----------|-------------|---------------|
| **SQL Server** | Microsoft SQL Server support | [PasswordManager.DAL.SqlServer](PasswordManager.DAL.SqlServer/README.md) |
| **MySQL** | MySQL database support | [PasswordManager.DAL.MySql](PasswordManager.DAL.MySql/README.md) |
| **PostgreSQL** | PostgreSQL database support | [PasswordManager.DAL.Postgres](PasswordManager.DAL.Postgres/README.md) |
| **Supabase** | Supabase cloud database support | [PasswordManager.DAL.SupaBase](PasswordManager.DAL.SupaBase/README.md) |

### Additional Documentation
| Document | Description |
|----------|-------------|
| **Docker Setup** | Complete Docker deployment guide with SQL Server | [docker/README.md](docker/README.md) |
| **Installers Guide** | Windows installers for Web API and WinUI app | [installers/README.md](installers/README.md) |
| **MSIX Packaging** | Modern Windows app packaging guide | [installers/MSIX_PACKAGING_GUIDE.md](installers/MSIX_PACKAGING_GUIDE.md) |
| **Uno Platform Guide** | Complete guide for mobile iOS/Android app development | [UNO_PLATFORM_GUIDE.md](UNO_PLATFORM_GUIDE.md) |
| **Mobile Screenshots Guide** | Instructions for capturing iOS and Android screenshots | [MOBILE_SCREENSHOTS.md](MOBILE_SCREENSHOTS.md) |
| **Entity Framework Identity Setup** | Complete guide for EF Identity and migration management | [EF_IDENTITY_SETUP_GUIDE.md](EF_IDENTITY_SETUP_GUIDE.md) |
| **WinUI Screenshots** | Visual interface documentation for Windows app | [screenshots.md](screenshots.md) |
| **Complete Setup Guide** | Full setup and configuration guide | [SETUP.md](SETUP.md) |
| **Browser Extension Setup** | Browser extension installation guide | [INSTALLATION.md](INSTALLATION.md) |
| **Database Setup** | Database configuration guide | [ReadMe.DatabaseProviders.md](ReadMe.DatabaseProviders.md) |
| **MySQL Setup** | MySQL-specific setup guide | [MYSQL_SETUP_GUIDE.md](MYSQL_SETUP_GUIDE.md) |
| **OTP Authentication** | SMS-based 2FA implementation guide | [OTP_IMPLEMENTATION_GUIDE.md](OTP_IMPLEMENTATION_GUIDE.md) |
| **Technology Stack** | Complete technology overview | [TECHNOLOGY_STACK.md](TECHNOLOGY_STACK.md) |
| **Encryption Details** | Technical encryption implementation | [ENCRYPTION_IMPLEMENTATION.md](ENCRYPTION_IMPLEMENTATION.md) |
| **Bitwarden Flow** | Bitwarden compatibility details | [BITWARDEN_FLOW_IMPLEMENTATION.md](BITWARDEN_FLOW_IMPLEMENTATION.md) |
| **PBKDF2 Upgrade** | Security upgrade summary | [OWASP_PBKDF2_UPGRADE_SUMMARY.md](OWASP_PBKDF2_UPGRADE_SUMMARY.md) |

## Key Features

### 🔒 End-to-End Encryption & Security
- **True end-to-end encryption** - Your master key never leaves your device
- **Zero-knowledge architecture** - Not even server admins can decrypt your data
- **Enterprise-grade encryption** with AES-256-GCM and PBKDF2 (600,000 iterations)
- **Master key stored locally only** - Exists only in device memory during sessions
- **Cross-platform consistency** - Same encryption on all platforms (Blazor, WinUI, iOS, Android, Linux)
- **Encrypted sync** - Only encrypted data synced to MySQL, SQL Server, PostgreSQL, or Supabase
- **SMS-based Two-Factor Authentication (2FA)** - OTP codes via SMS for enhanced security
- **Database-managed SMS settings** - Configure multiple SMS providers through API

### Secure Password Management
- **Password generator** with customizable strength settings
- **Biometric authentication** - Face ID / Touch ID / Fingerprint support on mobile
- **Vault locking** - Secure your passwords when not in use
- **Password breach checking** - Verify password strength and safety
- **Bitwarden-compatible** encryption flow for familiar experience

### Cross-Platform Support
- **Blazor Web App** - Modern web interface with light/dark theme support
- **Uno Platform Mobile App** - Native iOS and Android applications
- **Desktop Support** - Windows (WinUI), macOS, and Linux compatibility
- **Browser Extension** - Chrome, Edge, Firefox with multiple connection modes
- **Shared Components** - Consistent UI across all platforms

### Advanced Organization
- **Collections** - Group related items with custom icons
- **Categories** - Organize within collections for better structure
- **Tags** - Flexible tagging system with color coding
- **Smart Search** - Real-time search across all your data

### Cloud Synchronization
- **Encrypted data sync** - Only encrypted data synchronized across devices
- **RESTful API** - Secure data synchronization with end-to-end encryption
- **Multiple Databases** - SQLite, SQL Server, MySQL, PostgreSQL, Supabase
- **Master keys stay local** - Authentication on each device, no key syncing
- **JWT Authentication** - Secure API access with Bearer tokens
- **Offline Support** - Full functionality without internet connection

### Import & Export
- **Multiple Password Manager Support**:
  - **1Password** - Import from CSV and 1PUX export files
  - **Bitwarden** - Import from CSV exports
  - **LastPass** - Import from CSV exports with folder support
  - **Dashlane** - Import from CSV exports with categories
  - **KeePass** - Import from CSV exports with groups
- **Browser Password Import**:
  - **Google Chrome** - Import from Chrome password CSV exports
  - **Microsoft Edge** - Import from Edge password CSV exports
  - **Mozilla Firefox** - Import from Firefox Logins CSV with timestamps
  - **Apple Safari** - Import from Safari password CSV exports
- **Plugin Architecture** - Extensible system for adding new import sources
- **Bulk Operations** - Import hundreds of items efficiently with progress tracking
- **Smart Detection** - Auto-detects file format and validates compatibility
- **Preview Before Import** - See first 5 items before committing to full import
- **Collection & Category Creation** - Automatically creates folders/categories during import

## SMS Two-Factor Authentication

Enhanced security with SMS-based Two-Factor Authentication (2FA):

### OTP Features
- **Six-digit OTP codes** sent via SMS with configurable expiration (default: 5 minutes)
- **Platform-specific support** - Available on web browsers, Android, and iOS (desktop excluded)
- **Multiple SMS providers** - Twilio, AWS SNS, and Azure Communication Services
- **Rate limiting** - Configurable SMS limits to prevent abuse
- **Backup codes** - Eight-digit recovery codes for emergency access

### Database-Managed SMS Settings
- **API-managed configuration** - Complete REST API for SMS settings management
- **Encrypted credential storage** - SMS provider credentials encrypted with user's master key
- **Multi-provider support** - Configure and switch between multiple SMS providers
- **Platform detection** - Automatic restriction to supported platforms

### SMS Configuration API
- `GET /api/smssettings` - Retrieve all SMS configurations
- `POST /api/smssettings` - Create new SMS settings with encrypted credentials
- `PUT /api/smssettings/{id}` - Update existing SMS configuration
- `DELETE /api/smssettings/{id}` - Remove SMS settings
- `POST /api/smssettings/{id}/activate` - Switch active SMS configuration
- `POST /api/smssettings/{id}/test` - Test SMS settings with sample message

[View Complete OTP Implementation Guide](OTP_IMPLEMENTATION_GUIDE.md)

## Security First

This password manager implements enterprise-grade security with a zero-knowledge architecture:

- **600,000 PBKDF2 iterations** (OWASP 2024 recommendation, 6x stronger than Bitwarden's default)
- **AES-256-GCM encryption** with authenticated encryption preventing tampering
- **SMS-based Two-Factor Authentication** with platform-specific support (web and mobile)
- **Database-managed SMS settings** with encrypted provider credentials
- **Backup codes** for emergency account recovery
- **Zero-knowledge design** - server cannot decrypt your data without master password
- **Session-based vault management** - cryptographic keys cached securely
- **Memory safety** - encryption keys immediately cleared after use

[View Detailed Security Documentation](ENCRYPTION_IMPLEMENTATION.md)

## Project Structure

The solution is organized into focused, maintainable projects:

```
PasswordManagerApp/
├── Web & Mobile Apps
│   ├── PasswordManager.Web/              # Blazor Server Web App
│   ├── PasswordManager.Uno/              # Uno Platform Mobile App (iOS/Android/WebAssembly)
│   └── PasswordManager.Components.Shared/ # Shared Blazor Components
├── Desktop Apps
│   └── PasswordManager.WinUi/            # WinUI 3 Desktop App (Windows)
├── Browser Extension
│   ├── PasswordManager.BrowserExtension/ # Cross-browser extension
│   └── PasswordManager.BrowserExtension.NativeHost/ # Native messaging host
├── Backend Services
│   ├── PasswordManager.API/              # ASP.NET Core Web API
│   ├── PasswordManager.Services/         # Business Logic Services
│   └── PasswordManager.Models/           # Shared Models & DTOs
├── Data Layer
│   ├── PasswordManager.DAL/              # Core Data Access Layer
│   ├── PasswordManager.DAL.SqlServer/    # SQL Server Provider
│   ├── PasswordManager.DAL.MySql/        # MySQL Provider
│   ├── PasswordManager.DAL.Postgres/     # PostgreSQL Provider
│   └── PasswordManager.DAL.SupaBase/     # Supabase Provider
├── Security & Crypto
│   └── PasswordManager.Crypto/           # Encryption & Security
├── Testing
│   ├── PasswordManager.Tests.OTP/        # OTP & SMS Authentication Tests
│   └── PasswordManager.Tests.QrLogin/    # QR Login Tests
└── Import System
    ├── PasswordManager.Imports/             # Import Framework
    ├── PasswordManagerImports.1Password/    # 1Password Plugin
    ├── PasswordManagerImports.Bitwarden/    # Bitwarden Plugin
    ├── PasswordManagerImports.LastPass/     # LastPass Plugin
    ├── PasswordManagerImports.Dashlane/     # Dashlane Plugin
    ├── PasswordManagerImports.KeePass/      # KeePass Plugin
    ├── PasswordManagerImports.Chrome/       # Chrome Browser Plugin
    ├── PasswordManagerImports.Edge/         # Edge Browser Plugin
    ├── PasswordManagerImports.Firefox/      # Firefox Browser Plugin
    └── PasswordManagerImports.Safari/       # Safari Browser Plugin
```

## Applications

### Browser Extension
- **Multiple Connection Modes**:
  - **Native Messaging** - Direct local database access (most secure, offline capable)
  - **Web API** - Connect to your API server for remote access
  - **localStorage** - Offline mode with cached credentials
  - **Auto Mode** - Tries all methods automatically for best experience
- **1Password-Style Inline Autofill** - Click on login fields to see a dropdown list of saved passwords for that website
- **Smart Form Detection** - Automatically detects login, registration, and payment forms
- **Password Autofill** - One-click credential filling with domain-based filtering
- **Password Generator** - Generate strong passwords directly in the browser
- **Cross-browser Support** - Chrome, Edge, Firefox, and other Chromium browsers
- **Consistent UI** - Matches main app styling using shared design system
- **Settings Persistence** - Remembers your preferred connection method

### Web Application
- **Modern Blazor UI** with MudBlazor components
- **Theme Support** - Light, Dark, and System theme options
- **Responsive design** works on all devices
- **API key management** built-in
- **Real-time search** and filtering

### WinUI Desktop Application
- **Native Windows performance** with WinUI 3
- **Windows 11 Fluent Design** integration
- **XAML-powered interface** with smooth animations
- **DPAPI secure storage** for Windows
- **Theme System** - Light, Dark, and System theme support with persistence
- **Professional desktop experience**
- [View Screenshots](screenshots.md) of the interface

### Mobile Application (Uno Platform)
- **Cross-platform** iOS, Android, WebAssembly, Desktop
- **Native performance** with Uno Platform
- **Offline-first** design with SQLite local storage
- **Biometric authentication** - Face ID (iOS) and Fingerprint (Android)
- **Modern UI** with Material Design 3
- **Two-way sync** with cloud API
- [View Mobile App Guide](UNO_PLATFORM_GUIDE.md) for detailed documentation

### API Service
- **RESTful design** with OpenAPI documentation
- **JWT authentication** for secure access
- **Multi-database support** with automatic migrations
- **Health checks** and monitoring
- **Cross-platform deployment**

For detailed information, see the [Technology Stack](TECHNOLOGY_STACK.md) documentation.

## Contributing

We welcome contributions to make this password manager even better. Here's how you can help:

### Ways to Contribute
- **Bug Reports**: Found an issue? Let us know through GitHub Issues
- **Feature Requests**: Have an idea? Share it in GitHub Discussions
- **Code Contributions**: Submit pull requests with improvements
- **Documentation**: Help improve our documentation
- **Testing**: Test on different platforms and report feedback

### Development Setup
1. Fork the repository on GitHub
2. Clone your fork locally
3. Follow the [Development Guide](DEVELOPMENT.md) for setup instructions
4. Make your changes in a feature branch
5. Test your changes thoroughly
6. Submit a pull request with a clear description

### Code Standards
- Follow C# coding conventions
- Write unit tests for new features
- Update documentation for API changes
- Use meaningful commit messages

For detailed contribution guidelines, see our [Development Guide](DEVELOPMENT.md).

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

Special thanks to:
- Microsoft for the excellent .NET ecosystem
- MudBlazor Team for the beautiful UI components
- Entity Framework Team for the robust ORM
- Open Source Community for continuous inspiration and feedback

## Support & Community

### Get Help
- **Documentation**: Check our comprehensive guides above
- **Issues**: Report bugs on [GitHub Issues](https://github.com/dotnetappdev/PasswordManagerApp/issues)
- **Discussions**: Join conversations on [GitHub Discussions](https://github.com/dotnetappdev/PasswordManagerApp/discussions)

### Stay Connected
- Star this repository if you find it useful
- Watch releases to stay updated with new features
- Fork and contribute to help improve the project

---

Built with .NET 9, MAUI, Blazor Server, and MudBlazor

Secure your digital life with enterprise-grade encryption and cross-platform convenience.
