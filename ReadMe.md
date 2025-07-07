# 🔐 Password Manager

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![MAUI](https://img.shields.io/badge/MAUI-Cross--Platform-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/apps/maui)
[![Web API](https://img.shields.io/badge/Web%20API-ASP.NET%20Core-512BD4?style=for-the-badge&logo=dotnet)](https://docs.microsoft.com/aspnet/core/web-api/)
[![Open Source](https://img.shields.io/badge/Open%20Source-MIT-green?style=for-the-badge)](LICENSE)
[![Blazor](https://img.shields.io/badge/Blazor-Web%20UI-512BD4?style=for-the-badge&logo=blazor)](https://blazor.net/)

A modern, secure, and cross-platform password manager built with **.NET 9**, **.NET MAUI**, **Blazor**, and **ASP.NET Core Web API**. Keep your passwords, credit cards, secure notes, and WiFi credentials safe and organized across all your devices with cloud synchronization capabilities.

## ✨ Features

### 🔒 Password Management
- **Secure Storage**: Store passwords, usernames, and websites with industry-standard encryption
- **Password Generator**: Generate strong, customizable passwords with configurable length and character sets
- **Password Visibility Toggle**: Safely reveal passwords when needed
- **Auto-Fill Ready**: Copy credentials to clipboard with one click
- **Brand Recognition**: Automatic brand icon detection for popular websites

### 💳 Multi-Type Item Support
- **Login Items**: Username, password, website, and notes
- **Credit Cards**: Cardholder name, card number, expiry date, CVV, and bank website
- **Secure Notes**: Encrypted text storage for sensitive information
- **WiFi Credentials**: Network name, password, security type, and notes

### 📁 Organization & Management
- **Collections**: Organize items into hierarchical collections with custom icons and colors
- **Categories**: Categorize items within collections for better organization
- **Tags**: Apply multiple colored tags to items for flexible filtering
- **Search**: Fast, real-time search across all items
- **Filtering**: Filter by collections, categories, and tags

### 📊 Import & Export
- **Plugin-Based Import System**: Modular architecture for importing from other password managers
- **Bitwarden Import**: Native support for Bitwarden CSV exports
- **1Password Import**: Native support for 1Password CSV exports
- **External Plugin Support**: Load third-party import plugins at runtime
- **Progress Tracking**: Real-time import progress with detailed feedback

### 🎨 Modern User Interface
- **Blazor Web UI**: Responsive, modern web-based interface
- **Dark/Light Themes**: Customizable appearance
- **Intuitive Navigation**: Sidebar navigation with expandable sections
- **Modal Dialogs**: Clean, focused editing experiences
- **Real-time Updates**: Live search and filtering
- **Progress Indicators**: Visual feedback for long-running operations

### 🔧 Advanced Features
- **Plugin Architecture**: Extensible plugin system for import providers
- **Database Migrations**: Automatic database schema updates
- **Cross-Platform**: Runs on Windows, macOS, Linux, iOS, and Android
- **Web API**: ASP.NET Core Web API for cloud synchronization
- **Multi-Database**: Support for SQLite, SQL Server, and PostgreSQL
- **Scalar API Documentation**: Interactive API documentation with JWT support
- **Health Checks**: Built-in API health monitoring
- **Offline First**: Works without internet connection
- **Data Validation**: Comprehensive input validation and error handling
- **Bulk Operations**: Import hundreds of items at once

### 🔄 Cloud Synchronization
- **Web API Integration**: RESTful API for secure data synchronization
- **Multi-Database Support**: Sync between SQLite, SQL Server, and PostgreSQL
- **JWT Authentication**: Secure API access with Bearer token authentication
- **Auto-Sync**: Automatic synchronization on app startup and periodic intervals
- **Conflict Resolution**: Smart conflict handling with configurable resolution strategies
- **Data Filtering**: Master passwords and sensitive data automatically excluded from sync
- **Background Sync**: Non-blocking synchronization with progress tracking
- **Cross-Platform Sync**: Sync data across all supported platforms

## � Security & Encryption

This password manager implements enterprise-grade security with zero-knowledge architecture:

- **PBKDF2 Key Derivation**: 100,000+ iterations with SHA-256 for strong key derivation
- **AES-256-GCM Encryption**: Authenticated encryption preventing tampering
- **Zero-Knowledge Architecture**: Server cannot decrypt user data without master password
- **Bitwarden-Compatible Security**: Same encryption standards and approach
- **Memory Safety**: Encryption keys are immediately cleared from memory after use
- **Secure Database Storage**: Only encrypted ciphertext and authentication hashes stored

📋 **[View Detailed Encryption Implementation](ENCRYPTION_IMPLEMENTATION.md)** - Complete technical documentation of the security architecture, encryption algorithms, and implementation details.

## �🛠️ Technology Stack

### Core Frameworks
- **.NET 9**: Latest version of Microsoft's unified development platform
- **.NET MAUI**: Cross-platform framework for native mobile and desktop apps
- **ASP.NET Core Web API**: RESTful API for cloud synchronization and data access
- **Blazor**: Modern web UI framework using C# instead of JavaScript
- **Entity Framework Core**: Object-relational mapping for database operations

### UI & Graphics
- **MAUI Graphics**: Cross-platform graphics API for custom drawing and animations
- **Blazor Components**: Reusable UI components with two-way data binding
- **CSS Grid & Flexbox**: Modern CSS layout for responsive design
- **Custom Animations**: Smooth transitions and loading indicators

### Data & Storage
- **SQLite**: Lightweight, embedded database for local storage
- **Entity Framework Migrations**: Version-controlled database schema changes
- **FileHelpers**: CSV parsing and data import library
- **JSON Configuration**: Plugin metadata and configuration management

### Architecture
- **Plugin System**: MEF-based plugin discovery and loading
- **Dependency Injection**: Built-in IoC container for loose coupling
- **Repository Pattern**: Clean separation of data access logic
- **Interface-Based Design**: Testable and maintainable code architecture

## 🚀 Getting Started

### Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/)
- For mobile development: Android SDK and/or Xcode
- For Web API: SQL Server (optional) or PostgreSQL (optional) for cloud synchronization

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/password-manager.git
   cd password-manager
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Build the solution**
   ```bash
   dotnet build
   ```

4. **Run the application**
   ```bash
   dotnet run --project PasswordManager.App
   ```

5. **Run the Web API (optional for cloud sync)**
   ```bash
   dotnet run --project PasswordManager.API
   ```

### API Documentation

The Web API includes interactive documentation powered by Scalar:

1. **Start the API**
   ```bash
   dotnet run --project PasswordManager.API
   ```

2. **Access API Documentation**
   - Open your browser to `https://localhost:7001/scalar/v1`
   - Interactive API testing with JWT authentication support
   - Complete endpoint documentation with examples

### Development Setup

1. **Open in Visual Studio**
   - Open `PasswordManager.sln`
   - Set `PasswordManager.App` as startup project
   - Press F5 to run

2. **Database Setup**
   - Local SQLite database is automatically created on first run
   - Migrations are applied automatically
   - Test data is seeded in development mode
   - For cloud sync: Configure SQL Server or PostgreSQL connection strings in appsettings.json

3. **API Configuration (optional)**
   - Update `PasswordManager.API/appsettings.json` with your database connection strings
   - Configure JWT settings for authentication
   - Set sync intervals and options

## 🌐 Web API Features

### Authentication Endpoints
- **POST /register** - User registration with email and password
- **POST /login** - User authentication with JWT token response
- **POST /refresh** - Refresh JWT tokens
- **POST /forgot-password** - Password reset request
- **POST /reset-password** - Password reset confirmation

### Data Endpoints
- **GET/POST/PUT/DELETE /api/passworditems** - Password item management
- **GET/POST/PUT/DELETE /api/categories** - Category management
- **GET/POST/PUT/DELETE /api/collections** - Collection management
- **GET/POST/PUT/DELETE /api/tags** - Tag management

### Sync Endpoints
- **POST /api/sync** - Database synchronization between providers
- **GET /health** - API health check

### Features
- **JWT Bearer Authentication** with refresh token support
- **Swagger/Scalar Documentation** at `/scalar/v1`
- **CORS Support** for web clients
- **Health Checks** for monitoring
- **Multi-Database Support** (SQLite, SQL Server, PostgreSQL)
- **Automatic Data Filtering** (excludes master passwords from sync)

## 📱 Platform Support

| Platform | Status | Notes |
|----------|--------|-------|
| Windows | ✅ Supported | Full desktop experience |
| macOS | ✅ Supported | Native Mac application |
| Linux | ✅ Supported | GTK-based UI |
| iOS | ✅ Supported | Native iOS app |
| Android | ✅ Supported | Native Android app |
| Web API | ✅ Supported | RESTful API for cloud sync |
| Web | 🔄 In Progress | PWA support planned |

## 🔌 Plugin Development

Create custom import plugins by implementing the `IPasswordImportPlugin` interface:

```csharp
public class CustomImportPlugin : IPasswordImportPlugin
{
    public PluginMetadata Metadata { get; } = new()
    {
        Name = "CustomImporter",
        DisplayName = "Custom Password Manager",
        Version = "1.0.0",
        Author = "Your Name",
        Description = "Import from Custom Password Manager"
    };

    public string[] SupportedFileExtensions => new[] { ".csv", ".json" };

    public Task<ImportResult> ImportAsync(Stream fileStream, 
        IProgress<ImportProgress> progress = null)
    {
        // Implementation here
    }
}
```

## 🗂️ Project Structure

```
PasswordManager/
├── PasswordManager.App/              # Main MAUI application
│   ├── Components/Pages/             # Blazor pages and components
│   ├── Services/                     # Application services
│   └── Platforms/                    # Platform-specific code
├── PasswordManager.API/              # ASP.NET Core Web API
│   ├── Controllers/                  # API controllers
│   ├── Services/                     # API business logic
│   ├── Interfaces/                   # Service contracts
│   └── DTOs/                         # API-specific data transfer objects
├── PasswordManager.Models/           # Shared data models and DTOs
├── PasswordManager.DAL/              # Data access layer
├── PasswordManager.Services/         # Business logic services
├── PasswordManager.Crypto/           # Cryptographic services (NEW)
│   ├── Interfaces/                   # Crypto service contracts
│   ├── Services/                     # PBKDF2 and AES-256-GCM implementations
│   ├── Extensions/                   # DI container extensions
│   └── Tests/                        # Crypto functionality tests
├── PasswordManager.Imports/          # Import system core
├── PasswordManagerImports.Bitwarden/ # Bitwarden import plugin
└── PasswordManagerImports.1Password/ # 1Password import plugin
```

## 🔐 Security Features

### Enterprise-Grade Encryption
- **PBKDF2 Key Derivation**: 100,000+ iterations with SHA-256 for strong key derivation
- **AES-256-GCM Encryption**: Authenticated encryption preventing tampering and ensuring confidentiality
- **Zero-Knowledge Architecture**: Server cannot decrypt user data without master password
- **Bitwarden-Compatible Security**: Same encryption standards and security model
- **Memory Safety**: Encryption keys immediately cleared from memory after use
- **Salt-Based Security**: Unique user salts prevent rainbow table attacks

### Database Security
- **Encrypted Storage**: Only encrypted ciphertext stored in database
- **No Plain Text Passwords**: Master passwords never stored, only authentication hashes
- **Secure Field Encryption**: Individual field encryption for passwords, TOTP secrets, notes
- **Authentication Separation**: Authentication hash cannot be used for data decryption
- **Forward Secrecy**: Master password changes require complete data re-encryption

### Application Security
- **Local Storage**: All data stored locally on your device with encryption at rest
- **Cloud Sync Security**: JWT-based authentication for API access with encrypted data
- **User Data Isolation**: Multi-tenant architecture with user-specific data and encryption keys
- **No Cloud Dependency**: Works completely offline (cloud sync is optional)
- **Input Validation**: Protection against malicious input and injection attacks
- **Master Password Protection**: Master passwords automatically excluded from cloud sync
- **Token Security**: JWT tokens with configurable expiration and refresh capabilities

### Authentication & API
- **ASP.NET Core Identity**: User management and authentication
- **JWT Bearer Tokens**: Secure API authentication with refresh tokens
- **Scalar OpenAPI**: Interactive API documentation and testing
- **CORS Support**: Cross-origin resource sharing for web clients
- **Health Checks**: API health monitoring and diagnostics

### Data & Storage
- **SQLite**: Lightweight, embedded database for local storage
- **Entity Framework Migrations**: Version-controlled database schema changes
- **FileHelpers**: CSV parsing and data import library
- **JSON Configuration**: Plugin metadata and configuration management

### Architecture
- **Plugin System**: MEF-based plugin discovery and loading
- **Dependency Injection**: Built-in IoC container for loose coupling
- **Repository Pattern**: Clean separation of data access logic
- **Interface-Based Design**: Testable and maintainable code architecture

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guidelines](CONTRIBUTING.md) for details.

### How to Contribute
1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Areas for Contribution
- 🔌 Import plugins for other password managers
- 🎨 UI/UX improvements
- 🌐 Localization and translations
- 🔒 Security enhancements
- 📱 Platform-specific features
- 🧪 Test coverage improvements
- 🌐 Web API enhancements and new endpoints
- 🔄 Sync optimization and conflict resolution
- 📊 API monitoring and analytics

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- [.NET MAUI Team](https://github.com/dotnet/maui) for the amazing cross-platform framework
- [Blazor Team](https://github.com/dotnet/aspnetcore) for the modern web UI framework
- [Entity Framework Team](https://github.com/dotnet/efcore) for the excellent ORM
- [SimpleIcons](https://simpleicons.org/) for the beautiful brand icons
- All contributors who help make this project better

## 📞 Support

- 📚 [Documentation](https://github.com/yourusername/password-manager/wiki)
- 🐛 [Report Issues](https://github.com/yourusername/password-manager/issues)
- 💬 [Discussions](https://github.com/yourusername/password-manager/discussions)
- 📧 Email: support@passwordmanager.dev

---

<div align="center">
  <strong>Built with ❤️ using .NET 9, MAUI, Blazor, and ASP.NET Core</strong>
  <br>
  <sub>Keep your digital life secure and organized with cloud synchronization</sub>
</div>
