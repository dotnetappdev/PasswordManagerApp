# Setup Guide

Welcome to the Password Manager setup guide! This document will walk you through setting up your development environment, configuring databases, and getting the application running.

📖 **Back to [Main README](README.md)**

## Table of Contents
- [Prerequisites](#prerequisites)
- [Initial Setup](#initial-setup)
- [Database Configuration](#database-configuration)
- [Application Settings](#application-settings)
- [Running Migrations](#running-migrations)
- [Running the Application](#running-the-application)
- [Development Setup](#development-setup)
- [How to Contribute](#how-to-contribute)
- [Troubleshooting](#troubleshooting)

## Prerequisites

Before you begin, ensure you have the following installed:

- **.NET 9 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Visual Studio 2024** or **JetBrains Rider** (recommended IDEs)
- **Git** for version control
- **Database Server** (choose one):
  - **SQLite** (default, no setup required)
  - **SQL Server** (local or remote)
  - **MySQL** (local or remote)
  - **PostgreSQL** (local or remote)
  - **Supabase** (cloud PostgreSQL)

### Verify .NET Installation
```bash
dotnet --version
# Should show 9.0.x or higher
```

## Initial Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/dotnetappdev/PasswordManagerApp.git
   cd PasswordManagerApp
   ```

2. **Restore NuGet packages**
   ```bash
   dotnet restore
   ```

3. **Build the solution**
   ```bash
   dotnet build
   ```

## Database Configuration

The application supports multiple database providers. Choose the one that best fits your needs:

### SQLite (Recommended for Development)

SQLite is the default database provider and requires no additional setup.

**Configuration:**
```json
{
  "DatabaseProvider": "sqlite",
  "ConnectionStrings": {
    "SqliteConnection": "Data Source=passwordmanager.db"
  }
}
```

**Benefits:**
- ✅ No server setup required
- ✅ Portable database file
- ✅ Perfect for development and testing
- ✅ Automatically created on first run

### SQL Server

For production deployments or when you need enterprise features.

**Prerequisites:**
- SQL Server 2019 or later
- SQL Server Express (free) or full version

**Configuration:**
```json
{
  "DatabaseProvider": "sqlserver",
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=PasswordManager;Trusted_Connection=true;TrustServerCertificate=true;"
  }
}
```

**With Username/Password:**
```json
{
  "DatabaseProvider": "sqlserver",
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=PasswordManager;User Id=sa;Password=YourPassword;TrustServerCertificate=true;"
  }
}
```

### MySQL

Popular open-source database option.

**Prerequisites:**
- MySQL 8.0 or later
- MySQL Workbench (optional, for management)

**Installation:**
```bash
# Ubuntu/Debian
sudo apt install mysql-server

# macOS (using Homebrew)
brew install mysql

# Windows - Download from mysql.com
```

**Configuration:**
```json
{
  "DatabaseProvider": "mysql",
  "ConnectionStrings": {
    "MySqlConnection": "Server=localhost;Database=PasswordManager;User=root;Password=yourpassword;Port=3306;"
  }
}
```

**Setup Steps:**
1. Start MySQL service
2. Create database: `CREATE DATABASE PasswordManager;`
3. Create user (optional): `CREATE USER 'pmuser'@'localhost' IDENTIFIED BY 'password';`
4. Grant permissions: `GRANT ALL PRIVILEGES ON PasswordManager.* TO 'pmuser'@'localhost';`

📋 **For detailed MySQL setup, see [MySQL Setup Guide](MYSQL_SETUP_GUIDE.md)**

### PostgreSQL

Enterprise-grade open-source database.

**Prerequisites:**
- PostgreSQL 13 or later
- pgAdmin (optional, for management)

**Installation:**
```bash
# Ubuntu/Debian
sudo apt install postgresql postgresql-contrib

# macOS (using Homebrew)
brew install postgresql

# Windows - Download from postgresql.org
```

**Configuration:**
```json
{
  "DatabaseProvider": "postgresql",
  "ConnectionStrings": {
    "PostgresConnection": "Host=localhost;Database=passwordmanager;Username=postgres;Password=yourpassword;Port=5432;"
  }
}
```

**Setup Steps:**
1. Start PostgreSQL service
2. Create database: `createdb passwordmanager`
3. Create user (optional): `createuser -P pmuser`
4. Grant permissions: `GRANT ALL PRIVILEGES ON DATABASE passwordmanager TO pmuser;`

### Supabase (Cloud PostgreSQL)

Cloud-hosted PostgreSQL with additional features.

**Prerequisites:**
- Supabase account (free tier available)
- Project created in Supabase dashboard

**Configuration:**
```json
{
  "DatabaseProvider": "supabase",
  "ConnectionStrings": {
    "PostgresConnection": "postgresql://postgres:[YOUR-PASSWORD]@db.[YOUR-PROJECT-REF].supabase.co:5432/postgres"
  },
  "Supabase": {
    "Url": "https://[YOUR-PROJECT-REF].supabase.co",
    "Key": "[YOUR-ANON-KEY]"
  }
}
```

**Setup Steps:**
1. Create project at [supabase.com](https://supabase.com)
2. Get connection string from project settings
3. Copy anon key from API settings
4. Configure connection string and Supabase settings

## Application Settings

The application uses `appsettings.json` for configuration. Here's a complete configuration example:

### Complete appsettings.json Example

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  },
  "AllowedHosts": "*",
  
  "DatabaseProvider": "sqlite",
  "ConnectionStrings": {
    "SqliteConnection": "Data Source=passwordmanager.db",
    "DefaultConnection": "Server=localhost;Database=PasswordManager;Trusted_Connection=true;TrustServerCertificate=true;",
    "MySqlConnection": "Server=localhost;Database=PasswordManager;User=root;Password=yourpassword;Port=3306;",
    "PostgresConnection": "Host=localhost;Database=passwordmanager;Username=postgres;Password=yourpassword;Port=5432;"
  },
  
  "JwtSettings": {
    "SecretKey": "your-256-bit-secret-key-here-must-be-at-least-32-characters-long",
    "Issuer": "PasswordManagerAPI",
    "Audience": "PasswordManagerApp",
    "ExpirationHours": 24
  },
  
  "Encryption": {
    "DefaultIterations": 600000,
    "SaltLength": 32,
    "KeyLength": 32
  },
  
  "Supabase": {
    "Url": "https://[YOUR-PROJECT-REF].supabase.co",
    "Key": "[YOUR-ANON-KEY]"
  },
  
  "CORS": {
    "AllowedOrigins": [
      "https://localhost:5001",
      "https://localhost:7001"
    ]
  }
}
```

### Environment-Specific Configuration

Create environment-specific configuration files:

**appsettings.Development.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  },
  "DatabaseProvider": "sqlite",
  "ConnectionStrings": {
    "SqliteConnection": "Data Source=passwordmanager-dev.db"
  }
}
```

**appsettings.Production.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "DatabaseProvider": "sqlserver",
  "ConnectionStrings": {
    "DefaultConnection": "Server=production-server;Database=PasswordManager;User Id=pmuser;Password=production-password;"
  }
}
```

## Running Migrations

Database migrations are handled by Entity Framework Core. Follow these steps:

### 1. Navigate to the API Project
```bash
cd PasswordManager.API
```

### 2. Add Migration (when creating new migrations)
```bash
dotnet ef migrations add InitialCreate --project ../PasswordManager.DAL --startup-project .
```

### 3. Update Database
```bash
dotnet ef database update --project ../PasswordManager.DAL --startup-project .
```

### 4. Provider-Specific Migrations

For different database providers, you may need to create provider-specific migrations:

**SQL Server:**
```bash
dotnet ef migrations add InitialCreate --project ../PasswordManager.DAL.SqlServer --startup-project .
dotnet ef database update --project ../PasswordManager.DAL.SqlServer --startup-project .
```

**MySQL:**
```bash
dotnet ef migrations add InitialCreate --project ../PasswordManager.DAL.MySql --startup-project .
dotnet ef database update --project ../PasswordManager.DAL.MySql --startup-project .
```

**PostgreSQL:**
```bash
dotnet ef migrations add InitialCreate --project ../PasswordManager.DAL.Postgres --startup-project .
dotnet ef database update --project ../PasswordManager.DAL.Postgres --startup-project .
```

### 5. Migration Commands Reference

```bash
# List migrations
dotnet ef migrations list --project ../PasswordManager.DAL --startup-project .

# Remove last migration
dotnet ef migrations remove --project ../PasswordManager.DAL --startup-project .

# Generate SQL script
dotnet ef migrations script --project ../PasswordManager.DAL --startup-project .

# Update to specific migration
dotnet ef database update MigrationName --project ../PasswordManager.DAL --startup-project .
```

## Running the Application

### Development Mode

1. **Start the API (Terminal 1):**
   ```bash
   cd PasswordManager.API
   dotnet run
   ```
   API will be available at: `https://localhost:5001`

2. **Start the Web App (Terminal 2):**
   ```bash
   cd PasswordManager.Web
   dotnet run
   ```
   Web app will be available at: `https://localhost:7001`

3. **Start the MAUI App (Terminal 3):**
   ```bash
   cd PasswordManager.App
   dotnet run
   ```

### Production Mode

1. **Publish the API:**
   ```bash
   cd PasswordManager.API
   dotnet publish -c Release -o ./publish
   ```

2. **Publish the Web App:**
   ```bash
   cd PasswordManager.Web
   dotnet publish -c Release -o ./publish
   ```

3. **Run published applications:**
   ```bash
   # API
   cd PasswordManager.API/publish
   dotnet PasswordManager.API.dll

   # Web App
   cd PasswordManager.Web/publish
   dotnet PasswordManager.Web.dll
   ```

## Development Setup

### IDE Configuration

**Visual Studio 2024:**
1. Open `PasswordManager.sln`
2. Set multiple startup projects:
   - `PasswordManager.API`
   - `PasswordManager.Web`
3. Configure launch profiles for different environments

**JetBrains Rider:**
1. Open `PasswordManager.sln`
2. Create compound run configuration
3. Add both API and Web projects
4. Configure environment variables

### Development Tools

**Install useful tools:**
```bash
# Entity Framework tools
dotnet tool install --global dotnet-ef

# Code formatting
dotnet tool install --global dotnet-format

# Development certificates
dotnet dev-certs https --trust
```

### Hot Reload

Enable hot reload for faster development:
```bash
# API with hot reload
cd PasswordManager.API
dotnet watch run

# Web app with hot reload
cd PasswordManager.Web
dotnet watch run
```

## How to Contribute

We welcome contributions! Here's how to get started:

### 1. Fork and Clone
```bash
# Fork the repository on GitHub
git clone https://github.com/YOUR-USERNAME/PasswordManagerApp.git
cd PasswordManagerApp
git remote add upstream https://github.com/dotnetappdev/PasswordManagerApp.git
```

### 2. Create a Branch
```bash
git checkout -b feature/your-feature-name
```

### 3. Development Guidelines

**Code Style:**
- Follow C# coding conventions
- Use meaningful variable and method names
- Add XML documentation for public APIs
- Write unit tests for new features

**Commit Messages:**
- Use clear, descriptive commit messages
- Start with a verb (Add, Fix, Update, Remove)
- Reference issues when applicable

**Testing:**
```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test PasswordManager.Tests/
```

### 4. Submit Changes

1. **Push your changes:**
   ```bash
   git push origin feature/your-feature-name
   ```

2. **Create Pull Request:**
   - Go to GitHub and create a pull request
   - Describe your changes clearly
   - Reference any related issues
   - Ensure all tests pass

### 5. Code Review Process

- All pull requests require review
- Address feedback promptly
- Keep changes focused and atomic
- Update documentation as needed

### 6. Areas for Contribution

**High Priority:**
- Bug fixes and security improvements
- Performance optimizations
- Additional database providers
- Import/export features
- Mobile app enhancements

**Medium Priority:**
- UI/UX improvements
- Documentation updates
- Test coverage improvements
- Localization support

**Low Priority:**
- New features and enhancements
- Code refactoring
- Developer tooling improvements

## Troubleshooting

### Common Issues

**"Unable to connect to database"**
```bash
# Check connection string
dotnet ef database update --verbose

# Test connection
dotnet run --environment Development
```

**"Migration not found"**
```bash
# List migrations
dotnet ef migrations list

# Add initial migration
dotnet ef migrations add InitialCreate
```

**"Port already in use"**
```bash
# Change port in launchSettings.json
# Or kill process using port
lsof -ti:5001 | xargs kill -9
```

**"Certificate issues"**
```bash
# Trust development certificates
dotnet dev-certs https --trust

# Clear and regenerate
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

### Getting Help

- **Documentation**: Check our [documentation files](README.md)
- **Issues**: Create an issue on [GitHub](https://github.com/dotnetappdev/PasswordManagerApp/issues)
- **Discussions**: Join [GitHub Discussions](https://github.com/dotnetappdev/PasswordManagerApp/discussions)
- **Email**: Contact support@passwordmanager.dev

### Performance Tips

- Use SQLite for development
- Enable connection pooling for production
- Configure appropriate logging levels
- Use async/await properly
- Implement caching where appropriate

---

📖 **Back to [Main README](README.md)** | 📚 **See also:** [User Guide](USER_GUIDE.md) | [Development Guide](DEVELOPMENT.md) | [Technology Stack](TECHNOLOGY_STACK.md)

**Happy coding! 🚀**
