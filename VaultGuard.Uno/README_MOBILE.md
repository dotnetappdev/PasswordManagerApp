# Vault Guard - Uno Platform Mobile App

## Overview

This is a cross-platform mobile application built with Uno Platform that provides secure password management with offline-first capabilities and a **1Password-inspired design**. The app synchronizes with the Vault Guard API to keep passwords available across devices while maintaining a beautiful, professional user interface.

## ✨ Key Features

### 🎨 1Password-Inspired Design
- **Modern Material Design 3** - Clean, professional interface with refined colors and typography
- **Polished UI Components** - Rounded cards (12px radius), smooth animations, and intuitive interactions
- **Professional Color Palette** - Blue accent (#0066FF) with carefully selected supporting colors
- **Consistent Spacing** - 20px padding, 12-20px spacing for visual harmony
- **Card-Based Layout** - Elevated cards with subtle borders and shadows
- **Smooth Animations** - Subtle transitions and micro-interactions for delightful UX

### Core Functionality
- ✅ **Secure Authentication** - Login with email and password
- ✅ **Offline-First Storage** - SQLite local database for passwords
- ✅ **Two-Way Sync** - Sync passwords between device and API
- ✅ **Password Management** - Create, view, edit, and delete passwords
- ✅ **Categories** - Organize passwords into categories
- ✅ **Search** - Quick search across all passwords
- ✅ **Favorites** - Mark important passwords for quick access

### Platform Support
- ✅ Android (net9.0-android)
- ✅ iOS (net9.0-ios) 
- ✅ WebAssembly (net9.0-browserwasm)
- ✅ Desktop (net9.0-desktop) - Windows, Linux, macOS

## Architecture

### Technology Stack
- **Framework**: Uno Platform 6.4.42
- **UI Framework**: WinUI 3 / Uno.WinUI
- **Navigation**: Uno.Extensions.Navigation
- **MVVM**: CommunityToolkit.Mvvm
- **Local Database**: SQLite (sqlite-net-pcl)
- **HTTP Client**: Microsoft.Extensions.Http
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection

### Project Structure

```
VaultGuard.Uno/
├── Presentation/
│   ├── Pages/
│   │   ├── Login/           # Login page and view model
│   │   ├── Passwords/       # Password list page
│   │   └── Categories/      # Categories management
│   └── Shell.xaml           # App shell with navigation
├── Services/
│   ├── LocalDatabase/       # SQLite database service
│   │   ├── LocalPasswordItem.cs
│   │   ├── LocalCategory.cs
│   │   └── LocalDatabaseService.cs
│   └── Sync/                # API sync service
│       └── SyncService.cs
├── Models/                  # Local models and DTOs
├── Assets/                  # App icons and splash screens
└── Platforms/               # Platform-specific code
    ├── Android/
    ├── iOS/
    ├── WebAssembly/
    └── Desktop/
```

## Local Database

### Database Schema

**LocalPasswordItem**
- `LocalId` (Primary Key, Auto Increment)
- `ServerId` (Server ID from API)
- `Title`, `Description`, `Type`
- `Website`, `Username`, `EncryptedPassword`
- `CategoryId`, `IsFavorite`, `IsArchived`, `IsDeleted`
- `CreatedAt`, `LastModified`, `LastAccessedAt`
- `NeedsSyncToServer`, `LastSyncedAt` (Sync tracking)

**LocalCategory**
- `LocalId` (Primary Key, Auto Increment)
- `ServerId` (Server ID from API)
- `Name`, `Description`, `IconName`
- `UserId`, `CreatedAt`, `LastModified`
- `NeedsSyncToServer`, `LastSyncedAt` (Sync tracking)

### Sync Strategy

The app uses a two-way sync strategy:

1. **Pull from Server** (`SyncFromServerAsync`)
   - Downloads all passwords and categories from API
   - Updates local database with server data
   - Marks items as synced

2. **Push to Server** (`SyncToServerAsync`)
   - Identifies items marked `NeedsSyncToServer`
   - Pushes local changes to API
   - Updates `ServerId` and clears sync flag

3. **Conflict Resolution**
   - Server data takes precedence during sync
   - Local changes are marked for push on next sync

## API Integration

### Configuration

Configure the API base URL in `appsettings.json`:

```json
{
  "ApiBaseUrl": "https://your-api-domain.com"
}
```

### Endpoints Used

- `POST /api/auth/login` - User authentication
- `GET /api/passworditems` - Fetch all password items
- `GET /api/categories` - Fetch all categories
- `POST /api/passworditems` - Create password item
- `PUT /api/passworditems/{id}` - Update password item
- `DELETE /api/passworditems/{id}` - Delete password item

## UI Design

### 1Password-Inspired Interface

The app features a polished, professional design inspired by 1Password:

#### Visual Elements
- **Brand Color**: `#0066FF` (Primary blue for accents, buttons, and highlights)
- **Card Backgrounds**: White cards with 12px corner radius and 1px borders
- **Typography**: 
  - Headings: 32px bold for page titles
  - Body: 15-16px regular/semi-bold
  - Captions: 13-14px for secondary text
- **Spacing**: Consistent 20px padding and 12-20px spacing between elements
- **Icons**: 48x56px circular/rounded square with colored backgrounds
- **Buttons**: 48-52px height with 12px corner radius

#### Page Designs

**Login Page**:
- Centered layout with 80x80px branded icon
- Clean input fields (48px height)
- Primary blue button with white text
- Optional biometric login toggle
- Professional error messages with icons

**Passwords Page**:
- 32px bold page title with item count
- Search bar with rounded 12px corners
- Card-based password list with 48px icons
- Swipe-to-delete gestures
- Floating action button (56x56px) in brand blue
- Empty states with friendly messaging

**Categories Page**:
- Card-based layout with colored category icons
- Item count badges in brand blue
- Professional spacing and typography

**Settings Page**:
- Grouped settings with section headers (18px semi-bold)
- Toggle switches for preferences
- Card-based layout with 20px padding
- Primary blue buttons for actions
- Branded app icon in About section

### Color Scheme

#### Light Theme
- **Background**: `#F7F8FA` (Light gray)
- **Card Background**: `#FFFFFF` (White)
- **Primary**: `#0066FF` (Brand blue)
- **Text Primary**: `#1A1A1A` (Near black)
- **Text Secondary**: `#6B6B6B` (Gray)
- **Border**: `#E0E0E0` (Light gray)
- **Success**: `#34C759` (Green)
- **Error**: `#E85C5C` (Red)

#### Dark Theme
- **Background**: `#1A1A1A` (Dark gray)
- **Card Background**: `#2A2A2A` (Elevated surface)
- **Primary**: `#0066FF` (Brand blue - unchanged)
- **Text Primary**: `#FFFFFF` (White)
- **Text Secondary**: `#B0B0B0` (Light gray)
- **Border**: `#3A3A3A` (Dark border)

## Building the App

### Prerequisites
- .NET 9.0 SDK
- Uno Platform workload for .NET: `dotnet workload install android`
- For iOS: macOS with Xcode
- For Android: Android SDK

### Build Commands

**Android**
```bash
dotnet build -f net9.0-android
```

**iOS** (macOS only)
```bash
dotnet build -f net9.0-ios
```

**WebAssembly**
```bash
dotnet build -f net9.0-browserwasm
```

**Desktop**
```bash
dotnet build -f net9.0-desktop
```

### Run Commands

**Android Emulator**
```bash
dotnet run -f net9.0-android
```

**WebAssembly (Development Server)**
```bash
dotnet run -f net9.0-browserwasm
```

## Development

### Adding New Pages

1. Create page in `Presentation/Pages/{PageName}/`
2. Add `{PageName}Model.cs` (ViewModel)
3. Add `{PageName}Page.xaml` and `.xaml.cs`
4. Register in `App.xaml.cs`:
   ```csharp
   views.Register(
       new ViewMap<YourPage, YourModel>()
   );
   
   routes.Register(
       new ("YourRoute", View: views.FindByViewModel<YourModel>())
   );
   ```

### Adding Services

1. Create service in `Services/` directory
2. Register in `App.xaml.cs` `ConfigureServices`:
   ```csharp
   services.AddSingleton<IYourService, YourService>();
   ```

## Security

### Data Protection
- Passwords stored encrypted in SQLite database
- Database file location: `{LocalApplicationData}/passwordmanager.db3`
- JWT token-based authentication with API
- Secure HTTPS communication

### Best Practices
- Never log sensitive data
- Use secure storage for tokens
- Implement app-level encryption for sensitive fields
- Regular security audits

## Testing

### Manual Testing Checklist
- [ ] Login with valid credentials
- [ ] Sync passwords from server
- [ ] Create new password offline
- [ ] Edit existing password
- [ ] Delete password
- [ ] Search passwords
- [ ] Toggle favorites
- [ ] Browse categories
- [ ] Sync changes to server

## Known Limitations

1. **iOS Build**: Requires macOS for compilation
2. **Encryption**: Currently uses API-provided encrypted passwords, consider adding client-side encryption
3. **Offline Changes**: Require manual sync initiation
4. **Attachments**: Not yet supported in mobile app

## Future Enhancements

- [ ] Biometric authentication (Face ID, Fingerprint)
- [ ] Auto-sync with background tasks
- [ ] Password generator
- [ ] Secure notes
- [ ] Credit card storage
- [ ] Import/Export functionality
- [ ] Shared passwords (family/team)
- [ ] Password health check
- [ ] Dark mode support
- [ ] Localization (multiple languages)

## Support

For issues and questions:
- Check existing documentation
- Review API documentation
- Open an issue on GitHub

## License

[Your License Here]
