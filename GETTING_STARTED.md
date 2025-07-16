# Getting Started with Password Manager

## Prerequisites

- **.NET 9 SDK** or later
- **Visual Studio 2024** or **JetBrains Rider** (recommended)
- **Git** for version control

> **Note:** This project uses .NET 9 for the latest features and performance improvements. Please ensure you have the .NET 9 SDK installed to build and run the solution.

## Installation

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

## Quick Start

### Web App Access
- **URL**: `https://localhost:5001` (or the URL shown in the terminal)
- **Features**: Full password management, API key generation, settings
- **Authentication**: Master password (same as mobile app)
- **Theme**: Dark mode only for professional appearance

### Mobile Application (MAUI)
1. Launch the mobile application on your device
2. Create a strong master password on first launch
3. Optionally set a password hint for recovery
4. Start adding your passwords and secure data using the same UI as the web app

## Database Configuration

The application supports multiple database providers. Set the `DatabaseProvider` in `appsettings.json`:

### SQLite (Default)
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

### SQL Server
```json
{
  "DatabaseProvider": "sqlserver",
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=PasswordManager;Trusted_Connection=true;TrustServerCertificate=true;"
  }
}
```

### PostgreSQL
```json
{
  "DatabaseProvider": "postgresql",
  "ConnectionStrings": {
    "PostgresConnection": "Host=localhost;Database=passwordmanager;Username=postgres;Password=yourpassword"
  }
}
```

### MySQL
```json
{
  "DatabaseProvider": "mysql",
  "ConnectionStrings": {
    "MySqlConnection": "Server=localhost;Database=PasswordManager;User=root;Password=yourpassword;Port=3306;"
  }
}
```

For detailed MySQL setup instructions, see the [MySQL Setup Guide](MYSQL_SETUP_GUIDE.md).

## First Steps

1. **Create your first account** through the web app or mobile app
2. **Set up a strong master password** - this is the key to your vault
3. **Generate API keys** (if needed) through the web app settings
4. **Import existing passwords** using the import feature
5. **Start organizing** your passwords with collections and categories

## Next Steps

- Review the [Development Guide](DEVELOPMENT.md) if you plan to contribute
- Check the [User Guide](USER_GUIDE.md) for detailed usage instructions
- Read about [Security & Encryption](ENCRYPTION_IMPLEMENTATION.md) to understand how your data is protected
