using System.ComponentModel.DataAnnotations;

namespace PasswordManager.Models;

/// <summary>
/// Unified configuration setting that can store different types of configurations
/// with grouping support (database config, user settings, app settings)
/// </summary>
public class ConfigurationSetting
{
    /// <summary>
    /// Unique identifier for the configuration setting
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Group key to organize configuration types (e.g., "DatabaseConfig", "UserSettings", "AppSettings")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string GroupKey { get; set; } = string.Empty;

    /// <summary>
    /// Configuration type within the group (e.g., "SQLite", "SqlServer", "ApiUrl", "Theme")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ConfigType { get; set; } = string.Empty;

    /// <summary>
    /// The setting key/name (e.g., "DatabasePath", "Host", "Port", "DarkMode")
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// The setting value (encrypted if sensitive data)
    /// </summary>
    [Required]
    [MaxLength(2000)]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// User ID for user-specific settings (null for global app settings)
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Whether the value is encrypted for security
    /// </summary>
    public bool IsEncrypted { get; set; } = false;

    /// <summary>
    /// Whether this setting is system-level (not user-specific)
    /// </summary>
    public bool IsSystemLevel { get; set; } = false;

    /// <summary>
    /// When the setting was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the setting was last modified
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the user (if user-specific)
    /// </summary>
    public ApplicationUser? User { get; set; }
}

/// <summary>
/// Common group keys for configuration organization
/// </summary>
public static class ConfigurationGroups
{
    /// <summary>
    /// Database configuration settings
    /// </summary>
    public const string DatabaseConfig = "DatabaseConfig";

    /// <summary>
    /// User preference settings
    /// </summary>
    public const string UserSettings = "UserSettings";

    /// <summary>
    /// Application-level settings
    /// </summary>
    public const string AppSettings = "AppSettings";

    /// <summary>
    /// API configuration settings
    /// </summary>
    public const string ApiConfig = "ApiConfig";

    /// <summary>
    /// Security configuration settings
    /// </summary>
    public const string SecurityConfig = "SecurityConfig";
}

/// <summary>
/// Common configuration types for database providers
/// </summary>
public static class DatabaseConfigTypes
{
    public const string SQLite = "SQLite";
    public const string SqlServer = "SqlServer";
    public const string MySql = "MySql";
    public const string PostgreSql = "PostgreSql";
    public const string Supabase = "Supabase";
    public const string General = "General";
}

/// <summary>
/// Common configuration keys
/// </summary>
public static class ConfigurationKeys
{
    // Database general
    public const string Provider = "Provider";
    public const string IsFirstRun = "IsFirstRun";
    public const string ApiUrl = "ApiUrl";
    public const string ApiKey = "ApiKey";

    // SQLite specific
    public const string DatabasePath = "DatabasePath";

    // SQL Server specific
    public const string Host = "Host";
    public const string Port = "Port";
    public const string Database = "Database";
    public const string Username = "Username";
    public const string EncryptedPassword = "EncryptedPassword";
    public const string UseWindowsAuthentication = "UseWindowsAuthentication";
    public const string TrustServerCertificate = "TrustServerCertificate";
    public const string ConnectionTimeout = "ConnectionTimeout";

    // MySQL/PostgreSQL specific
    public const string UseSsl = "UseSsl";

    // Supabase specific
    public const string Url = "Url";
    public const string ServiceKey = "ServiceKey";

    // User settings
    public const string Theme = "Theme";
    public const string Language = "Language";
    public const string AutoSync = "AutoSync";
}