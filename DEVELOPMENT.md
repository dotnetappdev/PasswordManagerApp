# Development Guide

## Building from Source

### Prerequisites
- .NET 10 SDK (10.0.301 or newer; pinned in `global.json`)
- Visual Studio 2026 or JetBrains Rider
- Git
- For mobile development:
  - Android SDK (for Android)
  - Xcode 16+ (for iOS, on macOS)
  - MAUI workloads: `dotnet workload install maui`

### Build Commands
```bash
# Build all projects
dotnet build

# Build specific projects
dotnet build VaultGuard.Web          # Web application
dotnet build VaultGuard.Uno          # Uno Platform mobile app
dotnet build VaultGuard.Components.Shared  # Shared components

# Build Uno Platform for specific platforms
cd VaultGuard.Uno
dotnet build -f net10.0-android           # Android
dotnet build -f net10.0-ios               # iOS (macOS only)
dotnet build -f net10.0-browserwasm       # WebAssembly
dotnet build -f net10.0-desktop           # Desktop (Windows/macOS/Linux)

# Run tests
dotnet test
```

### Running in Development Mode
```bash
# Start the API server
cd VaultGuard.API
dotnet run

# Start the web application (new terminal)
cd VaultGuard.Web
dotnet run

# Run the Uno Platform mobile app (new terminal)
cd VaultGuard.Uno
dotnet run -f net10.0-android    # For Android emulator
dotnet run -f net10.0-ios         # For iOS simulator (macOS)
dotnet run -f net10.0-browserwasm # For WebAssembly in browser
```

## Project Structure

```
VaultGuardApp/
├── VaultGuard.API/              # ASP.NET Core Web API
├── VaultGuard.Web/              # Blazor Server Web Application
├── VaultGuard.WinUi/            # WinUI 3 Desktop Application
├── VaultGuard.Uno/              # Uno Platform Mobile App (iOS/Android/WASM)
├── VaultGuard.Components.Shared/ # Shared Blazor Component Library
├── VaultGuard.BrowserExtension/ # Cross-browser extension
├── VaultGuard.Crypto/           # Cryptography library
├── VaultGuard.DAL/              # Data Access Layer
├── VaultGuard.DAL.SqlServer/    # SQL Server provider
├── VaultGuard.DAL.MySql/        # MySQL provider
├── VaultGuard.DAL.Postgres/     # PostgreSQL provider
├── VaultGuard.DAL.SupaBase/     # Supabase provider
├── VaultGuard.Models/           # Shared models and DTOs
├── VaultGuard.Services/         # Business logic services
├── VaultGuard.Imports/          # Import framework
├── VaultGuardImports.1Password/ # 1Password import plugin
└── VaultGuardImports.Bitwarden/ # Enhanced secure import plugin
```

## Database Development

### Entity Framework Migrations
```bash
# Add a new migration
cd VaultGuard.DAL
dotnet ef migrations add MigrationName --startup-project ../VaultGuard.API

# Update database
dotnet ef database update --startup-project ../VaultGuard.API

# Generate SQL script
dotnet ef migrations script --startup-project ../VaultGuard.API
```

### Database Providers
Each database provider has its own project:
- **VaultGuard.DAL.SqlServer**: SQL Server support
- **VaultGuard.DAL.MySql**: MySQL support with Pomelo provider
- **VaultGuard.DAL.Postgres**: PostgreSQL support with Npgsql
- **VaultGuard.DAL.SupaBase**: Supabase cloud database support

## Creating Import Plugins

1. Create a new class library project
2. Reference `VaultGuard.Imports`
3. Implement `IPasswordImportProvider`
4. Register the plugin in your application

Example:
```csharp
public class CustomImportProvider : IPasswordImportProvider
{
    public string Name => "Custom Provider";
    public string Description => "Import from custom format";
    
    public async Task<List<PasswordItem>> ImportAsync(Stream data, IProgress<int> progress)
    {
        // Implementation
    }
}
```

## Testing

### Unit Tests
```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test VaultGuard.Crypto.Tests/
```

### Integration Tests
The solution includes integration tests for:
- API endpoints
- Database operations
- Encryption/decryption workflows
- Import functionality

## Code Style

### C# Conventions
- Use meaningful variable names
- Follow Microsoft C# coding conventions
- Use nullable reference types where appropriate
- Add XML documentation comments for public APIs

### Blazor Components
- Use meaningful component names
- Implement proper parameter validation
- Handle async operations properly
- Use proper disposal patterns

## Debugging

### Web Application
- Use browser developer tools for client-side debugging
- Check browser console for JavaScript errors
- Use Visual Studio debugger for server-side code

### MAUI Application
- Use Visual Studio debugger
- Check device logs for platform-specific issues
- Use Hot Reload for faster development

### API Development
- Use Swagger UI for API testing: `https://localhost:5001/swagger`
- Check API logs for authentication issues
- Test with Postman or similar tools

## Contributing

### Development Guidelines
- Follow C# coding conventions
- Write unit tests for new features
- Update documentation for API changes
- Use meaningful commit messages

### Pull Request Process
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Write tests for new functionality
5. Update documentation
6. Submit pull request

### Code Review
- All code changes require review
- Tests must pass before merge
- Documentation must be updated
- Breaking changes require version bump

## Architecture

### Shared Component Library
The `VaultGuard.Components.Shared` project contains:
- **Pages**: Master password, vault, settings, and admin pages
- **Components**: Authentication, layout, and UI components
- **Services**: Shared business logic and data access patterns

### Security Architecture
- Zero-knowledge encryption
- PBKDF2 key derivation (600,000 iterations)
- AES-256-GCM authenticated encryption
- Session-based vault management

For detailed security information, see [docs/SECURITY.md](docs/SECURITY.md).

## Performance

### Database Optimization
- Use proper indexing for search queries
- Implement connection pooling
- Use async/await patterns
- Optimize Entity Framework queries

### UI Performance
- Use virtualization for large lists
- Implement proper loading states
- Use efficient data binding
- Minimize re-renders in Blazor components

## Deployment

### Web Application
- Configure appropriate database provider
- Set up SSL certificates
- Configure authentication settings
- Set up proper logging

### Mobile Application
- Configure platform-specific settings
- Set up code signing certificates
- Configure app store deployment
- Test on target devices

## Troubleshooting

### Common Issues
- **.NET SDK version**: Ensure .NET 10 SDK is installed
- **Database connection**: Check connection strings
- **Authentication**: Verify JWT configuration
- **CORS**: Check cross-origin settings for API calls

### Debug Tips
- Enable detailed logging in development
- Use SQL profiler for database issues
- Check browser network tab for API calls
- Use debugger breakpoints effectively
