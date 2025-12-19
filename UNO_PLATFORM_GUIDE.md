# Password Manager - Uno Platform Mobile App Guide

## 📱 Overview

The Password Manager Uno Platform app delivers a professional, cross-platform mobile experience with a design inspired by 1Password. Built with Uno Platform, it provides native performance on iOS, Android, and WebAssembly with a consistent, beautiful interface.

## ✨ Key Features

### 🎨 1Password-Inspired Design
- **Modern Material Design 3** - Clean, professional interface with refined colors and typography
- **Polished UI Components** - Rounded cards, smooth animations, and intuitive interactions
- **Professional Color Palette** - Carefully selected colors that match 1Password's aesthetic
- **Consistent Spacing** - Material Design spacing system for visual harmony
- **Smooth Animations** - Subtle transitions and micro-interactions for delightful UX

### 🔒 Security First
- **Biometric Authentication** - Face ID on iOS, Fingerprint/Face unlock on Android
- **Offline-First Architecture** - Full functionality without internet connection
- **Local Encryption** - SQLite database with secure local storage
- **Zero-Knowledge Sync** - End-to-end encrypted synchronization with API
- **Auto-Lock** - Configurable timeout for automatic app locking

### 📦 Core Functionality
- ✅ **Secure Authentication** - Email/password with optional biometric login
- ✅ **Password Management** - Create, view, edit, and delete password entries
- ✅ **Category Organization** - Organize passwords with customizable categories
- ✅ **Smart Search** - Real-time search across all password entries
- ✅ **Favorites** - Quick access to frequently used passwords
- ✅ **Two-Way Sync** - Seamless synchronization between device and cloud
- ✅ **Offline Support** - Full access to passwords without connectivity

## 🏗️ Architecture

### Technology Stack

| Component | Technology | Version |
|-----------|-----------|---------|
| **Framework** | Uno Platform | 6.4.42 |
| **UI Framework** | WinUI 3 / Uno.WinUI | 3.0 |
| **Navigation** | Uno.Extensions.Navigation | Latest |
| **MVVM** | CommunityToolkit.Mvvm | 8.x |
| **Local Database** | SQLite (sqlite-net-pcl) | Latest |
| **HTTP Client** | Microsoft.Extensions.Http | 9.0 |
| **Dependency Injection** | Microsoft.Extensions.DependencyInjection | 9.0 |
| **Theme** | Uno.Toolkit.WinUI.Material | Latest |

### Project Structure

```
PasswordManager.Uno/
├── 📱 Presentation/              # UI Layer (MVVM)
│   ├── Pages/
│   │   ├── Login/               # Authentication page
│   │   │   ├── LoginPage.xaml
│   │   │   ├── LoginPage.xaml.cs
│   │   │   └── LoginModel.cs
│   │   ├── Passwords/           # Password management
│   │   │   ├── PasswordsPage.xaml
│   │   │   ├── PasswordsPage.xaml.cs
│   │   │   └── PasswordsModel.cs
│   │   ├── Categories/          # Category management
│   │   │   ├── CategoriesPage.xaml
│   │   │   ├── CategoriesPage.xaml.cs
│   │   │   └── CategoriesModel.cs
│   │   └── Settings/            # App settings
│   │       ├── SettingsPage.xaml
│   │       ├── SettingsPage.xaml.cs
│   │       └── SettingsModel.cs
│   └── Shell.xaml               # App shell with navigation
│
├── 🔧 Services/                  # Business Logic Layer
│   ├── LocalDatabase/           # SQLite database services
│   │   ├── LocalPasswordItem.cs # Password entity
│   │   ├── LocalCategory.cs     # Category entity
│   │   └── LocalDatabaseService.cs # Database operations
│   ├── Sync/                    # API synchronization
│   │   └── SyncService.cs       # Two-way sync logic
│   └── Biometric/               # Biometric authentication (platform-specific)
│
├── 📦 Models/                    # Data models and DTOs
│   ├── AppConfig.cs             # Configuration model
│   └── Entity.cs                # Base entity
│
├── 🎨 Styles/                    # Application styles and themes
│   └── AppStyles.xaml           # Custom styles
│
├── 🖼️ Assets/                    # App icons and resources
│   └── SharedAssets.md
│
└── 📱 Platforms/                 # Platform-specific code
    ├── Android/                 # Android-specific implementations
    ├── iOS/                     # iOS-specific implementations
    ├── WebAssembly/             # WASM-specific implementations
    └── Desktop/                 # Desktop-specific implementations
```

## 🎨 UI Design Philosophy

### 1Password-Inspired Interface

The app's design takes inspiration from 1Password's polished, professional aesthetic:

#### Color Palette
- **Primary Blue** - `#0066FF` - Accent color for CTAs and highlights
- **Background (Light)** - `#FAFBFC` - Clean, modern white background
- **Background (Dark)** - `#1A1A1A` - Professional dark surface
- **Card Background (Light)** - `#FFFFFF` - Pure white cards
- **Card Background (Dark)** - `#2A2A2A` - Dark elevated surfaces
- **Text Primary** - `#000000` / `#FFFFFF` - High contrast text
- **Text Secondary** - `#6B6B6B` / `#B0B0B0` - Supporting text

#### Typography
- **Headings** - Segoe UI Variable, semi-bold, 24-28pt
- **Body** - Segoe UI Variable, regular, 14-16pt
- **Captions** - Segoe UI Variable, regular, 12pt
- **Buttons** - Segoe UI Variable, semi-bold, 14pt

#### Spacing System
Following Material Design 3 spacing:
- **Extra Small** - 4dp
- **Small** - 8dp
- **Medium** - 16dp
- **Large** - 24dp
- **Extra Large** - 32dp

#### Component Design
- **Cards** - 8dp corner radius, subtle shadows, 1dp borders
- **Buttons** - 8dp corner radius, 48dp height for primary actions
- **Icons** - 40dp circular avatars with brand color backgrounds
- **Input Fields** - 8dp corner radius, clear focus states
- **Lists** - Card-based items with swipe gestures

## 📱 Platform Support

### Android (net9.0-android)
- **Minimum SDK**: Android 7.0 (API 24)
- **Target SDK**: Android 14 (API 34)
- **Features**:
  - Fingerprint authentication
  - Face unlock (supported devices)
  - Material Design 3 components
  - Adaptive icons
  - Dark theme support

### iOS (net9.0-ios)
- **Minimum Version**: iOS 14.0
- **Target Version**: iOS 17.0
- **Features**:
  - Face ID authentication
  - Touch ID authentication
  - Native iOS design language
  - Dark mode support
  - SF Symbols (where available)

### WebAssembly (net9.0-browserwasm)
- **Browsers**: Chrome, Edge, Firefox, Safari
- **Features**:
  - Progressive Web App (PWA) capabilities
  - Offline support with service workers
  - Responsive design for mobile and desktop browsers

### Desktop (net9.0-desktop)
- **Platforms**: Windows, macOS, Linux
- **Features**:
  - Native desktop performance
  - Keyboard shortcuts
  - Window state persistence

## 🔐 Local Database Schema

### LocalPasswordItem Table

| Column | Type | Description |
|--------|------|-------------|
| `LocalId` | INTEGER PRIMARY KEY | Auto-increment local identifier |
| `ServerId` | INTEGER | Server ID from API (null for offline-only items) |
| `Title` | TEXT | Password entry title |
| `Description` | TEXT | Optional description |
| `Type` | TEXT | Item type (Login, Card, Note, etc.) |
| `Website` | TEXT | Associated website URL |
| `Username` | TEXT | Username or email |
| `EncryptedPassword` | TEXT | Encrypted password value |
| `CategoryId` | INTEGER | Associated category ID |
| `IsFavorite` | INTEGER | Favorite flag (0/1) |
| `IsArchived` | INTEGER | Archived flag (0/1) |
| `IsDeleted` | INTEGER | Soft delete flag (0/1) |
| `CreatedAt` | TEXT | ISO 8601 creation timestamp |
| `LastModified` | TEXT | ISO 8601 last modified timestamp |
| `LastAccessedAt` | TEXT | ISO 8601 last access timestamp |
| `NeedsSyncToServer` | INTEGER | Sync flag (0/1) |
| `LastSyncedAt` | TEXT | ISO 8601 last sync timestamp |

### LocalCategory Table

| Column | Type | Description |
|--------|------|-------------|
| `LocalId` | INTEGER PRIMARY KEY | Auto-increment local identifier |
| `ServerId` | INTEGER | Server ID from API |
| `Name` | TEXT | Category name |
| `Description` | TEXT | Optional description |
| `IconName` | TEXT | Icon identifier |
| `UserId` | INTEGER | Owner user ID |
| `CreatedAt` | TEXT | ISO 8601 creation timestamp |
| `LastModified` | TEXT | ISO 8601 last modified timestamp |
| `NeedsSyncToServer` | INTEGER | Sync flag (0/1) |
| `LastSyncedAt` | TEXT | ISO 8601 last sync timestamp |

## 🔄 Synchronization Strategy

### Two-Way Sync

The app implements a robust two-way synchronization strategy:

#### 1. Pull from Server (Download)
```
1. Authenticate with API using JWT token
2. Fetch all passwords: GET /api/passworditems
3. Fetch all categories: GET /api/categories
4. For each server item:
   - Check if exists locally (by ServerId)
   - If exists: Update local data with server data
   - If not: Create new local record
5. Update sync timestamps
6. Clear NeedsSyncToServer flags for synced items
```

#### 2. Push to Server (Upload)
```
1. Query local items with NeedsSyncToServer = 1
2. For each unsynced item:
   - If ServerId is null: POST to create new server item
   - If ServerId exists: PUT to update server item
3. Update local ServerId from server response
4. Update LastSyncedAt timestamp
5. Clear NeedsSyncToServer flag
```

#### 3. Conflict Resolution
- **Server Wins**: During sync, server data always takes precedence
- **Local Changes**: Marked with `NeedsSyncToServer` flag for next sync
- **Deleted Items**: Soft delete locally, actual delete happens on server
- **Offline Changes**: Queued for sync when connection restored

### Sync Triggers
- **Login**: Initial sync on successful authentication
- **Manual**: Pull-to-refresh gesture on passwords page
- **Background**: Automatic sync every 15 minutes (when implemented)
- **On Change**: Local changes automatically marked for sync

## 🔒 Security Features

### Authentication
- **Email/Password**: Traditional authentication with JWT tokens
- **Biometric Login**: Face ID (iOS) / Fingerprint (Android) for quick access
- **Token Management**: Secure storage of JWT tokens using platform APIs
- **Auto-Lock**: Configurable timeout for automatic logout

### Data Protection
- **Local Encryption**: SQLite database with encryption at rest
- **Secure Storage**: Platform-specific secure storage (Keychain on iOS, KeyStore on Android)
- **Memory Safety**: Sensitive data cleared from memory after use
- **HTTPS Only**: All API communication over TLS 1.3
- **Certificate Pinning**: Validate server certificates (recommended for production)

### Password Security
- **Client-Side Encryption**: Passwords encrypted before storage
- **Zero-Knowledge**: Server cannot decrypt user passwords
- **Password Generator**: Strong password generation with customizable rules
- **Password Health**: Analyze password strength and reuse (future)

## 🚀 Getting Started

### Prerequisites

#### Development Environment
- **Operating System**: Windows 10/11, macOS 12+, or Ubuntu 20.04+
- **.NET 9.0 SDK**: Latest version
- **Visual Studio 2022** or **Visual Studio Code** with C# extension
- **Uno Platform Workload**: Install with `dotnet workload install android`

#### For Android Development
- **Android SDK**: API 34 (Android 14)
- **Android Emulator** or physical device
- **JDK 17**: For Android build tools

#### For iOS Development (macOS only)
- **Xcode 15+**: Latest stable version
- **iOS Simulator** or physical device
- **Apple Developer Account**: For device deployment

### Installation

#### 1. Clone the Repository
```bash
git clone https://github.com/dotnetappdev/PasswordManagerApp.git
cd PasswordManagerApp
```

#### 2. Install Workloads
```bash
# Install Android workload
dotnet workload install android

# Install iOS workload (macOS only)
dotnet workload install ios

# Install all Uno Platform workloads
dotnet workload install uno
```

#### 3. Configure API Endpoint
Edit `PasswordManager.Uno/appsettings.json`:
```json
{
  "ApiBaseUrl": "https://your-api-domain.com"
}
```

For local development:
```json
{
  "ApiBaseUrl": "https://localhost:7123"
}
```

#### 4. Restore Dependencies
```bash
cd PasswordManager.Uno
dotnet restore
```

### Building the App

#### Android
```bash
# Build for Android
dotnet build -f net9.0-android

# Build Release APK
dotnet build -f net9.0-android -c Release

# Build Android App Bundle (for Play Store)
dotnet publish -f net9.0-android -c Release
```

#### iOS (macOS only)
```bash
# Build for iOS Simulator
dotnet build -f net9.0-ios

# Build for iOS Device (requires provisioning profile)
dotnet build -f net9.0-ios -c Release /p:RuntimeIdentifier=ios-arm64

# Create iOS IPA (for App Store)
dotnet publish -f net9.0-ios -c Release
```

#### WebAssembly
```bash
# Build WASM
dotnet build -f net9.0-browserwasm

# Run development server
dotnet run -f net9.0-browserwasm
```

#### Desktop
```bash
# Build for Desktop
dotnet build -f net9.0-desktop

# Run Desktop app
dotnet run -f net9.0-desktop
```

### Running the App

#### Android Emulator
```bash
# List available emulators
adb devices

# Run on Android emulator
dotnet run -f net9.0-android
```

#### iOS Simulator (macOS)
```bash
# List available simulators
xcrun simctl list devices

# Run on iOS simulator
dotnet run -f net9.0-ios
```

#### WebAssembly Browser
```bash
# Start development server (opens browser automatically)
dotnet run -f net9.0-browserwasm
```

## 📸 Screenshots

### iOS Screenshots
> **Note**: Screenshots to be added. See [MOBILE_SCREENSHOTS.md](MOBILE_SCREENSHOTS.md) for capture instructions.

- Login Screen (Light/Dark)
- Password List (Light/Dark)
- Password Details (Light/Dark)
- Add/Edit Password (Light/Dark)
- Categories (Light/Dark)
- Settings (Light/Dark)

### Android Screenshots
> **Note**: Screenshots to be added. See [MOBILE_SCREENSHOTS.md](MOBILE_SCREENSHOTS.md) for capture instructions.

- Login Screen (Light/Dark)
- Password List (Light/Dark)
- Password Details (Light/Dark)
- Add/Edit Password (Light/Dark)
- Categories (Light/Dark)
- Settings (Light/Dark)

## 🎯 Usage Guide

### First Time Setup

1. **Launch the App**: Open Password Manager on your device
2. **Login**: Enter your email and password
3. **Enable Biometrics** (Optional): Toggle biometric login for quick access
4. **Initial Sync**: Wait for initial sync to download your passwords
5. **Explore**: Browse your passwords, favorites, and categories

### Managing Passwords

#### Viewing Passwords
1. Navigate to **Passwords** tab
2. Browse list or use search
3. Tap a password to view details
4. Tap eye icon to reveal password

#### Adding New Password
1. Tap **+** floating action button
2. Fill in password details:
   - Title (required)
   - Website URL
   - Username
   - Password (use generator or enter manually)
   - Category
   - Notes
3. Tap **Save**
4. Changes sync automatically when online

#### Editing Password
1. Tap password entry to open details
2. Tap **Edit** button
3. Modify fields as needed
4. Tap **Save**

#### Deleting Password
1. Swipe left on password entry
2. Tap **Delete** button
3. Confirm deletion
4. Deletion syncs to server when online

#### Favoriting Password
1. Tap star icon on password entry
2. Access favorites from main menu
3. Favorite status syncs across devices

### Organizing with Categories

1. Navigate to **Categories** tab
2. View all categories with password counts
3. Tap category to filter passwords
4. Create new categories in web app (mobile support coming soon)

### Syncing Data

#### Manual Sync
1. Go to **Passwords** page
2. Pull down to refresh
3. Wait for sync to complete
4. View sync status in banner

#### Automatic Sync
- Syncs on login
- Syncs on app resume
- Background sync (coming soon)

### Settings

1. Navigate to **Settings** tab
2. Configure:
   - Biometric login
   - Auto-lock timeout
   - Theme preference
   - Sync settings
   - About information

## 🔧 Development

### Adding New Pages

1. **Create Page Directory**:
   ```
   Presentation/Pages/YourPage/
   ├── YourPageModel.cs
   ├── YourPage.xaml
   └── YourPage.xaml.cs
   ```

2. **Create ViewModel**:
   ```csharp
   public partial class YourPageModel : ObservableObject
   {
       [ObservableProperty]
       private string title = "Your Page";
       
       [RelayCommand]
       private async Task LoadDataAsync()
       {
           // Your logic
       }
   }
   ```

3. **Create XAML Page**:
   ```xml
   <Page x:Class="PasswordManager.Mobile.Presentation.Pages.YourPage.YourPage"
         xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
         xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
       <Grid>
           <TextBlock Text="{Binding Title}" />
       </Grid>
   </Page>
   ```

4. **Register in App.xaml.cs**:
   ```csharp
   views.Register(
       new ViewMap<YourPage, YourPageModel>()
   );
   
   routes.Register(
       new RouteMap("YourPage", View: views.FindByViewModel<YourPageModel>())
   );
   ```

### Adding Services

1. **Create Service Interface and Implementation**:
   ```csharp
   public interface IYourService
   {
       Task<Result> DoSomethingAsync();
   }
   
   public class YourService : IYourService
   {
       public async Task<Result> DoSomethingAsync()
       {
           // Implementation
       }
   }
   ```

2. **Register in App.xaml.cs**:
   ```csharp
   services.AddSingleton<IYourService, YourService>();
   ```

### Testing

#### Unit Tests
```bash
# Create test project
dotnet new xunit -n PasswordManager.Uno.Tests

# Add reference to main project
dotnet add reference ../PasswordManager.Uno/PasswordManager.Uno.csproj

# Run tests
dotnet test
```

#### UI Tests
```bash
# Install Uno.UITest
dotnet add package Uno.UITest

# Run UI tests on Android
dotnet test --filter Platform=Android

# Run UI tests on iOS
dotnet test --filter Platform=iOS
```

## 📚 Best Practices

### UI/UX
- ✅ Use Material Design 3 components from Uno.Toolkit
- ✅ Implement pull-to-refresh for data refresh
- ✅ Show loading states with progress indicators
- ✅ Display error messages with clear actions
- ✅ Use swipe gestures for quick actions
- ✅ Maintain consistent spacing (8dp grid)
- ✅ Support both light and dark themes

### Performance
- ✅ Use `ListView` virtualization for large lists
- ✅ Implement lazy loading for data
- ✅ Cache images and avoid memory leaks
- ✅ Use async/await for all I/O operations
- ✅ Minimize database queries
- ✅ Debounce search input

### Security
- ✅ Never log sensitive data (passwords, tokens)
- ✅ Use secure storage for tokens
- ✅ Implement auto-lock functionality
- ✅ Clear sensitive data from memory
- ✅ Validate all user input
- ✅ Use HTTPS for all API calls

### Code Quality
- ✅ Follow C# coding conventions
- ✅ Use MVVM pattern consistently
- ✅ Write unit tests for business logic
- ✅ Document complex logic
- ✅ Use dependency injection
- ✅ Handle errors gracefully

## 🐛 Troubleshooting

### Build Issues

#### Android Build Fails
```bash
# Clean build output
dotnet clean

# Restore workloads
dotnet workload restore

# Rebuild
dotnet build -f net9.0-android
```

#### iOS Build Fails (macOS)
```bash
# Update Xcode command line tools
sudo xcode-select --install

# Clean build
dotnet clean

# Rebuild
dotnet build -f net9.0-ios
```

### Runtime Issues

#### Database Not Found
- Check database path in LocalDatabaseService
- Ensure write permissions to app data directory
- Reset app data if corrupted

#### Sync Not Working
- Verify API endpoint in appsettings.json
- Check JWT token validity
- Ensure internet connectivity
- Check API server status

#### Biometric Login Not Working
- Verify biometric permission in AndroidManifest.xml / Info.plist
- Check device supports biometrics
- Ensure biometric is enrolled on device

### Common Errors

| Error | Solution |
|-------|----------|
| `Database is locked` | Close other connections, use async methods |
| `401 Unauthorized` | JWT token expired, re-login required |
| `Network error` | Check internet connection, verify API URL |
| `NullReferenceException` | Check data binding, ensure view model initialized |

## 🚀 Future Enhancements

### Short-Term Roadmap
- [ ] Password details view page
- [ ] Add/Edit password forms
- [ ] Category management
- [ ] Password generator
- [ ] Settings page with preferences
- [ ] Biometric authentication implementation
- [ ] Pull-to-refresh improvements

### Long-Term Roadmap
- [ ] Background automatic sync
- [ ] Secure notes support
- [ ] Credit card storage
- [ ] Identity information storage
- [ ] File attachments
- [ ] Password health analysis
- [ ] Breach monitoring
- [ ] Shared passwords (family/team)
- [ ] Import/Export functionality
- [ ] Multi-language support
- [ ] Widgets for quick access
- [ ] Apple Watch / Wear OS support

## 📞 Support

### Getting Help
- **📖 Documentation**: Check this guide and other docs in the repository
- **🐛 Bug Reports**: [Open an issue](https://github.com/dotnetappdev/PasswordManagerApp/issues) on GitHub
- **💬 Discussions**: [Join the discussion](https://github.com/dotnetappdev/PasswordManagerApp/discussions)
- **📧 Email**: Contact support@passwordmanager.dev

### Contributing
We welcome contributions! See [DEVELOPMENT.md](DEVELOPMENT.md) for contribution guidelines.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

**Built with ❤️ using Uno Platform and .NET 9**

*Secure, beautiful, cross-platform password management for iOS, Android, and beyond.*
