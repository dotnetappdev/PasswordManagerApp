using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Crypto.Interfaces;
using PasswordManager.Models.Configuration;
using PasswordManager.Services.Interfaces;

namespace PasswordManager.Services.Services;

/// <summary>
/// Service for managing database configuration with encryption and unified storage support
/// </summary>
public class DatabaseConfigurationService : IDatabaseConfigurationService
{
    private readonly IConfiguration _configuration;
    private readonly ICryptographyService _cryptographyService;
    private readonly IPlatformService _platformService;
    private readonly ILogger<DatabaseConfigurationService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _configFilePath;
    private readonly byte[] _encryptionKey;

    // Flag to indicate if we should use unified configuration (database-based)
    private bool _useUnifiedConfig = false;

    public DatabaseConfigurationService(
        IConfiguration configuration,
        ICryptographyService cryptographyService,
        IPlatformService platformService,
        ILogger<DatabaseConfigurationService> logger,
        IServiceProvider serviceProvider)
    {
        _configuration = configuration;
        _cryptographyService = cryptographyService;
        _platformService = platformService;
        _logger = logger;
        _serviceProvider = serviceProvider;
        
        // Get the path to the appsettings.json file in the app data directory
        _configFilePath = Path.Combine(
            _platformService.GetAppDataDirectory(), 
            "appsettings.json");

        // Generate or load encryption key for database passwords
        _encryptionKey = GetOrCreateEncryptionKey();
    }

    public async Task<DatabaseConfiguration> GetConfigurationAsync()
    {
        try
        {
            // Try to get configuration from unified storage first
            if (_useUnifiedConfig && await CanUseUnifiedConfigAsync())
            {
                var unifiedConfig = await GetConfigurationFromUnifiedStorageAsync();
                if (unifiedConfig != null)
                {
                    _logger.LogInformation("Database configuration loaded from unified storage");
                    return unifiedConfig;
                }
            }

            // Fallback to file-based configuration
            if (!File.Exists(_configFilePath))
            {
                _logger.LogInformation("No database configuration file found, returning default configuration");
                return GetDefaultConfiguration();
            }

            var jsonContent = await File.ReadAllTextAsync(_configFilePath);
            var config = JsonSerializer.Deserialize<DatabaseConfiguration>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var result = config ?? GetDefaultConfiguration();

            // Attempt to migrate to unified storage if possible
            if (await CanUseUnifiedConfigAsync())
            {
                await MigrateToUnifiedStorageAsync(result);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading database configuration");
            return GetDefaultConfiguration();
        }
    }

    public async Task SaveConfigurationAsync(DatabaseConfiguration configuration)
    {
        try
        {
            // Mark as no longer first run
            configuration.IsFirstRun = false;

            // Try to save to unified storage first
            if (_useUnifiedConfig && await CanUseUnifiedConfigAsync())
            {
                await SaveConfigurationToUnifiedStorageAsync(configuration);
                _logger.LogInformation("Database configuration saved to unified storage");
            }
            else
            {
                // Fallback to file-based storage
                await SaveConfigurationToFileAsync(configuration);
                _logger.LogInformation("Database configuration saved to file");
            }
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
            var config = await GetConfigurationAsync();
            return config.IsFirstRun;
        }
        catch
        {
            return true; // If we can't read config, assume first run
        }
    }

    public string BuildConnectionString(DatabaseConfiguration configuration)
    {
        return configuration.Provider switch
        {
            DatabaseProvider.Sqlite => BuildSqliteConnectionString(configuration.Sqlite!),
            DatabaseProvider.SqlServer => BuildSqlServerConnectionString(configuration.SqlServer!),
            DatabaseProvider.MySql => BuildMySqlConnectionString(configuration.MySql!),
            DatabaseProvider.PostgreSql => BuildPostgreSqlConnectionString(configuration.PostgreSql!),
            DatabaseProvider.Supabase => BuildSupabaseConnectionString(configuration.Supabase!),
            _ => throw new NotSupportedException($"Database provider {configuration.Provider} is not supported")
        };
    }

    public async Task<(bool Success, string ErrorMessage)> TestConnectionAsync(DatabaseConfiguration configuration)
    {
        try
        {
            var connectionString = BuildConnectionString(configuration);
            
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
            var encryptedData = _cryptographyService.EncryptAes256Gcm(password, _encryptionKey);
            
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
            
            return _cryptographyService.DecryptAes256Gcm(encryptedData, _encryptionKey);
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
        return _platformService.ShouldShowDatabaseSelection();
    }

    private byte[] GetOrCreateEncryptionKey()
    {
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

    private string BuildSqlServerConnectionString(SqlServerConfig config)
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
                var password = DecryptPasswordAsync(config.EncryptedPassword).Result;
                builder.Append($";Password={password}");
            }
        }
        
        if (config.TrustServerCertificate)
            builder.Append(";TrustServerCertificate=true");
            
        builder.Append($";Connection Timeout={config.ConnectionTimeout}");
        
        return builder.ToString();
    }

    private string BuildMySqlConnectionString(MySqlConfig config)
    {
        var builder = new System.Text.StringBuilder();
        builder.Append($"Server={config.Host}");
        builder.Append($";Port={config.Port}");
        builder.Append($";Database={config.Database}");
        builder.Append($";Uid={config.Username}");
        
        if (!string.IsNullOrEmpty(config.EncryptedPassword))
        {
            var password = DecryptPasswordAsync(config.EncryptedPassword).Result;
            builder.Append($";Pwd={password}");
        }
        
        if (config.UseSsl)
            builder.Append(";SslMode=Required");
        else
            builder.Append(";SslMode=None");
            
        builder.Append($";Connection Timeout={config.ConnectionTimeout}");
        
        return builder.ToString();
    }

    private string BuildPostgreSqlConnectionString(PostgreSqlConfig config)
    {
        var builder = new System.Text.StringBuilder();
        builder.Append($"Host={config.Host}");
        builder.Append($";Port={config.Port}");
        builder.Append($";Database={config.Database}");
        builder.Append($";Username={config.Username}");
        
        if (!string.IsNullOrEmpty(config.EncryptedPassword))
        {
            var password = DecryptPasswordAsync(config.EncryptedPassword).Result;
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

    #region Unified Configuration Support

    /// <summary>
    /// Checks if unified configuration service is available
    /// </summary>
    private async Task<bool> CanUseUnifiedConfigAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var configService = scope.ServiceProvider.GetService<IConfigurationSettingService>();
            return configService != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Loads configuration from unified storage
    /// </summary>
    private async Task<DatabaseConfiguration?> GetConfigurationFromUnifiedStorageAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var configService = scope.ServiceProvider.GetService<IConfigurationSettingService>();
            if (configService == null) return null;

            return await configService.LoadDatabaseConfigurationAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load configuration from unified storage");
            return null;
        }
    }

    /// <summary>
    /// Saves configuration to unified storage
    /// </summary>
    private async Task SaveConfigurationToUnifiedStorageAsync(DatabaseConfiguration configuration)
    {
        using var scope = _serviceProvider.CreateScope();
        var configService = scope.ServiceProvider.GetService<IConfigurationSettingService>();
        if (configService == null)
            throw new InvalidOperationException("Unified configuration service not available");

        await configService.MigrateDatabaseConfigurationAsync(configuration);
    }

    /// <summary>
    /// Migrates existing file-based configuration to unified storage
    /// </summary>
    private async Task MigrateToUnifiedStorageAsync(DatabaseConfiguration configuration)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var configService = scope.ServiceProvider.GetService<IConfigurationSettingService>();
            if (configService == null) return;

            await configService.MigrateDatabaseConfigurationAsync(configuration);
            _useUnifiedConfig = true;
            _logger.LogInformation("Successfully migrated configuration to unified storage");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to migrate configuration to unified storage");
        }
    }

    /// <summary>
    /// Saves configuration to file (fallback method)
    /// </summary>
    private async Task SaveConfigurationToFileAsync(DatabaseConfiguration configuration)
    {
        // Ensure the directory exists
        var directory = Path.GetDirectoryName(_configFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var jsonContent = JsonSerializer.Serialize(configuration, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await File.WriteAllTextAsync(_configFilePath, jsonContent);
    }

    /// <summary>
    /// Enables unified configuration mode
    /// </summary>
    public void EnableUnifiedConfiguration()
    {
        _useUnifiedConfig = true;
    }

    /// <summary>
    /// Disables unified configuration mode (fallback to file-based)
    /// </summary>
    public void DisableUnifiedConfiguration()
    {
        _useUnifiedConfig = false;
    }

    #endregion
}