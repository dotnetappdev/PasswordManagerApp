# Vault Guard WinUI App

This is a comprehensive WinUI 3 desktop application that provides the same functionality as the existing Blazor MAUI app using a native Windows interface.

[View Interface Screenshots](../screenshots.md) - See the WinUI application in action with detailed interface screenshots.

## Master Key Authentication

**Important**: This application supports master-key-only authentication. All seeded users (Admin, Parent, User, Child) use the same common master key for login:

**Common Master Key**: `CommonMaster123!`

This allows you to log in to any user level using just the master key, without needing to remember individual usernames and passwords. The seeder automatically sets up all user accounts with this common master key and proper master key identifiers for seamless authentication.

### User Accounts Created
- **Admin**: admin@passwordmanager.local (Role: Admin)
- **Parent**: parent@passwordmanager.local (Role: Parent)  
- **User**: user@passwordmanager.local (Role: User)
- **Child**: child@passwordmanager.local (Role: Child)

All accounts use the common master key `CommonMaster123!` for authentication.

### Authentication Methods

1. **Automatic User Detection**: The app automatically finds the correct user account based on the master key identifier
2. **Specific User Login**: Use `AuthenticateAsUserAsync(masterKey, email)` to login as a specific user role
3. **Available Users**: Use `GetAvailableUsersAsync()` to see all users that can be authenticated with the master key

### How It Works

- Each user has their own unique salt and master key identifier
- The common master key works with all users due to the master key identifier system
- Users maintain separate encrypted vaults despite sharing the master key
- Role-based permissions are enforced based on the authenticated user's role

## Features Implemented

### Core Architecture
- **Dependency Injection**: Full service container with all existing services from the Blazor app
- **Navigation**: Frame-based navigation between pages with service provider passing
- **Platform Services**: Windows-specific implementations (WinUiPlatformService, WinUiSecureStorageService)
- **Secure Storage**: Windows DPAPI-based secure storage service for encrypted local storage
- **Database Support**: Same database providers as original (SQLite, SQL Server, MySQL, PostgreSQL)
- **Service Integration**: Reuses all business logic from VaultGuard.Services

### Advanced User Interface
- **Modern Design**: Windows 11 Fluent Design with proper theming
- **Theme Support**: Light, Dark, and System theme modes with proper NavigationView integration
- **Responsive Layout**: Adaptive UI that works on different screen sizes
- **Navigation**: Professional sidebar navigation with content frame
- **Search & Filter**: Real-time search with type and category filtering
- **Context Menus**: Right-click operations for all password items
- **Loading States**: Progress indicators, empty states, and error handling

### Password Management
- **Full CRUD Operations**: Create, Read, Update, Delete password items
- **Multiple Types**: Support for Login, Credit Card, Secure Note, WiFi credentials
- **Advanced Dialog**: Comprehensive add/edit dialog with type-specific fields
- **Password Generation**: Built-in secure password generator
- **Categories & Collections**: Full organization support
- **Favorites**: Star system for important passwords
- **Search**: Real-time search across all password fields

### Data Architecture
- **MVVM Pattern**: Complete Model-View-ViewModel architecture
- **Data Binding**: Two-way data binding for all UI controls
- **Observable Collections**: Real-time UI updates when data changes
- **Value Converters**: Custom converters for type icons, visibility, etc.
- **Async Operations**: Proper async/await patterns throughout

## Project Structure

```
VaultGuard.WinUi/
├── App.xaml & App.xaml.cs          # Application entry point with DI container
├── MainWindow.xaml & .cs           # Main application window and navigation
├── Services/
│   ├── WinUiPlatformService.cs        # Windows-specific platform operations
│   ├── WinUiSecureStorageService.cs   # DPAPI-based secure storage
│   └── ThemeService.cs                # Theme management (Light/Dark/System)
├── Views/
│   ├── LoginPage                      # Professional authentication interface
│   ├── DashboardPage                  # Main dashboard with sidebar navigation
│   ├── PasswordItemsPage              # Advanced password management interface
│   ├── CategoriesPage                 # Visual category management
│   ├── SettingsPage                   # Comprehensive settings interface
│   └── ImportPage                     # Multi-provider import functionality
├── ViewModels/
│   └── PasswordItemsViewModel.cs      # MVVM data binding for password items
├── Converters/
│   └── ValueConverters.cs             # UI value converters (type→icon, bool→visibility)
├── Dialogs/
│   └── AddPasswordDialog              # Full-featured add/edit password dialog
├── Themes/
│   ├── ModernTheme.xaml               # Base modern theme resources
│   ├── LightTheme.xaml                # Light theme color palette
│   └── DarkTheme.xaml                 # Dark theme color palette
└── Assets/                         # Application icons and resources
```

## Dependencies & Integration

The WinUI app seamlessly integrates with existing shared libraries:
- **VaultGuard.Models**: All data models and entities
- **VaultGuard.Services**: Complete business logic layer
- **VaultGuard.DAL**: Database access with Entity Framework
- **VaultGuard.Crypto**: Encryption and security services
- **VaultGuard.Imports**: Import functionality for 1Password, Bitwarden, etc.

## Key Advantages Over Blazor Version

1. **Native Performance**: True native Windows application performance
2. **Windows Integration**: Deep integration with Windows 11 features
3. **XAML Power**: Rich, hardware-accelerated UI with smooth animations
4. **Offline First**: No dependency on web technologies
5. **Platform-Specific**: Leverages Windows-only features like DPAPI
6. **Memory Efficient**: Better memory management than web-based apps
7. **Familiar UX**: Native Windows user experience

## Features Demonstrated

### Password Management
- Create new password items with comprehensive form
- Edit existing items with pre-populated data
- Delete items with confirmation dialogs
- Search and filter by multiple criteria
- Context menus with copy, favorite, archive options
- Type-specific fields (Login, Credit Card, etc.)
- Password generation with cryptographically secure random
- Category and collection organization

### User Interface
- Professional login screen
- Sidebar navigation with dashboard
- Master-detail views
- Loading states and progress indicators
- Empty state handling
- Error dialogs and user feedback
- Modern Windows 11 styling
- Light/Dark/System theme support

### Technical Features
- Complete MVVM architecture
- Async/await patterns
- Data binding with INotifyPropertyChanged
- Service injection and dependency management
- Platform-specific service implementations
- Value converters for UI transformation

## WinUI 3 Compatibility

This project is fully compatible with WinUI 3 and does not use any WPF-style triggers that are unsupported in WinUI 3.

### What We Use (WinUI 3 Compatible)
- **Value Converters**: `BoolToVisibilityConverter`, `StringToVisibilityConverter`, etc.
- **Property Binding**: `IsEnabled="{Binding IsButtonEnabled}"`  
- **MVVM Pattern**: INotifyPropertyChanged for reactive UI
- **VisualStateManager**: For complex UI state management (see Examples/)

### What We DON'T Use (WPF-only)
- **Style.Triggers**: `<DataTrigger>`, `<EventTrigger>`, `<MultiTrigger>`
- **UpdateSourceTrigger**: Removed from bindings (not needed in WinUI)
- **WPF-specific syntax**: All patterns converted to WinUI equivalents

### Additional Resources
- See `WinUI-Compatibility-Guide.md` for detailed migration information
- See `Examples/VisualStateManagerExample.xaml` for VisualStateManager usage
- See `Tests/WinUiCompatibilityTests.cs` for compatibility validation

## Next Steps for Production

To complete for production use:

1. **Authentication**: Connect to IdentityAuthService for real authentication
2. **Database Setup**: Implement first-run database initialization
3. **Settings Integration**: Connect settings page to configuration services
4. **Import/Export**: Connect import page to actual import services
5. **Error Handling**: Add comprehensive error handling and logging
6. **Accessibility**: Add full accessibility support
7. **Testing**: Unit and integration tests
8. **Packaging**: MSIX packaging for Microsoft Store
9. **Documentation**: User documentation and help system
10. **Localization**: Multi-language support

## Requirements

- **OS**: Windows 10 version 19041+ or Windows 11
- **Runtime**: .NET 9.0
- **Framework**: Windows App SDK 1.6+
- **IDE**: Visual Studio 2022 or VS Code with C# Dev Kit

## Running the Application

```bash
# Navigate to the project directory
cd VaultGuard.WinUi

# Run the application
dotnet run
```

The application provides a complete password management experience with all the features of the original Blazor app, optimized for Windows desktop users with native performance and Windows 11 design language.

### Packaged vs Unpackaged Mode

The WinUI application supports both packaged (MSIX) and unpackaged execution modes:

#### Packaged Mode (MSIX)
- App runs in a sandboxed environment
- Default app data location: `Windows.Storage.ApplicationData.Current.LocalFolder.Path` (typically `%LocalAppData%\Packages\[PackageId]\LocalState\`)
- Deployed via MSIX installer or Microsoft Store
- Launch Profile: "VaultGuard.WinUi (Package)"

#### Unpackaged Mode
- App runs with traditional desktop permissions
- Default app data location: `%LocalAppData%\VaultGuard`
- Deployed as standalone executable
- Launch Profile: "VaultGuard.WinUi (Unpackaged)"
- Requires `trustInfo` section in app.manifest for proper execution

#### Custom Database Path

**Users can dictate the database location** regardless of packaging mode:

1. On first run, the application shows a database configuration dialog
2. Users can:
   - Accept the default location (based on packaging mode)
   - Browse to select a custom folder location
   - Manually enter a custom database path
3. The selected path is saved in `appsettings.json` in the app data directory
4. The application will use the configured path on subsequent launches

**Path Restrictions** (for security):
- Cannot use system directories (Windows, System32, Program Files)
- Cannot use UNC/network paths
- Must be accessible with current user permissions

**To Change Database Location After First Run**:
- Delete or edit `appsettings.json` in your app data directory
- Or use the Settings page to reconfigure the database location

**Note**: The application automatically detects the execution mode at runtime using `Windows.ApplicationModel.Package.Current` and provides appropriate default paths, but **always respects user-configured custom paths** stored in the configuration.

## Interface Screenshots

For a visual tour of the WinUI application interface, including screenshots of all major screens and features, see the [WinUI Screenshots Documentation](../screenshots.md).

The screenshots showcase:
- Modern Windows 11 interface design
- Native desktop application performance
- Professional password management features
- Comprehensive user interface elements
- Windows platform integration features