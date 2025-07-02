# 🔐 Password Manager

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![MAUI](https://img.shields.io/badge/MAUI-Cross--Platform-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/apps/maui)
[![Open Source](https://img.shields.io/badge/Open%20Source-MIT-green?style=for-the-badge)](LICENSE)
[![Blazor](https://img.shields.io/badge/Blazor-Web%20UI-512BD4?style=for-the-badge&logo=blazor)](https://blazor.net/)

A modern, secure, and cross-platform password manager built with **.NET 8**, **.NET MAUI**, and **Blazor**. Keep your passwords, credit cards, secure notes, and WiFi credentials safe and organized across all your devices.

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
- **Offline First**: Works without internet connection
- **Data Validation**: Comprehensive input validation and error handling
- **Bulk Operations**: Import hundreds of items at once

## 🛠️ Technology Stack

### Core Frameworks
- **.NET 8**: Latest version of Microsoft's unified development platform
- **.NET MAUI**: Cross-platform framework for native mobile and desktop apps
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
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/)
- For mobile development: Android SDK and/or Xcode

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

### Development Setup

1. **Open in Visual Studio**
   - Open `PasswordManager.sln`
   - Set `PasswordManager.App` as startup project
   - Press F5 to run

2. **Database Setup**
   - Database is automatically created on first run
   - Migrations are applied automatically
   - Test data is seeded in development mode

## 📱 Platform Support

| Platform | Status | Notes |
|----------|--------|-------|
| Windows | ✅ Supported | Full desktop experience |
| macOS | ✅ Supported | Native Mac application |
| Linux | ✅ Supported | GTK-based UI |
| iOS | ✅ Supported | Native iOS app |
| Android | ✅ Supported | Native Android app |
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
├── PasswordManager.Models/           # Shared data models
├── PasswordManager.DAL/              # Data access layer
├── PasswordManager.Services/         # Business logic services
├── PasswordManager.Imports/          # Import system core
├── PasswordManagerImports.Bitwarden/ # Bitwarden import plugin
└── PasswordManagerImports.1Password/ # 1Password import plugin
```

## 🔐 Security Features

- **Local Storage**: All data stored locally on your device
- **Encryption**: Sensitive data encrypted at rest
- **No Cloud Dependency**: Works completely offline
- **Secure Memory**: Passwords cleared from memory after use
- **Input Validation**: Protection against malicious input

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
  <strong>Built with ❤️ using .NET, MAUI, and Blazor</strong>
  <br>
  <sub>Keep your digital life secure and organized</sub>
</div>
