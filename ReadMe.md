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
- **Enhanced Security Import**: Native support for secure CSV imports with advanced encryption
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
- **Multi-Database Support**: Sync between SQLite, SQL Server, PostgreSQL, and MySQL
- **JWT Authentication**: Secure API access with Bearer token authentication
- **Auto-Sync**: Automatic synchronization on app startup and periodic intervals
- **Conflict Resolution**: Smart conflict handling with configurable resolution strategies
- **Data Filtering**: Master passwords and sensitive data automatically excluded from sync
- **Background Sync**: Non-blocking synchronization with progress tracking
- **Cross-Platform Sync**: Sync data across all supported platforms

## 🔒 Security & Encryption

This password manager implements enterprise-grade security with zero-knowledge architecture:

### 🛡️ Enhanced Security Methods
- **PBKDF2 Key Derivation**: 600,000 iterations with SHA-256 (OWASP 2024 recommendation, 6x stronger than Bitwarden's default)
- **AES-256-GCM Encryption**: Authenticated encryption preventing tampering and ensuring data integrity
- **Zero-Knowledge Architecture**: Server cannot decrypt user data without master password
- **Bitwarden-Compatible Flow**: Master password → PBKDF2 → Master Key → AES-256-GCM encryption
- **Two-Layer Security Model**: 
  - Authentication Layer: PBKDF2 hash stored for login verification
  - Encryption Layer: Separate master key encrypts user data
- **Session-Based Vault Management**: Cryptographic keys cached in memory only during active sessions
- **Memory Safety**: Encryption keys are immediately cleared from memory after use
- **Salt Generation**: Cryptographically secure random salt generation for each user (256-bit)
- **Key Stretching**: 600,000 PBKDF2 iterations to prevent brute force attacks
- **Authenticated Encryption**: GCM mode provides both confidentiality and authenticity
- **Password Reveal on Demand**: Instant password decryption using cached session keys (Bitwarden-style UX)

### 🔐 Password Reveal System
- **Secure Decryption**: Passwords decrypted on-demand using cached session keys
- **Memory Protection**: Decrypted passwords never persist in memory longer than necessary
- **Session Validation**: All decrypt operations validate active vault session
- **Audit Trail**: All password reveal operations logged for security monitoring

### 🎯 Industry Standards Compliance
- **OWASP Guidelines**: Follows OWASP Password Storage Cheat Sheet recommendations
- **NIST Compliance**: Aligned with NIST Special Publication 800-63B guidelines
- **Zero-Knowledge Proof**: Complete implementation of zero-knowledge architecture
- **Forward Secrecy**: Each session uses fresh cryptographic material

📋 **[View Detailed Encryption Implementation](ENCRYPTION_IMPLEMENTATION.md)** - Complete technical documentation of the security architecture, encryption algorithms, and implementation details.

## 🛠️ Technology Stack

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
- **SQL Server**: Enterprise database support for production deployments
- **PostgreSQL**: Open-source database option for cloud deployments
- **MySQL**: Popular open-source database with full Entity Framework support

### Security & Cryptography
- **PasswordManager.Crypto**: Custom cryptography library with Bitwarden-compatible encryption
- **PBKDF2**: 600,000 iterations (OWASP 2024 recommendation) for key derivation
- **AES-256-GCM**: Authenticated encryption preventing tampering and ensuring data integrity
- **Zero-Knowledge Architecture**: Server cannot decrypt user data without master password
- **Master Key Derivation**: Bitwarden-compatible master password + salt → encryption key flow
- **Session-Based Vault Management**: Cryptographic keys cached securely during user sessions
- **Memory Safety**: Encryption keys immediately cleared from memory after use
- **Secure Random**: Cryptographically secure random salt generation for each user

### Cloud & API
- **ASP.NET Core**: Web API framework for cloud services
- **JWT Bearer Authentication**: Token-based API security
- **Swagger/OpenAPI**: API documentation and testing
- **Health Checks**: API monitoring and diagnostics
- **CORS**: Cross-origin resource sharing configuration

### Development & Testing
- **xUnit**: Unit testing framework
- **Moq**: Mocking framework for unit tests
- **Entity Framework Core InMemory**: In-memory database for testing
- **Microsoft.Extensions.Logging**: Structured logging framework

## 🏗️ Project Structure

```
PasswordManagerApp/
├── PasswordManager.API/              # ASP.NET Core Web API
│   ├── Controllers/                  # API controllers
│   ├── Services/                     # API-specific services
│   └── Program.cs                    # API startup configuration
├── PasswordManager.App/              # .NET MAUI Blazor App
│   ├── Components/                   # Blazor components
│   │   ├── Pages/                    # Page components
│   │   ├── Layout/                   # Layout components
│   │   └── Auth/                     # Authentication components
│   ├── Platforms/                    # Platform-specific code
│   └── MauiProgram.cs               # App startup configuration
├── PasswordManager.Crypto/           # Cryptography library
│   ├── Interfaces/                   # Crypto service interfaces
│   ├── Services/                     # Crypto implementations
│   └── Extensions/                   # DI extensions
├── PasswordManager.DAL/              # Data Access Layer
│   ├── Models/                       # Entity models
│   ├── Migrations/                   # EF migrations
│   └── PasswordManagerDbContext.cs  # Database context
├── PasswordManager.Models/           # Shared models and DTOs
│   └── DTOs/                         # Data transfer objects
├── PasswordManager.Services/         # Business logic services
│   ├── Interfaces/                   # Service interfaces
│   └── Services/                     # Service implementations
├── PasswordManager.Imports/          # Import framework
│   ├── Interfaces/                   # Import service interfaces
│   └── Services/                     # Import implementations
├── PasswordManagerImports.1Password/ # 1Password import plugin
└── PasswordManagerImports.Bitwarden/ # Enhanced secure import plugin
```

## 🚀 Getting Started

### Prerequisites
- **.NET 9 SDK** or later
- **Visual Studio 2024** or **JetBrains Rider** (recommended)
- **Git** for version control

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/dotnetappdev/PasswordManagerApp.git
   cd PasswordManagerApp
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Set up the database**
   ```bash
   # For local development (SQLite)
   cd PasswordManager.API
   dotnet ef database update
   ```

4. **Run the application**
   ```bash
   # Run the API (optional, for sync features)
   cd PasswordManager.API
   dotnet run

   # Run the MAUI app
   cd PasswordManager.App
   dotnet run
   ```

### Database Configuration

The application supports multiple database providers. Set the `DatabaseProvider` in `appsettings.json`:

#### SQLite (Default)
```json
{
  "DatabaseProvider": "sqlite",
  "ConnectionStrings": {
    "SqliteConnection": "Data Source=passwordmanager.db"
  }
}
```
- Automatically configured for local development
- Database file stored in app data directory
- No additional setup required

#### SQL Server
```json
{
  "DatabaseProvider": "sqlserver",
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=PasswordManager;Trusted_Connection=true;TrustServerCertificate=true;"
  }
}
```

#### PostgreSQL
```json
{
  "DatabaseProvider": "postgresql",
  "ConnectionStrings": {
    "PostgresConnection": "Host=localhost;Database=passwordmanager;Username=postgres;Password=yourpassword"
  }
}
```

#### MySQL
```json
{
  "DatabaseProvider": "mysql",
  "ConnectionStrings": {
    "MySqlConnection": "Server=localhost;Database=PasswordManager;User=root;Password=yourpassword;Port=3306;"
  }
}
```

📋 **[MySQL Setup Guide](MYSQL_SETUP_GUIDE.md)** - Complete guide for setting up MySQL with the Password Manager API.

**Note**: All database providers support the full encryption feature set with 600,000 PBKDF2 iterations and AES-256-GCM encryption.

## 📖 Usage

### First Time Setup
1. Launch the application
2. Create a strong master password
3. Optionally set a password hint
4. Start adding your passwords and secure data

### Adding Items
- Click the "+" button to add new items
- Choose from Login, Credit Card, Secure Note, or WiFi item types
- Fill in the details and save

### Organization
- Create collections to group related items
- Add categories for better organization
- Use tags for flexible filtering and searching

### Importing Data
- Go to Settings → Import
- Select your source (1Password, CSV, etc.)
- Follow the import wizard to migrate your data

### Syncing (Optional)
- Set up the Web API on a server
- Configure sync settings in the app
- Enable automatic sync for seamless cross-device access

## 🔧 Development

### Building from Source
```bash
# Build all projects
dotnet build

# Build specific project
dotnet build PasswordManager.App

# Run tests
dotnet test
```

### Database Migrations
```bash
# Add a new migration
cd PasswordManager.DAL
dotnet ef migrations add MigrationName --startup-project ../PasswordManager.API

# Update database
dotnet ef database update --startup-project ../PasswordManager.API
```

### Creating Import Plugins
1. Create a new class library project
2. Reference `PasswordManager.Imports`
3. Implement `IPasswordImportProvider`
4. Register the plugin in your application

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

### Development Guidelines
- Follow C# coding conventions
- Write unit tests for new features
- Update documentation for API changes
- Use meaningful commit messages

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- **Microsoft** for .NET, MAUI, and Blazor frameworks
- **Entity Framework Team** for the excellent ORM
- **Community contributors** for feedback and improvements

## 📞 Support

- 📧 Email: support@passwordmanager.dev
- 🐛 Issues: [GitHub Issues](https://github.com/dotnetappdev/PasswordManagerApp/issues)
- 💬 Discussions: [GitHub Discussions](https://github.com/dotnetappdev/PasswordManagerApp/discussions)

---

**Made with ❤️ using .NET 9, MAUI, and Blazor**
