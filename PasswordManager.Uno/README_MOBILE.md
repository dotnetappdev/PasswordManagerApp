# Password Manager - Uno Platform Mobile App

## Overview

This is a cross-platform mobile application built with Uno Platform that provides secure password management with offline-first capabilities. The app synchronizes with the Password Manager API to keep passwords available across devices.

## Features

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
PasswordManager.Uno/
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

### Modern Material Design
- Material Design 3 components via Uno.Toolkit
- Card-based layouts for list items
- Swipe gestures for quick actions
- Pull-to-refresh for sync
- Floating action buttons
- Bottom navigation bar

### Responsive Layouts
- Optimized for phones and tablets
- Adaptive layouts using Material Design spacing
- Support for different screen sizes and orientations

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
