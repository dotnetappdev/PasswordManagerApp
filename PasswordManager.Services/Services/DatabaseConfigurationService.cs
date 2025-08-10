using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Crypto.Interfaces;
using PasswordManager.Models.Configuration;
using PasswordManager.Services.Interfaces;

namespace PasswordManager.Services.Services;

/// <summary>
/// Service for managing database configuration with encryption
/// </summary>
public class DatabaseConfigurationService : IDatabaseConfigurationService
{
    private readonly IConfiguration _configuration;
    private readonly ICryptographyService _cryptographyService;
    private readonly IPlatformService _platformService;
    private readonly ILogger<DatabaseConfigurationService> _logger;
    private readonly string _configFilePath;
    private byte[]? _encryptionKey;
    private readonly SemaphoreSlim _keyInitLock = new(1, 1);

    public DatabaseConfigurationService(
        IConfiguration configuration,
        ICryptographyService cryptographyService,
        IPlatformService platformService,
        ILogger<DatabaseConfigurationService> logger)
    {
        _configuration = configuration;
        _cryptographyService = cryptographyService;
        _platformService = platformService;
        _logger = logger;
        
        // Get the path to the appsettings.json file in the app data directory
        _configFilePath = Path.Combine(
            _platformService.GetAppDataDirectory(), 
            "appsettings.json");

        // Note: Encryption key initialization is now lazy and async to prevent blocking the constructor
    }

    public async Task<DatabaseConfiguration> GetConfigurationAsync()
    {
        try
        {
            // Add timeout for file operations to prevent hanging
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            
            var configTask = Task.Run(async () =>
            {
                if (!File.Exists(_configFilePath))
                {
                    _logger.LogInformation("No database configuration file found, returning default configuration");
                    return GetDefaultConfiguration();
                }

                var jsonContent = await File.ReadAllTextAsync(_configFilePath, cts.Token);
                var config = JsonSerializer.Deserialize<DatabaseConfiguration>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return config ?? GetDefaultConfiguration();
            }, cts.Token);

            return await configTask;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("GetConfigurationAsync timed out, returning default configuration");
            return GetDefaultConfiguration();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading database configuration, returning default configuration");
            return GetDefaultConfiguration();
        }
    }

    public async Task SaveConfigurationAsync(DatabaseConfiguration configuration)
    {
        try
        {
            // Ensure the directory exists
            var directory = Path.GetDirectoryName(_configFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Mark as no longer first run
            configuration.IsFirstRun = false;

            var jsonContent = JsonSerializer.Serialize(configuration, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await File.WriteAllTextAsync(_configFilePath, jsonContent);
            
            _logger.LogInformation("Database configuration saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving database configuration");
            throw;
        }
    }

    public async Task<bool> IsFirstRunAsync()
    {
        try
        {
            // Add a timeout to prevent hanging on file system operations
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            
            var checkTask = Task.Run(async () =>
            {
                if (!File.Exists(_configFilePath))
                {
                    _logger.LogDebug("Configuration file does not exist, treating as first run");
                    return true;
                }

                var config = await GetConfigurationAsync();
                return config.IsFirstRun;
            }, cts.Token);

            return await checkTask;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("IsFirstRunAsync timed out, assuming first run");
            return true; // If we can't check quickly, assume first run
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking if first run, assuming first run");
            return true; // If we can't read config, assume first run
        }
    }

    public async Task<string> BuildConnectionStringAsync(DatabaseConfiguration configuration)
    {
        return configuration.Provider switch
        {
            DatabaseProvider.Sqlite => BuildSqliteConnectionString(configuration.Sqlite!),
            DatabaseProvider.SqlServer => await BuildSqlServerConnectionStringAsync(configuration.SqlServer!),
            DatabaseProvider.MySql => await BuildMySqlConnectionStringAsync(configuration.MySql!),
            DatabaseProvider.PostgreSql => await BuildPostgreSqlConnectionStringAsync(configuration.PostgreSql!),
            DatabaseProvider.Supabase => BuildSupabaseConnectionString(configuration.Supabase!),
            _ => throw new NotSupportedException($"Database provider {configuration.Provider} is not supported")
        };
    }

    public string BuildConnectionString(DatabaseConfiguration configuration)
    {
        // Keep the synchronous version for backwards compatibility, but limit it to providers that don't need async
        return configuration.Provider switch
        {
            DatabaseProvider.Sqlite => BuildSqliteConnectionString(configuration.Sqlite!),
            DatabaseProvider.Supabase => BuildSupabaseConnectionString(configuration.Supabase!),
            _ => throw new InvalidOperationException($"Provider {configuration.Provider} requires async operation. Use BuildConnectionStringAsync instead.")
        };
    }

    public async Task<(bool Success, string ErrorMessage)> TestConnectionAsync(DatabaseConfiguration configuration)
    {
        try
        {
            var connectionString = await BuildConnectionStringAsync(configuration);
            
            // Test connection based on provider
            switch (configuration.Provider)
            {
                case DatabaseProvider.Sqlite:
                    return await TestSqliteConnectionAsync(connectionString);
                case DatabaseProvider.SqlServer:
                    return await TestSqlServerConnectionAsync(connectionString);
                case DatabaseProvider.MySql:
                    return await TestMySqlConnectionAsync(connectionString);
                case DatabaseProvider.PostgreSql:
                    return await TestPostgreSqlConnectionAsync(connectionString);
                case DatabaseProvider.Supabase:
                    return await TestSupabaseConnectionAsync(connectionString);
                default:
                    return (false, $"Unsupported database provider: {configuration.Provider}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing database connection");
            return (false, $"Connection test failed: {ex.Message}");
        }
    }

    public async Task<string> EncryptPasswordAsync(string password)
    {
        if (string.IsNullOrEmpty(password))
            return string.Empty;

        try
        {
            var encryptionKey = await GetOrCreateEncryptionKeyAsync();
            var encryptedData = _cryptographyService.EncryptAes256Gcm(password, encryptionKey);
            
            // Convert to base64 for storage
            var combined = new byte[encryptedData.Nonce.Length + encryptedData.AuthenticationTag.Length + encryptedData.Ciphertext.Length];
            Array.Copy(encryptedData.Nonce, 0, combined, 0, encryptedData.Nonce.Length);
            Array.Copy(encryptedData.AuthenticationTag, 0, combined, encryptedData.Nonce.Length, encryptedData.AuthenticationTag.Length);
            Array.Copy(encryptedData.Ciphertext, 0, combined, encryptedData.Nonce.Length + encryptedData.AuthenticationTag.Length, encryptedData.Ciphertext.Length);
            
            return Convert.ToBase64String(combined);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encrypting password");
            throw;
        }
    }

    public async Task<string> DecryptPasswordAsync(string encryptedPassword)
    {
        if (string.IsNullOrEmpty(encryptedPassword))
            return string.Empty;

        try
        {
            var encryptionKey = await GetOrCreateEncryptionKeyAsync();
            var combined = Convert.FromBase64String(encryptedPassword);
            
            // Extract components (nonce: 12 bytes, auth tag: 16 bytes, rest: ciphertext)
            var nonce = new byte[12];
            var authTag = new byte[16];
            var ciphertext = new byte[combined.Length - 28];
            
            Array.Copy(combined, 0, nonce, 0, 12);
            Array.Copy(combined, 12, authTag, 0, 16);
            Array.Copy(combined, 28, ciphertext, 0, ciphertext.Length);
            
            var encryptedData = new EncryptedData
            {
                Nonce = nonce,
                AuthenticationTag = authTag,
                Ciphertext = ciphertext
            };
            
            return _cryptographyService.DecryptAes256Gcm(encryptedData, encryptionKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting password");
            throw;
        }
    }

    public DatabaseConfiguration GetDefaultConfiguration()
    {
        var config = new DatabaseConfiguration
        {
            Provider = DatabaseProvider.Sqlite,
            IsFirstRun = true,
            Sqlite = new SqliteConfig
            {
                DatabasePath = Path.Combine(_platformService.GetAppDataDirectory(), "data", "passwordmanager.db")
            }
        };

        return config;
    }

    public bool ShouldShowDatabaseSelection()
    {
        try
        {
            // Call platform service directly - it should be fast
            return _platformService.ShouldShowDatabaseSelection();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking if database selection should be shown, defaulting to false");
            return false; // Default to not showing database selection if there's an error
        }
    }

    /// <summary>
    /// Ensures a basic SQLite database exists for the application to start, regardless of configuration status.
    /// This method creates a minimal database structure without running migrations.
    /// </summary>
    public async Task EnsureBasicSqliteDatabaseAsync(IServiceProvider serviceProvider)
    {
        try
        {
            var defaultConfig = GetDefaultConfiguration();
            var dbPath = defaultConfig.Sqlite!.DatabasePath;
            
            // Ensure directory exists
            var directory = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogInformation("Created database directory: {Directory}", directory);
            }

            // Check if database file exists and has proper schema
            if (!File.Exists(dbPath))
            {
                _logger.LogInformation("Creating basic SQLite database with schema at: {DbPath}", dbPath);
                
                // Create database with proper schema using Entity Framework
                using var scope = serviceProvider.CreateScope();
                try
                {
                    // Get the DbContext services and create the database schema
                    var dbContext = scope.ServiceProvider.GetService<PasswordManager.DAL.PasswordManagerDbContext>();
                    var dbContextApp = scope.ServiceProvider.GetService<PasswordManager.DAL.PasswordManagerDbContextApp>();
                    
                    if (dbContext != null)
                    {
                        await dbContext.Database.EnsureCreatedAsync();
                        _logger.LogInformation("Created PasswordManagerDbContext database schema");
                    }
                    
                    if (dbContextApp != null)
                    {
                        await dbContextApp.Database.EnsureCreatedAsync();
                        _logger.LogInformation("Created PasswordManagerDbContextApp database schema");
                    }
                    
                    _logger.LogInformation("Basic SQLite database with schema created successfully");
                }
                catch (Exception contextEx)
                {
                    _logger.LogWarning(contextEx, "Could not create database schema using Entity Framework, creating empty database file");
                    // Fall back to creating an empty database file
                    using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}");
                    await connection.OpenAsync();
                    _logger.LogInformation("Empty SQLite database created as fallback");
                }
            }
            else
            {
                _logger.LogDebug("SQLite database already exists at: {DbPath}", dbPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring basic SQLite database exists");
            // Don't throw - allow app to continue
        }
    }

    private async Task<byte[]> GetOrCreateEncryptionKeyAsync()
    {
        if (_encryptionKey != null)
            return _encryptionKey;

        await _keyInitLock.WaitAsync();
        try
        {
            if (_encryptionKey != null)
                return _encryptionKey;

            var keyPath = Path.Combine(_platformService.GetAppDataDirectory(), ".dbkey");
            
            try
            {
                if (File.Exists(keyPath))
                {
                    _encryptionKey = await File.ReadAllBytesAsync(keyPath);
                    return _encryptionKey;
                }
                
                // Generate new key
                var key = _cryptographyService.GenerateSalt(32); // 256-bit key
                
                // Ensure directory exists
                var directory = Path.GetDirectoryName(keyPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                await File.WriteAllBytesAsync(keyPath, key);
                _encryptionKey = key;
                return _encryptionKey;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not create/read encryption key file, using fallback key");
                // Fallback: derive key from device/app info (less secure but functional)
                var fallbackSeed = $"{_platformService.GetDeviceIdentifier()}-PasswordManager";
                _encryptionKey = _cryptographyService.DeriveKey(fallbackSeed, 
                    System.Text.Encoding.UTF8.GetBytes("PasswordManagerDbKey"), 
                    100000, 32);
                return _encryptionKey;
            }
        }
        finally
        {
            _keyInitLock.Release();
        }
    }

    private byte[] GetOrCreateEncryptionKey()
    {
        // This synchronous version is kept for backwards compatibility but deprecated
        // New code should use GetOrCreateEncryptionKeyAsync()
        var keyPath = Path.Combine(_platformService.GetAppDataDirectory(), ".dbkey");
        
        try
        {
            if (File.Exists(keyPath))
            {
                return File.ReadAllBytes(keyPath);
            }
            
            // Generate new key
            var key = _cryptographyService.GenerateSalt(32); // 256-bit key
            
            // Ensure directory exists
            var directory = Path.GetDirectoryName(keyPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            File.WriteAllBytes(keyPath, key);
            return key;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not create/read encryption key file, using fallback key");
            // Fallback: derive key from device/app info (less secure but functional)
            var fallbackSeed = $"{_platformService.GetDeviceIdentifier()}-PasswordManager";
            return _cryptographyService.DeriveKey(fallbackSeed, 
                System.Text.Encoding.UTF8.GetBytes("PasswordManagerDbKey"), 
                100000, 32);
        }
    }

    private string BuildSqliteConnectionString(SqliteConfig config)
    {
        return $"Data Source={config.DatabasePath}";
    }

    private async Task<string> BuildSqlServerConnectionStringAsync(SqlServerConfig config)
    {
        var builder = new System.Text.StringBuilder();
        builder.Append($"Server={config.Host}");
        
        if (config.Port != 1433)
            builder.Append($",{config.Port}");
            
        builder.Append($";Database={config.Database}");
        
        if (config.UseWindowsAuthentication)
        {
            builder.Append(";Integrated Security=true");
        }
        else if (!string.IsNullOrEmpty(config.Username))
        {
            builder.Append($";User Id={config.Username}");
            if (!string.IsNullOrEmpty(config.EncryptedPassword))
            {
                var password = await DecryptPasswordAsync(config.EncryptedPassword);
                builder.Append($";Password={password}");
            }
        }
        
        if (config.TrustServerCertificate)
            builder.Append(";TrustServerCertificate=true");
            
        builder.Append($";Connection Timeout={config.ConnectionTimeout}");
        
        return builder.ToString();
    }

    private async Task<string> BuildMySqlConnectionStringAsync(MySqlConfig config)
    {
        var builder = new System.Text.StringBuilder();
        builder.Append($"Server={config.Host}");
        builder.Append($";Port={config.Port}");
        builder.Append($";Database={config.Database}");
        builder.Append($";Uid={config.Username}");
        
        if (!string.IsNullOrEmpty(config.EncryptedPassword))
        {
            var password = await DecryptPasswordAsync(config.EncryptedPassword);
            builder.Append($";Pwd={password}");
        }
        
        if (config.UseSsl)
            builder.Append(";SslMode=Required");
        else
            builder.Append(";SslMode=None");
            
        builder.Append($";Connection Timeout={config.ConnectionTimeout}");
        
        return builder.ToString();
    }

    private async Task<string> BuildPostgreSqlConnectionStringAsync(PostgreSqlConfig config)
    {
        var builder = new System.Text.StringBuilder();
        builder.Append($"Host={config.Host}");
        builder.Append($";Port={config.Port}");
        builder.Append($";Database={config.Database}");
        builder.Append($";Username={config.Username}");
        
        if (!string.IsNullOrEmpty(config.EncryptedPassword))
        {
            var password = await DecryptPasswordAsync(config.EncryptedPassword);
            builder.Append($";Password={password}");
        }
        
        if (config.UseSsl)
            builder.Append(";SSL Mode=Require");
        else
            builder.Append(";SSL Mode=Disable");
            
        builder.Append($";Timeout={config.ConnectionTimeout}");
        
        return builder.ToString();
    }

    private string BuildSupabaseConnectionString(SupabaseConfig config)
    {
        // For Supabase, we use PostgreSQL connection string format with SSL
        var builder = new System.Text.StringBuilder();
        
        // Extract host from Supabase URL (e.g., https://abc123.supabase.co -> abc123.supabase.co)
        var uri = new Uri(config.Url);
        builder.Append($"Host={uri.Host}");
        builder.Append(";Port=5432"); // Supabase uses PostgreSQL on port 5432
        builder.Append(";Database=postgres"); // Supabase default database
        builder.Append(";Username=postgres"); // Supabase default username
        
        if (!string.IsNullOrEmpty(config.ServiceKey))
        {
            builder.Append($";Password={config.ServiceKey}");
        }
        
        builder.Append(";SSL Mode=Require"); // Supabase requires SSL
        builder.Append($";Timeout={config.ConnectionTimeout}");
        
        return builder.ToString();
    }

    private async Task<(bool Success, string ErrorMessage)> TestSqliteConnectionAsync(string connectionString)
    {
        try
        {
            using var connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
            await connection.OpenAsync();
            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private async Task<(bool Success, string ErrorMessage)> TestSqlServerConnectionAsync(string connectionString)
    {
        try
        {
            using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
            await connection.OpenAsync();
            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private async Task<(bool Success, string ErrorMessage)> TestMySqlConnectionAsync(string connectionString)
    {
        try
        {
            using var connection = new MySqlConnector.MySqlConnection(connectionString);
            await connection.OpenAsync();
            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private async Task<(bool Success, string ErrorMessage)> TestPostgreSqlConnectionAsync(string connectionString)
    {
        try
        {
            using var connection = new Npgsql.NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private async Task<(bool Success, string ErrorMessage)> TestSupabaseConnectionAsync(string connectionString)
    {
        try
        {
            // Supabase uses PostgreSQL under the hood
            using var connection = new Npgsql.NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}