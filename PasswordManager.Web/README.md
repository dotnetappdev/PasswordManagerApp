# PasswordManager.Web

A modern Blazor Server web application for password management with a professional dark theme, built using .NET 9 and MudBlazor components.

## Features

- **🎨 Dark Theme Only**: Professional dark theme optimized for reduced eye strain
- **🔐 Master Password Authentication**: Secure vault unlock using Bitwarden-compatible encryption
- **📱 Responsive Design**: Works perfectly on desktop, tablet, and mobile browsers
- **🔑 API Key Management**: Built-in interface for generating and managing API keys
- **⚙️ Database Provider Selection**: Support for SQL Server, MySQL, PostgreSQL, and Supabase
- **🔍 Real-time Search**: Instant search across all password items
- **📁 Category Filtering**: Filter by categories, collections, and tags
- **📋 One-Click Copy**: Copy passwords to clipboard with visual feedback
- **✏️ Full CRUD Operations**: Create, edit, delete passwords and other items
- **🔄 Real-time Sync**: Changes sync automatically with mobile apps via API

## Technology Stack

- **.NET 9**: Latest version of Microsoft's unified development platform
- **Blazor Server**: Server-side rendering with SignalR for real-time updates
- **MudBlazor 7.20.0**: Material Design components for rich UI
- **ASP.NET Core Identity**: User authentication and authorization
- **Entity Framework Core 9.0.0**: Database operations with multiple provider support
- **Shared Components**: Reusable UI components shared with MAUI app

## Architecture

### Project Structure

```
PasswordManager.Web/
├── Components/
│   ├── App.razor              # Root application component
│   ├── Routes.razor           # Route configuration
│   └── _Imports.razor         # Global imports
├── Properties/
│   └── launchSettings.json    # Development settings
├── wwwroot/                   # Static files
├── appsettings.json           # Configuration
├── Program.cs                 # Application startup
└── PasswordManager.Web.csproj # Project file
```

### Shared Component Integration

The web app uses components from `PasswordManager.Components.Shared`:

- **Pages**: MasterPassword, Vault, Settings, Admin pages
- **Layout**: MainLayout, NavMenu, and responsive components
- **Auth**: Authentication and authorization components

### Database Provider Support

Supports multiple database providers through Entity Framework Core:

- **SQL Server**: With ASP.NET Core Identity
- **MySQL**: Using Pomelo.EntityFrameworkCore.MySql
- **PostgreSQL**: Using Npgsql.EntityFrameworkCore.PostgreSQL
- **Supabase**: Custom authentication integration

## Getting Started

### Prerequisites

- .NET 9 SDK
- Visual Studio 2024 or JetBrains Rider
- One of the supported databases (SQL Server, MySQL, PostgreSQL, or Supabase)

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/dotnetappdev/PasswordManagerApp.git
   cd PasswordManagerApp/PasswordManager.Web
   ```

2. **Configure database**
   
   Edit `appsettings.json` to configure your database provider:
   
   ```json
   {
     "DatabaseProvider": "sqlserver",
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=PasswordManager;Trusted_Connection=true;TrustServerCertificate=true;"
     }
   }
   ```

3. **Run database migrations**
   ```bash
   dotnet ef database update --startup-project ../PasswordManager.API
   ```

4. **Run the application**
   ```bash
   dotnet run
   ```

5. **Access the web app**
   - Open your browser and navigate to `https://localhost:5001`
   - Enter your master password to unlock the vault

## Configuration

### Database Providers

Configure in `appsettings.json`:

#### SQL Server (Default)
```json
{
  "DatabaseProvider": "sqlserver",
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=PasswordManager;Trusted_Connection=true;TrustServerCertificate=true;"
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

#### PostgreSQL
```json
{
  "DatabaseProvider": "postgresql",
  "ConnectionStrings": {
    "PostgresConnection": "Host=localhost;Database=passwordmanager;Username=postgres;Password=yourpassword"
  }
}
```

#### Supabase
```json
{
  "DatabaseProvider": "supabase",
  "ConnectionStrings": {
    "SupabaseConnection": "Host=db.xxx.supabase.co;Database=postgres;Username=postgres;Password=yourpassword;Port=5432;SSL Mode=Require;"
  }
}
```

### Authentication

The web app uses ASP.NET Core Identity for user management:

```csharp
// Program.cs
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
})
.AddEntityFrameworkStores<PasswordManagerDbContextApp>();
```

## Features

### Master Password Authentication

The web app implements the same secure authentication flow as the mobile app:

1. User enters master password
2. PBKDF2 key derivation (600,000 iterations)
3. Vault unlocked with derived encryption key
4. Session-based key caching for performance

### API Key Management

Generate and manage API keys for programmatic access:

1. Navigate to Settings
2. Enter a descriptive name for your API key
3. Click "Generate API Key"
4. Copy the generated key (shown only once)
5. Use the key for REST API access

### Password Management

Full CRUD operations for all password item types:

- **Login Items**: Username, password, website, notes
- **Credit Cards**: Card details with secure storage
- **Secure Notes**: Encrypted text notes
- **WiFi Credentials**: Network passwords and settings

### Search and Filtering

- **Real-time Search**: Search across all fields instantly
- **Category Filtering**: Filter by predefined categories
- **Collection Filtering**: Organize items in collections
- **Tag Filtering**: Multiple tag support with colors

## Security

### Encryption

- **AES-256-GCM**: Authenticated encryption for all sensitive data
- **PBKDF2**: 600,000 iterations for key derivation (OWASP 2024 recommendation)
- **Zero-Knowledge Architecture**: Server cannot decrypt data without master password
- **Session-Based Keys**: Encryption keys cached securely during session

### Authentication

- **ASP.NET Core Identity**: Industry-standard authentication
- **JWT Tokens**: Secure API access
- **Session Management**: Automatic session timeout
- **CSRF Protection**: Built-in protection against cross-site request forgery

### Data Protection

- **HTTPS Only**: All communications encrypted in transit
- **Secure Cookies**: HttpOnly and Secure flags set
- **Content Security Policy**: Protection against XSS attacks
- **Input Validation**: Comprehensive input sanitization

## Development

### Building

```bash
dotnet build
```

### Running in Development

```bash
dotnet run --environment Development
```

### Running Tests

```bash
dotnet test
```

### Adding New Features

1. Create components in `PasswordManager.Components.Shared` for shared functionality
2. Add web-specific components in `PasswordManager.Web/Components`
3. Update navigation in shared `NavMenu.razor`
4. Add necessary services to DI container in `Program.cs`

## Deployment

### IIS Deployment

1. Publish the application:
   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. Configure IIS with ASP.NET Core Hosting Bundle

3. Set up database connection string in production `appsettings.json`

### Docker Deployment

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["PasswordManager.Web/PasswordManager.Web.csproj", "PasswordManager.Web/"]
RUN dotnet restore "PasswordManager.Web/PasswordManager.Web.csproj"
COPY . .
WORKDIR "/src/PasswordManager.Web"
RUN dotnet build "PasswordManager.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PasswordManager.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PasswordManager.Web.dll"]
```

## Performance

- **Server-Side Rendering**: Blazor Server for optimal performance
- **SignalR**: Real-time updates without page refreshes
- **Lazy Loading**: Components loaded on-demand
- **Caching**: Intelligent caching strategies
- **Debounced Search**: Optimized search performance

## Browser Support

- **Chrome**: Latest version
- **Firefox**: Latest version
- **Safari**: Latest version
- **Edge**: Latest version
- **Mobile Browsers**: iOS Safari, Android Chrome

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Submit a pull request

## License

This project is licensed under the MIT License - see the [LICENSE](../LICENSE) file for details.

## Support

- 📧 Email: support@passwordmanager.dev
- 🐛 Issues: [GitHub Issues](https://github.com/dotnetappdev/PasswordManagerApp/issues)
- 💬 Discussions: [GitHub Discussions](https://github.com/dotnetappdev/PasswordManagerApp/discussions)

---

**Built with .NET 9, Blazor Server, and MudBlazor**
