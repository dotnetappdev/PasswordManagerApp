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