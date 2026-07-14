# Database & Migrations Guide

Consolidated database-provider, EF Core Identity setup, MySQL setup and migration documentation. Each section was previously a standalone root-level document.

## Contents
- Database Provider Plugin Architecture for VaultGuardApp
- Entity Framework Identity Setup and Migration Guide
- MySQL Database Setup Guide
- Vault Guard Migration Guide


---

<!-- merged from ReadMe.DatabaseProviders.md -->

# Database Provider Plugin Architecture for VaultGuardApp

This document describes how to support multiple database providers (MySQL, SQL Server, PostgreSQL, and SQLite) in the VaultGuardApp using a plugin-like architecture with Entity Framework Core.

## Overview
- The API and App projects will have a configuration option in `appsettings.json` to select the database provider.
- SQLite will always be available for local/offline use.
- Other providers (MySQL, SQL Server, PostgreSQL) are supported via separate context factories and migrations, each in their own class library (DLL).
- The main API loads the correct provider/factory at runtime based on configuration.

## Structure
- `VaultGuard.DAL.SqlServer/` — Contains context factory and migrations for SQL Server
- `VaultGuard.DAL.MySql/` — Contains context factory and migrations for MySQL
- `VaultGuard.DAL.Postgres/` — Contains context factory and migrations for PostgreSQL
- `VaultGuard.DAL.SupaBase/` — Contains context factory and migrations for Supabase (Postgres)
- `VaultGuard.DAL/` — Contains context factory and migrations for SQLite (default)

## Configuration
In both `VaultGuard.API/appsettings.json` and `VaultGuard.App/appsettings.json`:

```json
{
  "DatabaseProvider": "SqlServer", // or "MySql", "Postgres", "Supabase", "Sqlite"
  "ConnectionStrings": {
    "SqlServerConnection": "...",
    "MySqlConnection": "...",
    "PostgresConnection": "...",
    "SqliteConnection": "..."
  },
  "Supabase": {
    "Url": "https://your-project.supabase.co",
    "ApiKey": "your-supabase-api-key"
  }
}
```

## Usage
- The API reads `DatabaseProvider` from configuration and loads the corresponding context factory and migrations.
- Each provider DLL is referenced by the API, but only the selected one is used at runtime.
- Migrations are managed per-provider in their respective projects.

## Adding a New Provider
1. Create a new DAL project for the provider (e.g., `VaultGuard.DAL.Oracle`).
2. Implement a context factory and add migrations.
3. Update the API to recognize the new provider in configuration.

## Example
See the main repo for code samples and further details.

---

## Migrations: How To

Each provider has its own migrations. Use the following commands from the root of the repo or the provider's DAL directory:

### SQLite (default)
```
dotnet ef migrations add <MigrationName> --project VaultGuard.DAL --startup-project VaultGuard.API
```

### SQL Server
```
dotnet ef migrations add <MigrationName> --project VaultGuard.DAL.SqlServer --startup-project VaultGuard.API --context VaultGuard.DAL.SqlServer.SqlServerContextFactory
```

### MySQL
```
dotnet ef migrations add <MigrationName> --project VaultGuard.DAL.MySql --startup-project VaultGuard.API --context VaultGuard.DAL.MySql.MySqlContextFactory
```

### PostgreSQL
```
dotnet ef migrations add <MigrationName> --project VaultGuard.DAL.Postgres --startup-project VaultGuard.API --context VaultGuard.DAL.Postgres.PostgresContextFactory
```


### Supabase (Postgres)
```
dotnet ef migrations add <MigrationName> --project VaultGuard.DAL.SupaBase --startup-project VaultGuard.API --context SupabaseDbContext
```

### Applying Migrations
- Migrations are applied automatically at runtime by the API startup code.
- To apply manually, use:
```
dotnet ef database update --project <ProviderProject> --startup-project VaultGuard.API
```
Replace `<ProviderProject>` with the appropriate DAL project (e.g., `VaultGuard.DAL.SqlServer`, `VaultGuard.DAL.SupaBase`).


---

<!-- merged from EF_IDENTITY_SETUP_GUIDE.md -->

# Entity Framework Identity Setup and Migration Guide

## Overview

This guide explains how the Vault Guard application handles Entity Framework Identity tables creation, migration management, and database initialization to prevent the "SQLite Error 1: 'no such table: AspNetUsers'" issue.

## Problem Statement

The application may encounter the error: `SQLite Error 1: 'no such table: AspNetUsers'` when:
- The database exists but Identity tables are missing
- Migrations haven't been applied properly
- The database was created without proper Identity configuration

## Identity Configuration

### WinUI Application Identity Setup

The WinUI application (`VaultGuard.WinUi/App.xaml.cs`) uses full ASP.NET Core Identity with roles:

```csharp
// Add Identity services with roles so all Identity tables are created
services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
})
.AddEntityFrameworkStores<VaultGuardDbContextApp>()
.AddDefaultTokenProviders();
```

**Key Points:**
- Uses `AddIdentity<ApplicationUser, ApplicationRole>()` instead of `AddIdentityCore<ApplicationUser>()`
- Creates ALL Identity tables: AspNetUsers, AspNetRoles, AspNetUserRoles, AspNetUserClaims, etc.
- Enables role-based authorization and token providers

### Database Context Configuration

The `VaultGuardDbContextApp` inherits from `IdentityDbContext<ApplicationUser, ApplicationRole, string>`:

```csharp
public class VaultGuardDbContextApp : IdentityDbContext<ApplicationUser, ApplicationRole, string>, IVaultGuardDbContextApp
{
    // Use 'new' keyword to explicitly hide the inherited Users property from IdentityDbContext
    public new DbSet<ApplicationUser> Users { get; set; } = null!;
    
    // ... other DbSets
}
```

**Important:**
- The `new` keyword is used to hide the inherited `Users` property and eliminate build warnings
- The context automatically maps `ApplicationUser` to the `AspNetUsers` table
- All Identity tables are created through the base `IdentityDbContext` functionality

## Database Initialization Process

### AppStartupService Initialization Flow

The `AppStartupService.InitializeDatabaseAsync()` method handles database initialization with the following logic:

1. **Check Database Connectivity**
   ```csharp
   var canConnect = await dbContext.Database.CanConnectAsync();
   var canConnectApp = await dbContextApp.Database.CanConnectAsync();
   ```

2. **New Database Creation**
   - If database doesn't exist, use migrations first: `await dbContextApp.Database.MigrateAsync()`
   - Fallback to `EnsureCreatedAsync()` if migrations fail
   - Seed Identity data for new installations

3. **Identity Tables Check**
   ```csharp
   var identityTablesExist = await CheckIdentityTablesExistAsync(dbContextApp);
   ```

4. **Missing Identity Tables Recovery**
   - Apply migrations to create missing Identity tables
   - Verify tables were created successfully
   - Use `EnsureCreatedAsync()` as final fallback
   - Seed Identity data after table creation

### Identity Tables Detection

The `CheckIdentityTablesExistAsync()` method uses a reliable approach to detect missing Identity tables:

```csharp
private async Task<bool> CheckIdentityTablesExistAsync(VaultGuardDbContextApp dbContext)
{
    try
    {
        // Check if the database can be connected to first
        if (!await dbContext.Database.CanConnectAsync())
        {
            return false;
        }

        // Use a more reliable method to check for table existence in SQLite
        using var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync();
        
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='AspNetUsers'";
        var result = await command.ExecuteScalarAsync();
        var aspNetUsersExists = Convert.ToInt32(result) > 0;

        if (!aspNetUsersExists)
        {
            return false;
        }

        // Double-check by querying the Users table
        var userCount = await dbContext.Users.CountAsync();
        return true;
    }
    catch (Exception ex)
    {
        return false;
    }
}
```

**Key Features:**
- Checks `sqlite_master` table directly for reliable table detection
- Handles database connectivity issues gracefully
- Double-checks by attempting to query the Users table
- Provides comprehensive logging for troubleshooting

## Migration Management

### Available Migrations

The application includes several migrations that create and update the database schema:

1. **20250803101910_firstmigration.cs** - Creates initial Identity tables and application tables
2. **20250904175440_AddIdentityRoles.cs** - Adds Identity roles support
3. **20250904185307_AddMasterKeyIdentifier.cs** - Adds master key identifier for authentication
4. Additional migrations for features and schema updates

### Automatic Migration Application

For desktop applications (WinUI), migrations are applied automatically:

```csharp
var isDesktopApp = Environment.OSVersion.Platform == PlatformID.Win32NT && 
                  !Environment.GetCommandLineArgs().Any(arg => arg.Contains("server") || arg.Contains("web"));

if (pendingMigrations.Any() && isDesktopApp)
{
    await dbContext.Database.MigrateAsync();
}
```

### Manual Migration Commands

For development and troubleshooting, you can use these Entity Framework CLI commands:

```bash
# Check migration status
dotnet ef migrations list --project VaultGuard.DAL

# Add a new migration
dotnet ef migrations add MigrationName --project VaultGuard.DAL

# Apply pending migrations
dotnet ef database update --project VaultGuard.DAL

# Remove last migration (development only)
dotnet ef migrations remove --project VaultGuard.DAL
```

## Error Handling and Recovery

### WinUiAuthService Error Handling

The `WinUiAuthService` includes specific error handling for missing Identity tables:

```csharp
try
{
    await _dbContext.SaveChangesAsync();
}
catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.Message.Contains("no such table: AspNetUsers"))
{
    _logger.LogWarning("AspNetUsers table not found, attempting to create Identity tables");
    
    // Try to ensure Identity tables are created
    await _dbContext.Database.MigrateAsync();
    
    // Retry saving the user after migration
    await _dbContext.SaveChangesAsync();
}
```

### Common Error Scenarios and Solutions

#### 1. "no such table: AspNetUsers"
**Cause:** Identity tables missing from database
**Solution:** 
- The application automatically detects this and applies migrations
- If automatic recovery fails, manually run: `dotnet ef database update`

#### 2. "table Users has no column named MasterKeyIdentifier"
**Cause:** Missing recent migration with MasterKeyIdentifier column
**Solution:**
- Apply pending migrations: `dotnet ef database update`
- The application includes automatic retry logic for this scenario

#### 3. Migration conflicts or failures
**Cause:** Database schema conflicts or corrupted migration state
**Solution:**
- For development: Reset database and reapply all migrations
- For production: Manually resolve conflicts or restore from backup

## Best Practices

### Development Environment

1. **Always use migrations** for schema changes instead of modifying the database directly
2. **Test migrations** in a development environment before applying to production
3. **Keep migrations in source control** and apply them consistently across environments
4. **Use the DatabaseMigrationService** for programmatic migration management

### Production Environment

1. **Backup your database** before applying migrations in production
2. **Monitor application logs** for migration status and errors
3. **Use the web interface** at `/admin/migrations` to monitor migration health
4. **Test the application** after migration application to ensure functionality

### Troubleshooting

1. **Check application logs** for detailed error information and migration status
2. **Use the migration status API** to understand the current database state
3. **Verify database connectivity** before attempting migration operations
4. **Ensure proper permissions** for the application to modify the database schema

## API Endpoints for Migration Management

The application provides REST API endpoints for monitoring and managing migrations:

- `GET /api/migrations/status` - Get current migration status
- `POST /api/migrations/apply` - Apply pending migrations
- `GET /api/migrations/history` - Get migration application history
- `GET /api/database/health` - Check database health and connectivity

## Identity Data Seeding

The application includes automatic Identity data seeding through the `IdentityDataSeeder`:

```csharp
// Register Identity data seeder for proper Identity table initialization
services.AddScoped<VaultGuard.DAL.Seed.IdentityDataSeeder>();
```

The seeder:
- Creates default roles if they don't exist
- Sets up initial user data for fresh installations
- Runs automatically during database initialization
- Handles errors gracefully without stopping application startup

## Testing and Validation

### Validating Identity Setup

To verify that Identity tables are properly created and configured:

1. **Check database tables:** Ensure AspNetUsers, AspNetRoles, and related tables exist
2. **Test user creation:** Verify that the master password setup works correctly
3. **Test authentication:** Confirm that login functionality works as expected
4. **Monitor logs:** Check for any Identity-related errors during application startup

### Migration Testing

1. **Test with fresh database:** Verify that migrations create all required tables
2. **Test with existing database:** Ensure migrations update schema correctly
3. **Test migration rollback:** Verify that schema changes can be undone if needed
4. **Test data preservation:** Ensure that existing data is preserved during migrations

## Conclusion

This guide provides comprehensive information about Entity Framework Identity setup and migration management in the Vault Guard application. The application includes robust error handling, automatic recovery mechanisms, and comprehensive logging to prevent and resolve Identity table issues.

For additional support, refer to the application logs, use the migration management API endpoints, or consult the Entity Framework Core documentation for advanced scenarios.

---

<!-- merged from MYSQL_SETUP_GUIDE.md -->

# MySQL Database Setup Guide

## Overview
The Vault Guard API now supports MySQL as a database provider alongside SQL Server, PostgreSQL, and SQLite. This guide explains how to configure and use MySQL with the application.

## Prerequisites
- MySQL Server 8.0 or higher
- MySQL client/workbench for database management

## Configuration

### 1. Install MySQL Server
Follow the official MySQL installation guide for your operating system:
- **Windows**: Download from https://dev.mysql.com/downloads/installer/
- **Linux**: `sudo apt-get install mysql-server` (Ubuntu/Debian)
- **macOS**: `brew install mysql`

### 2. Create Database and User
Connect to MySQL as root and run the following commands:

```sql
-- Create database
CREATE DATABASE VaultGuardDB CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- Create user for the application
CREATE USER 'passwordmgr'@'localhost' IDENTIFIED BY 'your_secure_password';

-- Grant privileges
GRANT ALL PRIVILEGES ON VaultGuardDB.* TO 'passwordmgr'@'localhost';
FLUSH PRIVILEGES;
```

### 3. Update Configuration Files

#### appsettings.json
```json
{
  "DatabaseProvider": "mysql",
  "ConnectionStrings": {
    "MySqlConnection": "Server=localhost;Database=VaultGuardDB;User=passwordmgr;Password=your_secure_password;Port=3306;"
  }
}
```

#### appsettings.Development.json
```json
{
  "ConnectionStrings": {
    "MySqlConnection": "Server=localhost;Database=VaultGuardDB_Dev;User=passwordmgr;Password=dev_password;Port=3306;"
  }
}
```

## Connection String Parameters

| Parameter | Description | Example |
|-----------|-------------|---------|
| Server | MySQL server hostname/IP | `localhost`, `192.168.1.100` |
| Database | Database name | `VaultGuardDB` |
| User | MySQL username | `passwordmgr` |
| Password | User password | `your_secure_password` |
| Port | MySQL port (default: 3306) | `3306` |
| SslMode | SSL connection mode | `Required`, `Preferred`, `None` |
| CharSet | Character set | `utf8mb4` |

### Advanced Connection String Options
```
Server=localhost;Database=VaultGuardDB;User=passwordmgr;Password=your_password;Port=3306;SslMode=Required;CharSet=utf8mb4;AllowUserVariables=true;UseAffectedRows=false;
```

## Database Migration

### Initial Setup
```bash
# Navigate to API project
cd VaultGuard.API

# Add migration for MySQL
dotnet ef migrations add InitialCreate --context VaultGuardDbContext

# Update database
dotnet ef database update
```

### Provider-Specific Migrations
If you need separate migrations for different providers:

```bash
# MySQL-specific migration
dotnet ef migrations add MySqlInitial --context VaultGuardDbContext --output-dir Migrations/MySQL

# Apply migration
dotnet ef database update --context VaultGuardDbContext
```

## Performance Optimization

### MySQL Configuration
Add these settings to your MySQL configuration file (`my.cnf` or `my.ini`):

```ini
[mysqld]
# Character set and collation
character-set-server = utf8mb4
collation-server = utf8mb4_unicode_ci

# Performance settings
innodb_buffer_pool_size = 256M
innodb_log_file_size = 64M
max_connections = 200

# Security settings
sql_mode = STRICT_TRANS_TABLES,NO_ZERO_DATE,NO_ZERO_IN_DATE,ERROR_FOR_DIVISION_BY_ZERO
```

### Application-Level Optimizations
The application automatically optimizes queries for MySQL using Entity Framework Core's MySQL provider features.

## Supported Features

✅ **Full Encryption Support**: All password encryption with 600,000 PBKDF2 iterations  
✅ **User Authentication**: Identity management with MySQL backend  
✅ **Database Sync**: Multi-database synchronization including MySQL  
✅ **Migrations**: Automatic schema updates  
✅ **Transactions**: ACID compliance for data integrity  
✅ **Connection Pooling**: Built-in connection management  

## Troubleshooting

### Common Issues

1. **Connection Timeout**
   ```
   Solution: Increase connection timeout in connection string:
   Server=localhost;Database=VaultGuardDB;User=passwordmgr;Password=password;Port=3306;Connection Timeout=30;
   ```

2. **Character Encoding Issues**
   ```
   Solution: Ensure UTF-8 encoding:
   Server=localhost;Database=VaultGuardDB;User=passwordmgr;Password=password;Port=3306;CharSet=utf8mb4;
   ```

3. **SSL Connection Errors**
   ```
   Solution: Adjust SSL mode:
   Server=localhost;Database=VaultGuardDB;User=passwordmgr;Password=password;Port=3306;SslMode=None;
   ```

### Logging
Enable detailed logging in `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

## Security Considerations

1. **Use Strong Passwords**: Ensure MySQL user has a strong password
2. **Network Security**: Use SSL/TLS for production environments
3. **User Privileges**: Grant only necessary privileges to application user
4. **Regular Updates**: Keep MySQL server updated
5. **Backup Strategy**: Implement regular database backups

## Example Docker Setup

```yaml
version: '3.8'
services:
  mysql:
    image: mysql:8.0
    environment:
      MYSQL_ROOT_PASSWORD: rootpassword
      MYSQL_DATABASE: VaultGuardDB
      MYSQL_USER: passwordmgr
      MYSQL_PASSWORD: password
    ports:
      - "3306:3306"
    volumes:
      - mysql_data:/var/lib/mysql

volumes:
  mysql_data:
```

## Migration from Other Providers

To migrate from SQLite/SQL Server/PostgreSQL to MySQL:

1. Export existing data using the sync feature
2. Set up MySQL database
3. Update configuration to use MySQL
4. Run migrations
5. Import data using sync feature

This ensures all encrypted data remains intact during the migration process.


---

<!-- merged from MIGRATION_GUIDE.md -->

# Vault Guard Migration Guide

This guide helps you migrate your passwords from other password managers and browsers to Vault Guard App.

## 📋 Table of Contents
- [Supported Import Sources](#supported-import-sources)
- [Step-by-Step Migration Instructions](#step-by-step-migration-instructions)
- [Tips for a Smooth Migration](#tips-for-a-smooth-migration)
- [Troubleshooting](#troubleshooting)

## 🔄 Supported Import Sources

### Vault Guards
- **1Password** - CSV and 1PUX formats
- **Bitwarden** - CSV format
- **LastPass** - CSV format with folder preservation
- **Dashlane** - CSV format with categories
- **KeePass** - CSV format with groups

### Web Browsers
- **Google Chrome** - Password CSV export
- **Microsoft Edge** - Password CSV export
- **Mozilla Firefox** - Logins CSV export
- **Apple Safari** - Password CSV export

## 📖 Step-by-Step Migration Instructions

### From 1Password

1. **Export from 1Password**:
   - Open 1Password and log in
   - Go to **File** → **Export** → **All Items**
   - Choose either:
     - **1PUX** format (encrypted archive - recommended)
     - **CSV** format (unencrypted - less secure but simpler)
   - Save the file to a secure location

2. **Import to Vault Guard**:
   - Open Vault Guard App
   - Navigate to **Settings** → **Import/Export**
   - Click **Import Data**
   - Select **1Password** as the source
   - Choose your exported file
   - Review the preview (first 5 items)
   - Click **Import** to complete

3. **Verify Import**:
   - Check that all passwords were imported
   - Verify folders/collections are preserved
   - Test a few logins to ensure they work

4. **Secure Cleanup**:
   - Delete the exported CSV/1PUX file
   - Empty your trash/recycle bin

### From Bitwarden

1. **Export from Bitwarden**:
   - Log into Bitwarden (web vault or desktop app)
   - Go to **Tools** → **Export Vault**
   - Select **File Format**: `.csv`
   - Click **Export Vault**
   - Save the CSV file securely

2. **Import to Vault Guard**:
   - Open Vault Guard App
   - Go to **Settings** → **Import/Export**
   - Click **Import Data**
   - Select **Bitwarden** as the source
   - Browse to your CSV file
   - Preview the items
   - Click **Import**

3. **Post-Import**:
   - Verify all items imported correctly
   - Check that folders are preserved
   - Securely delete the exported CSV file

### From LastPass

1. **Export from LastPass**:
   - Log into LastPass (browser extension or web vault)
   - Click **Advanced Options** (⚙️ icon)
   - Select **Advanced** → **Export**
   - Your passwords will open in a new tab in CSV format
   - Save this as a CSV file (Ctrl+S or Cmd+S)

2. **Import to Vault Guard**:
   - Open Vault Guard App
   - Navigate to **Settings** → **Import/Export**
   - Click **Import Data**
   - Select **LastPass** as the source
   - Choose your CSV file
   - Review the preview
   - Click **Import**

3. **Note**: LastPass folders will be imported as collections

### From Dashlane

1. **Export from Dashlane**:
   - Open Dashlane desktop app
   - Go to **Settings** → **Export Data**
   - Choose **Unsecured archive (readable) in CSV**
   - Click **Export to CSV**
   - Save the file

2. **Import to Vault Guard**:
   - Open Vault Guard App
   - Go to **Settings** → **Import/Export**
   - Click **Import Data**
   - Select **Dashlane**
   - Browse to your CSV file
   - Preview and import

3. **Verify**: Categories from Dashlane become collections in Vault Guard

### From KeePass

1. **Export from KeePass**:
   - Open KeePass
   - Go to **File** → **Export**
   - Select **Generic CSV Exporter** (1.xx format)
   - Save the CSV file

2. **Import to Vault Guard**:
   - Open Vault Guard App
   - Go to **Settings** → **Import/Export**
   - Click **Import Data**
   - Select **KeePass**
   - Choose your CSV file
   - Review and import

3. **Note**: KeePass groups are preserved as collections

### From Google Chrome

1. **Export from Chrome**:
   - Open Chrome
   - Go to **Settings** (⚙️)
   - Click **Autofill and passwords** → **Google Vault Guard**
   - Click the ⚙️ (Settings) icon
   - Select **Export passwords**
   - Authenticate if prompted
   - Save the CSV file

2. **Import to Vault Guard**:
   - Open Vault Guard App
   - Go to **Settings** → **Import/Export**
   - Click **Import Data**
   - Select **Google Chrome**
   - Choose your CSV file
   - Import the passwords

3. **Security**: Delete the CSV file immediately after import

### From Microsoft Edge

1. **Export from Edge**:
   - Open Microsoft Edge
   - Go to **Settings** → **Profiles** → **Passwords**
   - Click the three dots (⋯) next to **Saved passwords**
   - Select **Export passwords**
   - Save the CSV file

2. **Import to Vault Guard**:
   - Follow the same process as Chrome import
   - Select **Microsoft Edge** as the source

### From Mozilla Firefox

1. **Export from Firefox**:
   - Open Firefox
   - Type `about:logins` in the address bar
   - Click the three dots menu (⋮)
   - Select **Export Logins**
   - Save the CSV file

2. **Import to Vault Guard**:
   - Open Vault Guard App
   - Go to **Settings** → **Import/Export**
   - Click **Import Data**
   - Select **Mozilla Firefox**
   - Choose your CSV file
   - Import

3. **Note**: Firefox timestamps are preserved during import

### From Apple Safari

1. **Export from Safari** (macOS):
   - Open Safari
   - Go to **Settings/Preferences**
   - Click **Passwords**
   - Authenticate with your Mac password
   - Click the three dots (⋯) menu
   - Select **Export Passwords**
   - Save the CSV file

2. **Import to Vault Guard**:
   - Open Vault Guard App
   - Go to **Settings** → **Import/Export**
   - Click **Import Data**
   - Select **Apple Safari**
   - Choose your CSV file
   - Review and import

## 💡 Tips for a Smooth Migration

### Before You Start
1. **Backup First**: Ensure your current password manager has a backup
2. **Clean Up**: Remove duplicate or unused passwords before exporting
3. **Update Weak Passwords**: Use this as an opportunity to strengthen weak passwords
4. **Organize**: Review your folder/collection structure

### During Migration
1. **Preview First**: Always review the import preview before committing
2. **Small Batches**: If you have many passwords, consider importing in smaller batches
3. **Verify Format**: Make sure the export file is in CSV format
4. **Check Encoding**: Ensure the CSV file is UTF-8 encoded

### After Migration
1. **Verify Everything**: Check that all items imported correctly
2. **Test Logins**: Test several logins to ensure passwords work
3. **Delete Exports**: Securely delete all exported CSV files
4. **Keep Both Active**: Keep your old password manager active for a few weeks as backup
5. **Update Browser Extension**: Install the Vault Guard browser extension
6. **Update Mobile Apps**: Install the mobile app on your devices

## 🔒 Security Best Practices

1. **Export File Security**:
   - Never email CSV files to yourself
   - Delete export files immediately after import
   - Don't save exports to cloud storage
   - Use encrypted USB drives for transfers if needed

2. **Verification**:
   - Check all critical passwords imported correctly
   - Verify 2FA codes if imported (from Safari)
   - Test password autofill in browser

3. **Cleanup**:
   - Clear browser downloads
   - Empty recycle bin
   - Run secure file deletion tools if concerned

## 🔧 Troubleshooting

### Import Fails

**Problem**: Import fails with error message

**Solutions**:
- Verify the CSV file isn't corrupted (open in text editor)
- Check that the file format matches the selected source
- Ensure the CSV is properly formatted (no missing columns)
- Try importing a smaller subset first

### Missing Passwords

**Problem**: Some passwords didn't import

**Solutions**:
- Check if they were in a special vault/folder
- Verify they were included in the export
- Try exporting and importing again
- Check import logs for skipped items

### Wrong Format Detection

**Problem**: The import tool doesn't recognize your file

**Solutions**:
- Verify you selected the correct source
- Check the file extension is `.csv`
- Open the CSV in a text editor and verify the header row
- Ensure the CSV uses UTF-8 encoding

### Duplicate Items

**Problem**: Some items were imported multiple times

**Solutions**:
- Use the duplicate detection feature
- Manually review and delete duplicates
- Re-import with "Skip Duplicates" option if available

### Special Characters Issues

**Problem**: Passwords with special characters are corrupted

**Solutions**:
- Ensure the CSV file is UTF-8 encoded
- Check that special characters are properly escaped
- Try re-exporting with different encoding options

## 📞 Getting Help

If you encounter issues not covered in this guide:

1. Check the [main README](ReadMe.md) for general documentation
2. Review the [User Guide](USER_GUIDE.md) for detailed usage instructions
3. Visit [GitHub Issues](https://github.com/dotnetappdev/VaultGuardApp/issues) to report problems
4. Join our community discussions for migration tips

## 🎯 Success Checklist

After migration, ensure you've completed:

- [ ] All passwords imported successfully
- [ ] Folders/collections are organized correctly
- [ ] Test logins work from Vault Guard
- [ ] Browser extension is installed and working
- [ ] Mobile apps are set up
- [ ] Export files are securely deleted
- [ ] Old password manager account is secure
- [ ] Master password is strong and unique

---

**Welcome to Vault Guard! 🎉**

You've successfully migrated your passwords. Enjoy secure, cross-platform password management with enterprise-grade encryption.
