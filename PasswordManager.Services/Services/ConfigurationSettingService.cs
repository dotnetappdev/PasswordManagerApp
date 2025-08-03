using Microsoft.EntityFrameworkCore;
using PasswordManager.DAL.Interfaces;
using PasswordManager.Models;
using PasswordManager.Models.Configuration;
using PasswordManager.Services.Interfaces;

namespace PasswordManager.Services.Services;

/// <summary>
/// Service for managing unified configuration settings with grouping support
/// </summary>
public class ConfigurationSettingService : IConfigurationSettingService
{
    private readonly IPasswordManagerDbContext _context;
    private readonly IPasswordEncryptionService _encryptionService;

    public ConfigurationSettingService(IPasswordManagerDbContext context, IPasswordEncryptionService encryptionService)
    {
        _context = context;
        _encryptionService = encryptionService;
    }

    public async Task<string?> GetConfigurationValueAsync(string groupKey, string configType, string key, string? userId = null)
    {
        var setting = await _context.ConfigurationSettings
            .FirstOrDefaultAsync(c => c.GroupKey == groupKey && 
                                    c.ConfigType == configType && 
                                    c.Key == key && 
                                    c.UserId == userId);

        if (setting == null)
            return null;

        return setting.IsEncrypted ? _encryptionService.Decrypt(setting.Value) : setting.Value;
    }

    public async Task SetConfigurationValueAsync(string groupKey, string configType, string key, string value, 
        string? userId = null, bool isEncrypted = false, bool isSystemLevel = false)
    {
        var encryptedValue = isEncrypted ? _encryptionService.Encrypt(value) : value;

        var setting = await _context.ConfigurationSettings
            .FirstOrDefaultAsync(c => c.GroupKey == groupKey && 
                                    c.ConfigType == configType && 
                                    c.Key == key && 
                                    c.UserId == userId);

        if (setting != null)
        {
            setting.Value = encryptedValue;
            setting.IsEncrypted = isEncrypted;
            setting.IsSystemLevel = isSystemLevel;
            setting.LastModified = DateTime.UtcNow;
        }
        else
        {
            setting = new ConfigurationSetting
            {
                GroupKey = groupKey,
                ConfigType = configType,
                Key = key,
                Value = encryptedValue,
                UserId = userId,
                IsEncrypted = isEncrypted,
                IsSystemLevel = isSystemLevel,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };
            _context.ConfigurationSettings.Add(setting);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<Dictionary<string, string>> GetConfigurationGroupAsync(string groupKey, string configType, string? userId = null)
    {
        var settings = await _context.ConfigurationSettings
            .Where(c => c.GroupKey == groupKey && 
                       c.ConfigType == configType && 
                       c.UserId == userId)
            .ToListAsync();

        var result = new Dictionary<string, string>();
        foreach (var setting in settings)
        {
            var value = setting.IsEncrypted ? _encryptionService.Decrypt(setting.Value) : setting.Value;
            result[setting.Key] = value;
        }

        return result;
    }

    public async Task SetConfigurationGroupAsync(string groupKey, string configType, Dictionary<string, string> configurations, 
        string? userId = null, List<string>? encryptedKeys = null, bool isSystemLevel = false)
    {
        encryptedKeys ??= new List<string>();

        foreach (var config in configurations)
        {
            var shouldEncrypt = encryptedKeys.Contains(config.Key);
            await SetConfigurationValueAsync(groupKey, configType, config.Key, config.Value, userId, shouldEncrypt, isSystemLevel);
        }
    }

    public async Task DeleteConfigurationAsync(string groupKey, string configType, string key, string? userId = null)
    {
        var setting = await _context.ConfigurationSettings
            .FirstOrDefaultAsync(c => c.GroupKey == groupKey && 
                                    c.ConfigType == configType && 
                                    c.Key == key && 
                                    c.UserId == userId);

        if (setting != null)
        {
            _context.ConfigurationSettings.Remove(setting);
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteConfigurationGroupAsync(string groupKey, string configType, string? userId = null)
    {
        var settings = await _context.ConfigurationSettings
            .Where(c => c.GroupKey == groupKey && 
                       c.ConfigType == configType && 
                       c.UserId == userId)
            .ToListAsync();

        if (settings.Any())
        {
            _context.ConfigurationSettings.RemoveRange(settings);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<string>> GetConfigurationGroupsAsync(string? userId = null)
    {
        return await _context.ConfigurationSettings
            .Where(c => c.UserId == userId)
            .Select(c => c.GroupKey)
            .Distinct()
            .ToListAsync();
    }

    public async Task<List<string>> GetConfigurationTypesAsync(string groupKey, string? userId = null)
    {
        return await _context.ConfigurationSettings
            .Where(c => c.GroupKey == groupKey && c.UserId == userId)
            .Select(c => c.ConfigType)
            .Distinct()
            .ToListAsync();
    }

    public async Task MigrateDatabaseConfigurationAsync(DatabaseConfiguration databaseConfig, string? userId = null)
    {
        // First clear existing database configuration
        await DeleteConfigurationGroupAsync(ConfigurationGroups.DatabaseConfig, DatabaseConfigTypes.General, userId);

        // Migrate general database settings
        var generalConfig = new Dictionary<string, string>
        {
            [ConfigurationKeys.Provider] = databaseConfig.Provider.ToString(),
            [ConfigurationKeys.IsFirstRun] = databaseConfig.IsFirstRun.ToString()
        };

        if (!string.IsNullOrEmpty(databaseConfig.ApiUrl))
            generalConfig[ConfigurationKeys.ApiUrl] = databaseConfig.ApiUrl;
        
        if (!string.IsNullOrEmpty(databaseConfig.ApiKey))
            generalConfig[ConfigurationKeys.ApiKey] = databaseConfig.ApiKey;

        await SetConfigurationGroupAsync(
            ConfigurationGroups.DatabaseConfig, 
            DatabaseConfigTypes.General, 
            generalConfig, 
            userId, 
            new List<string> { ConfigurationKeys.ApiKey },
            isSystemLevel: userId == null);

        // Migrate provider-specific configurations
        switch (databaseConfig.Provider)
        {
            case DatabaseProvider.Sqlite:
                if (databaseConfig.Sqlite != null)
                    await MigrateSqliteConfigAsync(databaseConfig.Sqlite, userId);
                break;
            case DatabaseProvider.SqlServer:
                if (databaseConfig.SqlServer != null)
                    await MigrateSqlServerConfigAsync(databaseConfig.SqlServer, userId);
                break;
            case DatabaseProvider.MySql:
                if (databaseConfig.MySql != null)
                    await MigrateMySqlConfigAsync(databaseConfig.MySql, userId);
                break;
            case DatabaseProvider.PostgreSql:
                if (databaseConfig.PostgreSql != null)
                    await MigratePostgreSqlConfigAsync(databaseConfig.PostgreSql, userId);
                break;
            case DatabaseProvider.Supabase:
                if (databaseConfig.Supabase != null)
                    await MigrateSupabaseConfigAsync(databaseConfig.Supabase, userId);
                break;
        }
    }

    public async Task<DatabaseConfiguration> LoadDatabaseConfigurationAsync(string? userId = null)
    {
        var config = new DatabaseConfiguration();

        // Load general configuration
        var generalConfig = await GetConfigurationGroupAsync(ConfigurationGroups.DatabaseConfig, DatabaseConfigTypes.General, userId);
        
        if (generalConfig.TryGetValue(ConfigurationKeys.Provider, out var providerValue) && 
            Enum.TryParse<DatabaseProvider>(providerValue, out var provider))
        {
            config.Provider = provider;
        }

        if (generalConfig.TryGetValue(ConfigurationKeys.IsFirstRun, out var firstRunValue) && 
            bool.TryParse(firstRunValue, out var isFirstRun))
        {
            config.IsFirstRun = isFirstRun;
        }

        config.ApiUrl = generalConfig.GetValueOrDefault(ConfigurationKeys.ApiUrl);
        config.ApiKey = generalConfig.GetValueOrDefault(ConfigurationKeys.ApiKey);

        // Load provider-specific configurations
        switch (config.Provider)
        {
            case DatabaseProvider.Sqlite:
                config.Sqlite = await LoadSqliteConfigAsync(userId);
                break;
            case DatabaseProvider.SqlServer:
                config.SqlServer = await LoadSqlServerConfigAsync(userId);
                break;
            case DatabaseProvider.MySql:
                config.MySql = await LoadMySqlConfigAsync(userId);
                break;
            case DatabaseProvider.PostgreSql:
                config.PostgreSql = await LoadPostgreSqlConfigAsync(userId);
                break;
            case DatabaseProvider.Supabase:
                config.Supabase = await LoadSupabaseConfigAsync(userId);
                break;
        }

        return config;
    }

    #region Private Helper Methods

    private async Task MigrateSqliteConfigAsync(SqliteConfig config, string? userId)
    {
        var sqliteConfig = new Dictionary<string, string>
        {
            [ConfigurationKeys.DatabasePath] = config.DatabasePath
        };

        if (!string.IsNullOrEmpty(config.ApiUrl))
            sqliteConfig[ConfigurationKeys.ApiUrl] = config.ApiUrl;
        
        if (!string.IsNullOrEmpty(config.ApiKey))
            sqliteConfig[ConfigurationKeys.ApiKey] = config.ApiKey;

        await SetConfigurationGroupAsync(
            ConfigurationGroups.DatabaseConfig, 
            DatabaseConfigTypes.SQLite, 
            sqliteConfig, 
            userId, 
            new List<string> { ConfigurationKeys.ApiKey },
            isSystemLevel: userId == null);
    }

    private async Task MigrateSqlServerConfigAsync(SqlServerConfig config, string? userId)
    {
        var sqlServerConfig = new Dictionary<string, string>
        {
            [ConfigurationKeys.Host] = config.Host,
            [ConfigurationKeys.Port] = config.Port.ToString(),
            [ConfigurationKeys.Database] = config.Database,
            [ConfigurationKeys.UseWindowsAuthentication] = config.UseWindowsAuthentication.ToString(),
            [ConfigurationKeys.TrustServerCertificate] = config.TrustServerCertificate.ToString(),
            [ConfigurationKeys.ConnectionTimeout] = config.ConnectionTimeout.ToString()
        };

        if (!string.IsNullOrEmpty(config.Username))
            sqlServerConfig[ConfigurationKeys.Username] = config.Username;
        
        if (!string.IsNullOrEmpty(config.EncryptedPassword))
            sqlServerConfig[ConfigurationKeys.EncryptedPassword] = config.EncryptedPassword;

        if (!string.IsNullOrEmpty(config.ApiUrl))
            sqlServerConfig[ConfigurationKeys.ApiUrl] = config.ApiUrl;
        
        if (!string.IsNullOrEmpty(config.ApiKey))
            sqlServerConfig[ConfigurationKeys.ApiKey] = config.ApiKey;

        await SetConfigurationGroupAsync(
            ConfigurationGroups.DatabaseConfig, 
            DatabaseConfigTypes.SqlServer, 
            sqlServerConfig, 
            userId, 
            new List<string> { ConfigurationKeys.EncryptedPassword, ConfigurationKeys.ApiKey },
            isSystemLevel: userId == null);
    }

    private async Task MigrateMySqlConfigAsync(MySqlConfig config, string? userId)
    {
        var mysqlConfig = new Dictionary<string, string>
        {
            [ConfigurationKeys.Host] = config.Host,
            [ConfigurationKeys.Port] = config.Port.ToString(),
            [ConfigurationKeys.Database] = config.Database,
            [ConfigurationKeys.Username] = config.Username,
            [ConfigurationKeys.UseSsl] = config.UseSsl.ToString(),
            [ConfigurationKeys.ConnectionTimeout] = config.ConnectionTimeout.ToString()
        };

        if (!string.IsNullOrEmpty(config.EncryptedPassword))
            mysqlConfig[ConfigurationKeys.EncryptedPassword] = config.EncryptedPassword;

        if (!string.IsNullOrEmpty(config.ApiUrl))
            mysqlConfig[ConfigurationKeys.ApiUrl] = config.ApiUrl;
        
        if (!string.IsNullOrEmpty(config.ApiKey))
            mysqlConfig[ConfigurationKeys.ApiKey] = config.ApiKey;

        await SetConfigurationGroupAsync(
            ConfigurationGroups.DatabaseConfig, 
            DatabaseConfigTypes.MySql, 
            mysqlConfig, 
            userId, 
            new List<string> { ConfigurationKeys.EncryptedPassword, ConfigurationKeys.ApiKey },
            isSystemLevel: userId == null);
    }

    private async Task MigratePostgreSqlConfigAsync(PostgreSqlConfig config, string? userId)
    {
        var postgresConfig = new Dictionary<string, string>
        {
            [ConfigurationKeys.Host] = config.Host,
            [ConfigurationKeys.Port] = config.Port.ToString(),
            [ConfigurationKeys.Database] = config.Database,
            [ConfigurationKeys.Username] = config.Username,
            [ConfigurationKeys.UseSsl] = config.UseSsl.ToString(),
            [ConfigurationKeys.ConnectionTimeout] = config.ConnectionTimeout.ToString()
        };

        if (!string.IsNullOrEmpty(config.EncryptedPassword))
            postgresConfig[ConfigurationKeys.EncryptedPassword] = config.EncryptedPassword;

        if (!string.IsNullOrEmpty(config.ApiUrl))
            postgresConfig[ConfigurationKeys.ApiUrl] = config.ApiUrl;
        
        if (!string.IsNullOrEmpty(config.ApiKey))
            postgresConfig[ConfigurationKeys.ApiKey] = config.ApiKey;

        await SetConfigurationGroupAsync(
            ConfigurationGroups.DatabaseConfig, 
            DatabaseConfigTypes.PostgreSql, 
            postgresConfig, 
            userId, 
            new List<string> { ConfigurationKeys.EncryptedPassword, ConfigurationKeys.ApiKey },
            isSystemLevel: userId == null);
    }

    private async Task MigrateSupabaseConfigAsync(SupabaseConfig config, string? userId)
    {
        var supabaseConfig = new Dictionary<string, string>
        {
            [ConfigurationKeys.Url] = config.Url,
            [ConfigurationKeys.ServiceKey] = config.ServiceKey,
            [ConfigurationKeys.ConnectionTimeout] = config.ConnectionTimeout.ToString()
        };

        if (!string.IsNullOrEmpty(config.ApiUrl))
            supabaseConfig[ConfigurationKeys.ApiUrl] = config.ApiUrl;
        
        if (!string.IsNullOrEmpty(config.ApiKey))
            supabaseConfig[ConfigurationKeys.ApiKey] = config.ApiKey;

        await SetConfigurationGroupAsync(
            ConfigurationGroups.DatabaseConfig, 
            DatabaseConfigTypes.Supabase, 
            supabaseConfig, 
            userId, 
            new List<string> { ConfigurationKeys.ServiceKey, ConfigurationKeys.ApiKey },
            isSystemLevel: userId == null);
    }

    private async Task<SqliteConfig> LoadSqliteConfigAsync(string? userId)
    {
        var config = await GetConfigurationGroupAsync(ConfigurationGroups.DatabaseConfig, DatabaseConfigTypes.SQLite, userId);
        
        return new SqliteConfig
        {
            DatabasePath = config.GetValueOrDefault(ConfigurationKeys.DatabasePath, "passwordmanager.db"),
            ApiUrl = config.GetValueOrDefault(ConfigurationKeys.ApiUrl),
            ApiKey = config.GetValueOrDefault(ConfigurationKeys.ApiKey)
        };
    }

    private async Task<SqlServerConfig> LoadSqlServerConfigAsync(string? userId)
    {
        var config = await GetConfigurationGroupAsync(ConfigurationGroups.DatabaseConfig, DatabaseConfigTypes.SqlServer, userId);
        
        return new SqlServerConfig
        {
            Host = config.GetValueOrDefault(ConfigurationKeys.Host, string.Empty),
            Port = int.TryParse(config.GetValueOrDefault(ConfigurationKeys.Port), out var port) ? port : 1433,
            Database = config.GetValueOrDefault(ConfigurationKeys.Database, "PasswordManager"),
            Username = config.GetValueOrDefault(ConfigurationKeys.Username),
            EncryptedPassword = config.GetValueOrDefault(ConfigurationKeys.EncryptedPassword),
            UseWindowsAuthentication = bool.TryParse(config.GetValueOrDefault(ConfigurationKeys.UseWindowsAuthentication), out var winAuth) && winAuth,
            TrustServerCertificate = bool.TryParse(config.GetValueOrDefault(ConfigurationKeys.TrustServerCertificate), out var trustCert) && trustCert,
            ConnectionTimeout = int.TryParse(config.GetValueOrDefault(ConfigurationKeys.ConnectionTimeout), out var timeout) ? timeout : 30,
            ApiUrl = config.GetValueOrDefault(ConfigurationKeys.ApiUrl),
            ApiKey = config.GetValueOrDefault(ConfigurationKeys.ApiKey)
        };
    }

    private async Task<MySqlConfig> LoadMySqlConfigAsync(string? userId)
    {
        var config = await GetConfigurationGroupAsync(ConfigurationGroups.DatabaseConfig, DatabaseConfigTypes.MySql, userId);
        
        return new MySqlConfig
        {
            Host = config.GetValueOrDefault(ConfigurationKeys.Host, string.Empty),
            Port = int.TryParse(config.GetValueOrDefault(ConfigurationKeys.Port), out var port) ? port : 3306,
            Database = config.GetValueOrDefault(ConfigurationKeys.Database, "PasswordManager"),
            Username = config.GetValueOrDefault(ConfigurationKeys.Username, string.Empty),
            EncryptedPassword = config.GetValueOrDefault(ConfigurationKeys.EncryptedPassword),
            UseSsl = bool.TryParse(config.GetValueOrDefault(ConfigurationKeys.UseSsl), out var useSsl) ? useSsl : true,
            ConnectionTimeout = int.TryParse(config.GetValueOrDefault(ConfigurationKeys.ConnectionTimeout), out var timeout) ? timeout : 30,
            ApiUrl = config.GetValueOrDefault(ConfigurationKeys.ApiUrl),
            ApiKey = config.GetValueOrDefault(ConfigurationKeys.ApiKey)
        };
    }

    private async Task<PostgreSqlConfig> LoadPostgreSqlConfigAsync(string? userId)
    {
        var config = await GetConfigurationGroupAsync(ConfigurationGroups.DatabaseConfig, DatabaseConfigTypes.PostgreSql, userId);
        
        return new PostgreSqlConfig
        {
            Host = config.GetValueOrDefault(ConfigurationKeys.Host, string.Empty),
            Port = int.TryParse(config.GetValueOrDefault(ConfigurationKeys.Port), out var port) ? port : 5432,
            Database = config.GetValueOrDefault(ConfigurationKeys.Database, "PasswordManager"),
            Username = config.GetValueOrDefault(ConfigurationKeys.Username, string.Empty),
            EncryptedPassword = config.GetValueOrDefault(ConfigurationKeys.EncryptedPassword),
            UseSsl = bool.TryParse(config.GetValueOrDefault(ConfigurationKeys.UseSsl), out var useSsl) ? useSsl : true,
            ConnectionTimeout = int.TryParse(config.GetValueOrDefault(ConfigurationKeys.ConnectionTimeout), out var timeout) ? timeout : 30,
            ApiUrl = config.GetValueOrDefault(ConfigurationKeys.ApiUrl),
            ApiKey = config.GetValueOrDefault(ConfigurationKeys.ApiKey)
        };
    }

    private async Task<SupabaseConfig> LoadSupabaseConfigAsync(string? userId)
    {
        var config = await GetConfigurationGroupAsync(ConfigurationGroups.DatabaseConfig, DatabaseConfigTypes.Supabase, userId);
        
        return new SupabaseConfig
        {
            Url = config.GetValueOrDefault(ConfigurationKeys.Url, string.Empty),
            ServiceKey = config.GetValueOrDefault(ConfigurationKeys.ServiceKey, string.Empty),
            ConnectionTimeout = int.TryParse(config.GetValueOrDefault(ConfigurationKeys.ConnectionTimeout), out var timeout) ? timeout : 30,
            ApiUrl = config.GetValueOrDefault(ConfigurationKeys.ApiUrl),
            ApiKey = config.GetValueOrDefault(ConfigurationKeys.ApiKey)
        };
    }

    #endregion
}