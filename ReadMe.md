# 🔐 Passw## 🚀 Quick Start

- **[Setup Guide](SETUP.md)** - Complete setup and configuration guide
- **[Getting Started](GETTING_STARTED.md)** - Installation and setup guide
- **[User Guide](USER_GUIDE.md)** - Complete user documentation
- **[Development Guide](DEVELOPMENT.md)** - Developer setup and contributinganager

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![MAUI](https://img.shields.io/badge/MAUI-Cross--Platform-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/apps/maui)
[![Web API](https://img.shields.io/badge/Web%20API-ASP.NET%20Core-512BD4?style=for-the-badge&logo=dotnet)](https://docs.microsoft.com/aspnet/core/web-api/)
[![Open Source](https://img.shields.io/badge/Open%20Source-MIT-green?style=for-the-badge)](LICENSE)
[![Blazor](https://img.shields.io/badge/Blazor-Web%20UI-512BD4?style=for-the-badge&logo=blazor)](https://blazor.net/)

A modern, secure, and cross-platform password manager built with **.NET 9**, **.NET MAUI**, **Blazor Server**, and **ASP.NET Core Web API**. Keep your passwords, credit cards, secure notes, and WiFi credentials safe and organized across all your devices with cloud synchronization capabilities.

## � Quick Start

- **[Getting Started](GETTING_STARTED.md)** - Installation and setup guide
- **[User Guide](USER_GUIDE.md)** - Complete user documentation
- **[Development Guide](DEVELOPMENT.md)** - Developer setup and contributing

## �📚 Documentation

### 🏗️ Project Components
| Component | Description | Documentation |
|-----------|-------------|---------------|
| **Blazor Web App** | Web application with dark theme | [PasswordManager.Web](PasswordManager.Web/README.md) |
| **Shared Components** | Cross-platform UI components | [PasswordManager.Components.Shared](PasswordManager.Components.Shared/README.md) |
| **Cryptography** | Security and encryption details | [PasswordManager.Crypto](PasswordManager.Crypto/README.md) |

### 🗃️ Database Providers
| Provider | Description | Documentation |
|----------|-------------|---------------|
| **SQL Server** | Microsoft SQL Server support | [PasswordManager.DAL.SqlServer](PasswordManager.DAL.SqlServer/README.md) |
| **MySQL** | MySQL database support | [PasswordManager.DAL.MySql](PasswordManager.DAL.MySql/README.md) |
| **PostgreSQL** | PostgreSQL database support | [PasswordManager.DAL.Postgres](PasswordManager.DAL.Postgres/README.md) |
| **Supabase** | Supabase cloud database support | [PasswordManager.DAL.SupaBase](PasswordManager.DAL.SupaBase/README.md) |

### 📋 Additional Documentation
| Document | Description |
|----------|-------------|
| **Complete Setup Guide** | Full setup and configuration guide | [SETUP.md](SETUP.md) |
| **Database Setup** | Database configuration guide | [ReadMe.DatabaseProviders.md](ReadMe.DatabaseProviders.md) |
| **MySQL Setup** | MySQL-specific setup guide | [MYSQL_SETUP_GUIDE.md](MYSQL_SETUP_GUIDE.md) |
| **Technology Stack** | Complete technology overview | [TECHNOLOGY_STACK.md](TECHNOLOGY_STACK.md) |
| **Encryption Details** | Technical encryption implementation | [ENCRYPTION_IMPLEMENTATION.md](ENCRYPTION_IMPLEMENTATION.md) |
| **Bitwarden Flow** | Bitwarden compatibility details | [BITWARDEN_FLOW_IMPLEMENTATION.md](BITWARDEN_FLOW_IMPLEMENTATION.md) |
| **PBKDF2 Upgrade** | Security upgrade summary | [OWASP_PBKDF2_UPGRADE_SUMMARY.md](OWASP_PBKDF2_UPGRADE_SUMMARY.md) |

## ✨ Key Features

### 🔒 Secure Password Management
- **Enterprise-grade encryption** with AES-256-GCM and PBKDF2 (600,000 iterations)
- **Zero-knowledge architecture** - your data stays private
- **Password generator** with customizable strength settings
- **Bitwarden-compatible** encryption flow for familiar experience

### 💻 Cross-Platform Support
- **Blazor Web App** - Modern web interface with dark theme
- **MAUI Mobile App** - Native iOS and Android applications
- **Desktop Support** - Windows, macOS, and Linux compatibility
- **Shared Components** - Consistent UI across all platforms

### 🗂️ Advanced Organization
- **Collections** - Group related items with custom icons
- **Categories** - Organize within collections for better structure
- **Tags** - Flexible tagging system with color coding
- **Smart Search** - Real-time search across all your data

### 🔄 Cloud Synchronization
- **RESTful API** - Secure data synchronization across devices
- **Multiple Databases** - SQLite, SQL Server, MySQL, PostgreSQL, Supabase
- **JWT Authentication** - Secure API access with Bearer tokens
- **Offline Support** - Full functionality without internet connection

### 📊 Import & Export
- **1Password Import** - Native support for 1Password CSV exports
- **Plugin Architecture** - Extensible system for other password managers
- **Bulk Operations** - Import hundreds of items efficiently
- **Progress Tracking** - Real-time feedback during operations

## �️ Security First

This password manager implements enterprise-grade security with a zero-knowledge architecture:

- **600,000 PBKDF2 iterations** (OWASP 2024 recommendation, 6x stronger than Bitwarden's default)
- **AES-256-GCM encryption** with authenticated encryption preventing tampering
- **Zero-knowledge design** - server cannot decrypt your data without master password
- **Session-based vault management** - cryptographic keys cached securely
- **Memory safety** - encryption keys immediately cleared after use

📋 **[View Detailed Security Documentation](ENCRYPTION_IMPLEMENTATION.md)**

## 🏗️ Project Structure

The solution is organized into focused, maintainable projects:

```
PasswordManagerApp/
├── 🌐 Web & Mobile Apps
│   ├── PasswordManager.Web/              # Blazor Server Web App
│   ├── PasswordManager.App/              # .NET MAUI Mobile App
│   └── PasswordManager.Components.Shared/ # Shared Blazor Components
├── 🔧 Backend Services
│   ├── PasswordManager.API/              # ASP.NET Core Web API
│   ├── PasswordManager.Services/         # Business Logic Services
│   └── PasswordManager.Models/           # Shared Models & DTOs
├── 🗃️ Data Layer
│   ├── PasswordManager.DAL/              # Core Data Access Layer
│   ├── PasswordManager.DAL.SqlServer/    # SQL Server Provider
│   ├── PasswordManager.DAL.MySql/        # MySQL Provider
│   ├── PasswordManager.DAL.Postgres/     # PostgreSQL Provider
│   └── PasswordManager.DAL.SupaBase/     # Supabase Provider
├── 🔐 Security & Crypto
│   └── PasswordManager.Crypto/           # Encryption & Security
└── 📥 Import System
    ├── PasswordManager.Imports/          # Import Framework
    ├── PasswordManagerImports.1Password/ # 1Password Plugin
    └── PasswordManagerImports.Bitwarden/ # Bitwarden Plugin
```

## 📱 Applications

### Web Application
- **Modern Blazor UI** with MudBlazor components
- **Dark theme** optimized for professional use
- **Responsive design** works on all devices
- **API key management** built-in
- **Real-time search** and filtering

### Mobile Application
- **Cross-platform** iOS, Android, Windows, macOS
- **Native performance** with .NET MAUI
- **Offline-first** design
- **Biometric authentication** support
- **Shared UI components** with web app

### API Service
- **RESTful design** with OpenAPI documentation
- **JWT authentication** for secure access
- **Multi-database support** with automatic migrations
- **Health checks** and monitoring
- **Cross-platform deployment**

For detailed information, see the **[Technology Stack](TECHNOLOGY_STACK.md)** documentation.

## 🤝 Contributing

We welcome contributions to make this password manager even better! Here's how you can help:

### Ways to Contribute
- **Bug Reports**: Found an issue? Let us know through GitHub Issues
- **Feature Requests**: Have an idea? Share it in GitHub Discussions
- **Code Contributions**: Submit pull requests with improvements
- **Documentation**: Help improve our documentation
- **Testing**: Test on different platforms and report feedback

### Development Setup
1. **Fork the repository** on GitHub
2. **Clone your fork** locally
3. **Follow the [Development Guide](DEVELOPMENT.md)** for setup instructions
4. **Make your changes** in a feature branch
5. **Test your changes** thoroughly
6. **Submit a pull request** with a clear description

### Code Standards
- Follow C# coding conventions
- Write unit tests for new features
- Update documentation for API changes
- Use meaningful commit messages

For detailed contribution guidelines, see our **[Development Guide](DEVELOPMENT.md)**.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

Special thanks to:
- **Microsoft** for the excellent .NET ecosystem
- **MudBlazor Team** for the beautiful UI components
- **Entity Framework Team** for the robust ORM
- **Open Source Community** for continuous inspiration and feedback

## 📞 Support & Community

### Get Help
- **� Documentation**: Check our comprehensive guides above
- **🐛 Issues**: Report bugs on [GitHub Issues](https://github.com/dotnetappdev/PasswordManagerApp/issues)
- **💬 Discussions**: Join conversations on [GitHub Discussions](https://github.com/dotnetappdev/PasswordManagerApp/discussions)
- **📧 Email**: Reach out to support@passwordmanager.dev

### Stay Connected
- **⭐ Star this repository** if you find it useful
- **👁️ Watch releases** to stay updated with new features
- **🔄 Fork and contribute** to help improve the project

---

**Built with ❤️ using .NET 9, MAUI, Blazor Server, and MudBlazor**

*Secure your digital life with enterprise-grade encryption and cross-platform convenience.*
