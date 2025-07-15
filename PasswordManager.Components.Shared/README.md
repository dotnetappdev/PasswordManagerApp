# PasswordManager.Components.Shared

A shared Blazor component library that provides reusable UI components for both the .NET MAUI mobile app and Blazor Server web app, ensuring consistent user experience across all platforms.

## Features

- **Cross-Platform Components**: Shared between MAUI and Blazor Server apps
- **Dark Theme**: Professional dark theme using MudBlazor components
- **Responsive Design**: Adapts to different screen sizes and devices
- **Modular Architecture**: Reusable components with clear separation of concerns
- **Authentication Integration**: Built-in authentication and authorization components
- **Real-time Updates**: Live search and filtering capabilities

## Components

### Pages

#### MasterPassword.razor
- Master password authentication page
- Secure vault unlock functionality
- Password strength validation
- Biometric authentication support (mobile only)

#### Vault.razor
- Main password vault interface
- Card-based password item display
- Real-time search and filtering
- Category and collection navigation
- One-click password copying
- CRUD operations for all item types

#### Settings.razor
- Application settings management
- API key generation and management
- Database provider configuration
- User profile editing
- Import/export functionality

#### Admin/ (Administrative Components)
- User management components
- System configuration panels
- Database administration tools
- Security audit interfaces

### Shared Components

#### Auth/
- Login/logout components
- User profile management
- Session management
- JWT token handling

#### Layout/
- `MainLayout.razor`: Primary application layout with sidebar navigation
- `NavMenu.razor`: Navigation menu with collapsible sections
- `PageTitle.razor`: Dynamic page title management
- `ErrorBoundary.razor`: Error handling and display

## Architecture

### Component Sharing Strategy

The library uses a shared component approach where:

1. **Common UI Logic**: Shared between MAUI and Web apps
2. **Platform-Specific Overrides**: Each platform can override specific behaviors
3. **Dependency Injection**: Services are injected based on the hosting platform
4. **Responsive Design**: Components adapt to different screen sizes automatically

### Dependencies

- **MudBlazor**: Material Design components for rich UI
- **Microsoft.AspNetCore.Components**: Core Blazor functionality
- **Microsoft.AspNetCore.Components.Authorization**: Authentication components
- **PasswordManager.Models**: Shared data models
- **PasswordManager.Services**: Business logic services
- **PasswordManager.Crypto**: Cryptographic operations

## Usage

### In MAUI App

```csharp
// MauiProgram.cs
builder.Services.AddMauiBlazorWebView();
builder.Services.AddPasswordManagerComponents();
```

### In Blazor Server App

```csharp
// Program.cs
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddPasswordManagerComponents();
```

### Component Discovery

The library automatically discovers and registers routes for both platforms:

```csharp
// In App.razor or Routes.razor
<Router AppAssembly="@typeof(App).Assembly"
        AdditionalAssemblies="new[] { typeof(PasswordManager.Components.Shared.Pages.Vault).Assembly }">
    <Found Context="routeData">
        <RouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)" />
    </Found>
</Router>
```

## Development

### Building

```bash
dotnet build PasswordManager.Components.Shared
```

### Testing

```bash
dotnet test PasswordManager.Components.Shared.Tests
```

### Adding New Components

1. Create component in appropriate folder (`Pages/`, `Components/`, etc.)
2. Add necessary dependencies to `_Imports.razor`
3. Update navigation in `NavMenu.razor` if needed
4. Add any required services to DI container

## Security Features

- **Master Password Protection**: All sensitive operations require master password
- **Session Management**: Secure session handling with automatic timeouts
- **Memory Safety**: Sensitive data cleared from memory after use
- **CSRF Protection**: Built-in cross-site request forgery protection
- **Input Validation**: Comprehensive input validation and sanitization

## Responsive Design

Components automatically adapt to different screen sizes:

- **Desktop**: Full sidebar navigation with expanded menus
- **Tablet**: Collapsible navigation with touch-friendly controls
- **Mobile**: Drawer-based navigation optimized for mobile devices

## Performance

- **Lazy Loading**: Components loaded on-demand
- **Virtual Scrolling**: Efficient rendering of large lists
- **Caching**: Intelligent caching of frequently accessed data
- **Debounced Search**: Optimized search performance

## Customization

### Theming

The library uses MudBlazor's theming system:

```csharp
// Custom theme configuration
services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopCenter;
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = true;
});
```

### Component Overrides

Platform-specific overrides can be created:

```csharp
// MAUI-specific override
@inherits VaultBase
@if (DeviceInfo.Platform == DevicePlatform.iOS)
{
    // iOS-specific UI
}
else
{
    // Default UI
}
```

## Contributing

When adding new components:

1. Follow the existing component structure
2. Use MudBlazor components for consistency
3. Implement responsive design patterns
4. Add proper error handling
5. Include accessibility features
6. Update documentation

## License

This project is licensed under the MIT License - see the [LICENSE](../LICENSE) file for details.

---

**Part of the PasswordManager.App suite - Built with .NET 9, Blazor, and MudBlazor**
