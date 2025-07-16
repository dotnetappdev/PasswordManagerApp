# 🔐 Password Manager

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![MAUI](https://img.shields.io/badge/MAUI-Cross--Platform-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/apps/maui)
[![Web API](https://img.shields.io/badge/Web%20API-ASP.NET%20Core-512BD4?style=for-the-badge&logo=dotnet)](https://docs.microsoft.com/aspnet/core/web-api/)
[![Open Source](https://img.shields.io/badge/Open%20Source-MIT-green?style=for-the-badge)](LICENSE)
[![Blazor](https://img.shields.io/badge/Blazor-Web%20UI-512BD4?style=for-the-badge&logo=blazor)](https://blazor.net/)

A modern, secure, and cross-platform password manager built with **.NET 9**, **.NET MAUI**, **Blazor Server**, and **ASP.NET Core Web API**. Keep your passwords, credit cards, secure notes, and WiFi credentials safe and organized across all your devices with cloud synchronization capabilities.

## 📚 Documentation

### 🏗️ Project Documentation
| Component | Description | Documentation |
|-----------|-------------|---------------|
| **Main Project** | Overview and getting started | [ReadMe.md](ReadMe.md) |
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
| **Database Setup** | Database configuration guide | [ReadMe.DatabaseProviders.md](ReadMe.DatabaseProviders.md) |
| **MySQL Setup** | MySQL-specific setup guide | [MYSQL_SETUP_GUIDE.md](MYSQL_SETUP_GUIDE.md) |
| **Encryption Details** | Technical encryption implementation | [ENCRYPTION_IMPLEMENTATION.md](ENCRYPTION_IMPLEMENTATION.md) |
| **Bitwarden Flow** | Bitwarden compatibility details | [BITWARDEN_FLOW_IMPLEMENTATION.md](BITWARDEN_FLOW_IMPLEMENTATION.md) |
| **PBKDF2 Upgrade** | Security upgrade summary | [OWASP_PBKDF2_UPGRADE_SUMMARY.md](OWASP_PBKDF2_UPGRADE_SUMMARY.md) |

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
- **Blazor Web UI**: Responsive, modern web-based interface with MudBlazor components
- **MAUI Blazor Mobile**: Cross-platform mobile app with shared UI components
- **Dark Theme**: Beautiful dark-only theme for reduced eye strain
- **Intuitive Navigation**: Sidebar navigation with expandable sections
- **Modal Dialogs**: Clean, focused editing experiences
- **Real-time Updates**: Live search and filtering
- **Progress Indicators**: Visual feedback for long-running operations

### 🌐 Blazor Web App Features
- **Dark Mode Only**: Sleek, modern dark theme optimized for professional use
- **MudBlazor Components**: Rich, Material Design-based UI components
- **Responsive Design**: Works perfectly on desktop, tablet, and mobile browsers
- **API Key Management**: Built-in API key generation and management interface
- **Settings Panel**: Comprehensive settings management with database provider selection
- **Profile Management**: User profile editing and management
- **Master Password Authentication**: Secure vault unlock with master password
- **1Password-Style Vault**: Familiar password vault interface with card-based layout
- **Real-time Search**: Instant search across all password items
- **Category Filtering**: Filter passwords by categories and collections
- **Copy to Clipboard**: One-click password copying with security notifications
- **CRUD Operations**: Full create, read, update, delete operations for all password items

### 🔧 Advanced Features
- **Shared Component Library**: Reusable Blazor components shared between MAUI and Web apps
- **Plugin Architecture**: Extensible plugin system for import providers
- **Database Migrations**: Automatic database schema updates
- **Cross-Platform**: Runs on Windows, macOS, Linux, iOS, Android, and web browsers
- **Web API**: ASP.NET Core Web API for cloud synchronization and API access
- **Multi-Database**: Support for SQLite, SQL Server, PostgreSQL, MySQL, and Supabase
- **Scalar API Documentation**: Interactive API documentation with JWT support
- **Health Checks**: Built-in API health monitoring
- **Offline First**: Mobile app works without internet connection
- **Data Validation**: Comprehensive input validation and error handling
- **Bulk Operations**: Import hundreds of items at once
- **Identity Integration**: ASP.NET Core Identity for user management across different database providers

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

## � Recent Updates & Fixes

### Build & Compilation Fixes (Latest)
- **✅ Session Management**: Fixed `UnlockVault` method to properly initialize sessions with master keys
- **✅ Interface Consistency**: Updated `IPasswordRevealService` to accept `sessionId` parameters across all methods
- **✅ Vault Session Service**: Fixed authentication flow to use session-based vault management
- **✅ Enum Corrections**: Fixed `ConflictResolution` enum usage in sync operations
- **✅ Database Context**: Resolved missing `IDatabaseContextFactory` interface definitions
- **✅ MySQL Support**: Added proper `ServerVersion` imports for MySQL Entity Framework
- **✅ Configuration Builder**: Fixed missing using statements for configuration management
- **✅ Namespace Consistency**: Resolved interface implementation conflicts between API and Services layers
- **✅ Authentication Service**: Fixed session storage and retrieval for proper vault state management
- **✅ Logout Functionality**: Updated logout to properly clear session data and lock vault

### Security Enhancements
- **🔒 Master Key Derivation**: Implemented proper PBKDF2 key derivation with 600,000 iterations
- **🔒 Session-Based Encryption**: All password operations now use cached session keys for performance
- **🔒 Memory Management**: Sensitive cryptographic material properly cleared from memory
- **🔒 Vault State Management**: Consistent vault lock/unlock state across all components

### Code Quality Improvements
- **📝 Error Handling**: Comprehensive error handling in authentication and encryption services
- **📝 Interface Definitions**: Clear separation between API and Services layer interfaces
- **📝 Type Safety**: Fixed nullable reference type issues and parameter validation
- **📝 Documentation**: Enhanced code documentation and inline comments

## �🛠️ Technology Stack

### Core Frameworks
- **.NET 9**: Latest stable version of Microsoft's unified development platform
- **.NET MAUI**: Cross-platform framework for native mobile and desktop apps
- **ASP.NET Core Web API**: RESTful API for cloud synchronization and data access
- **Blazor Server**: Modern web UI framework using C# with server-side rendering
- **Entity Framework Core**: Object-relational mapping for database operations

### UI & Graphics
- **MudBlazor**: Rich Material Design component library for Blazor
- **MAUI Graphics**: Cross-platform graphics API for custom drawing and animations
- **Blazor Components**: Reusable UI components with two-way data binding
- **Shared Component Library**: Common UI components shared between MAUI and Web apps
- **CSS Grid & Flexbox**: Modern CSS layout for responsive design
- **Custom Animations**: Smooth transitions and loading indicators

### Data & Storage
- **SQLite**: Lightweight, embedded database for local storage
- **Entity Framework Migrations**: Version-controlled database schema changes
- **SQL Server**: Enterprise database support for production deployments
- **PostgreSQL**: Open-source database option for cloud deployments
- **MySQL**: Popular open-source database with full Entity Framework support
- **Supabase**: Cloud-native PostgreSQL with real-time features

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
├── PasswordManager.Web/              # Blazor Server Web Application
│   ├── Components/                   # Web-specific components
│   │   ├── Pages/                    # Web page components
│   │   └── _Imports.razor            # Web imports
│   ├── Program.cs                    # Web app startup configuration
│   └── appsettings.json              # Web app configuration
├── PasswordManager.Components.Shared/ # Shared Blazor Component Library
│   ├── Pages/                        # Shared page components
│   │   ├── MasterPassword.razor      # Master password unlock
│   │   ├── Vault.razor               # Main password vault
│   │   ├── Settings.razor            # Settings management
│   │   └── Admin/                    # Admin components
│   ├── Components/                   # Shared UI components
│   │   ├── Auth/                     # Authentication components
│   │   └── Layout/                   # Layout components
│   └── _Imports.razor                # Shared imports
├── PasswordManager.App/              # .NET MAUI Blazor App
│   ├── Components/                   # MAUI-specific components
│   │   ├── Pages/                    # MAUI page components
│   │   ├── Layout/                   # MAUI layout components
│   │   └── Auth/                     # MAUI authentication components
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
├── PasswordManager.DAL.SqlServer/    # SQL Server provider
├── PasswordManager.DAL.MySql/        # MySQL provider
├── PasswordManager.DAL.SupaBase/     # Supabase provider
├── PasswordManager.Models/           # Shared models and DTOs
│   ├── ApiKey.cs                     # API key model
│   └── DTOs/                         # Data transfer objects
├── PasswordManager.Services/         # Business logic services
│   ├── Interfaces/                   # Service interfaces
│   │   └── IApiKeyService.cs         # API key management
│   └── Services/                     # Service implementations
│       └── ApiKeyService.cs          # API key implementation
├── PasswordManager.Imports/          # Import framework
│   ├── Interfaces/                   # Import service interfaces
│   └── Services/                     # Import implementations
├── PasswordManagerImports.1Password/ # 1Password import plugin
└── PasswordManagerImports.Bitwarden/ # Enhanced secure import plugin
```

## 🌐 Web Application Architecture

The Password Manager now includes a **Blazor Server Web Application** that provides a modern web interface for managing your passwords:

### Web App Features
- **🎨 Dark-Only Theme**: Professional dark theme using MudBlazor components
- **🔐 Master Password Authentication**: Secure vault unlock identical to mobile app
- **📱 Responsive Design**: Works on desktop, tablet, and mobile browsers
- **🔑 API Key Management**: Built-in interface for generating and managing API keys
- **⚙️ Settings Panel**: Database provider selection and configuration
- **👤 Profile Management**: User profile editing and account management
- **🔍 Real-time Search**: Instant search across all password items
- **📁 Category Filtering**: Filter by categories, collections, and tags
- **📋 One-Click Copy**: Copy passwords to clipboard with visual feedback
- **✏️ Full CRUD Operations**: Create, edit, delete passwords and other items

### Shared Component Architecture
The web app and MAUI app share components through `PasswordManager.Components.Shared`:

- **Pages**: Master password, vault, settings, and admin pages
- **Components**: Authentication, layout, and UI components
- **Routing**: Discoverable routes work in both web and mobile contexts
- **Services**: Shared business logic and data access patterns

### Database Provider Support
The web app supports all database providers with ASP.NET Core Identity:

- **SQL Server**: ASP.NET Core Identity with Entity Framework
- **MySQL**: ASP.NET Core Identity with MySQL Entity Framework provider
- **Supabase**: Custom authentication using Supabase auth tables
- **PostgreSQL**: ASP.NET Core Identity with Npgsql provider

### API Integration
- **JWT Authentication**: Secure API access with Bearer tokens
- **API Key Management**: Generate, view, and revoke API keys through web interface
- **Real-time Sync**: Changes in web app sync with mobile apps via API
- **Cross-Platform Data**: Same encryption and data format across all platforms

## 🚀 Getting Started

### Prerequisites
- **.NET 9 SDK** or later
- **Visual Studio 2024** or **JetBrains Rider** (recommended)
- **Git** for version control

> **Note:** This project uses .NET 9 for the latest features and performance improvements. Please ensure you have the .NET 9 SDK installed to build and run the solution.

Recent updates:
- Upgraded to .NET 9 for improved performance and latest features
- Added Blazor Server Web Application with dark theme
- Implemented shared component library for code reuse
- Added API key management system
- Enhanced database provider support including Supabase
- If you see .NET SDK errors, install the SDK from https://aka.ms/dotnet/download.

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

4. **Run the applications**
   ```bash
   # Run the API (for sync features and web app backend)
   cd PasswordManager.API
   dotnet run

   # Run the Blazor Web App (in a new terminal)
   cd PasswordManager.Web
   dotnet run

   # Run the MAUI app (in a new terminal)
   cd PasswordManager.App
   dotnet run
   ```

### Web App Access
- **URL**: `https://localhost:5001` (or the URL shown in the terminal)
- **Features**: Full password management, API key generation, settings
- **Authentication**: Master password (same as mobile app)
- **Theme**: Dark mode only for professional appearance

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

### Web Application
1. Open your browser and navigate to `https://localhost:5001`
2. Enter your master password to unlock the vault
3. Use the sidebar navigation to access different features:
   - **Vault**: View and manage all your passwords
   - **Settings**: Generate API keys and configure database providers
   - **Profile**: Manage your user profile and account settings
4. Create, edit, and organize your passwords using the intuitive interface
5. Use the search functionality to quickly find specific items
6. Generate API keys for programmatic access to your data

### Mobile Application (MAUI)
1. Launch the mobile application on your device
2. Create a strong master password on first launch
3. Optionally set a password hint for recovery
4. Start adding your passwords and secure data using the same UI as the web app

### Adding Items
- Click the "+" button to add new items
- Choose from Login, Credit Card, Secure Note, or WiFi item types
- Fill in the details and save
- Items are automatically encrypted and synchronized across platforms

### API Key Management
- Navigate to Settings in the web app
- Enter a descriptive name for your API key
- Click "Generate API Key" to create a new key
- Copy the generated key immediately (it won't be shown again)
- Use the API key for programmatic access to the REST API
- Delete unused keys from the management interface

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

# Build specific projects
dotnet build PasswordManager.Web          # Web application
dotnet build PasswordManager.App          # MAUI application
dotnet build PasswordManager.Components.Shared  # Shared components

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

**Made with ❤️ using .NET 9, MAUI, Blazor Server, and MudBlazor**
