using System.ComponentModel.DataAnnotations;

namespace PasswordManager.Models.Configuration;

/// <summary>
/// Database provider types supported by the application
/// </summary>
public enum DatabaseProvider
{
    Sqlite,
    SqlServer,
    MySql,
    PostgreSql
}

/// <summary>
/// Database configuration settings
/// </summary>
public class DatabaseConfiguration
{
    /// <summary>
    /// The selected database provider
    /// </summary>
    public DatabaseProvider Provider { get; set; } = DatabaseProvider.Sqlite;

    /// <summary>
    /// Whether this is the first run and configuration is needed
    /// </summary>
    public bool IsFirstRun { get; set; } = true;

    /// <summary>
    /// SQLite-specific configuration
    /// </summary>
    public SqliteConfig? Sqlite { get; set; }

    /// <summary>
    /// SQL Server-specific configuration
    /// </summary>
    public SqlServerConfig? SqlServer { get; set; }

    /// <summary>
    /// MySQL-specific configuration
    /// </summary>
    public MySqlConfig? MySql { get; set; }

    /// <summary>
    /// PostgreSQL-specific configuration
    /// </summary>
    public PostgreSqlConfig? PostgreSql { get; set; }
}

/// <summary>
/// SQLite database configuration
/// </summary>
public class SqliteConfig
{
    /// <summary>
    /// Database file path (relative or absolute)
    /// </summary>
    public string DatabasePath { get; set; } = "passwordmanager.db";
}

/// <summary>
/// SQL Server database configuration
/// </summary>
public class SqlServerConfig
{
    /// <summary>
    /// Database server host/IP address
    /// </summary>
    [Required]
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Database server port (default: 1433)
    /// </summary>
    public int Port { get; set; } = 1433;

    /// <summary>
    /// Database name
    /// </summary>
    [Required]
    public string Database { get; set; } = "PasswordManager";

    /// <summary>
    /// Username for database authentication
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Encrypted password for database authentication
    /// </summary>
    public string? EncryptedPassword { get; set; }

    /// <summary>
    /// Whether to use Windows Authentication
    /// </summary>
    public bool UseWindowsAuthentication { get; set; } = false;

    /// <summary>
    /// Whether to trust server certificate
    /// </summary>
    public bool TrustServerCertificate { get; set; } = false;

    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    public int ConnectionTimeout { get; set; } = 30;
}

/// <summary>
/// MySQL database configuration
/// </summary>
public class MySqlConfig
{
    /// <summary>
    /// Database server host/IP address
    /// </summary>
    [Required]
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Database server port (default: 3306)
    /// </summary>
    public int Port { get; set; } = 3306;

    /// <summary>
    /// Database name
    /// </summary>
    [Required]
    public string Database { get; set; } = "PasswordManager";

    /// <summary>
    /// Username for database authentication
    /// </summary>
    [Required]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Encrypted password for database authentication
    /// </summary>
    public string? EncryptedPassword { get; set; }

    /// <summary>
    /// Whether to use SSL connection
    /// </summary>
    public bool UseSsl { get; set; } = true;

    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    public int ConnectionTimeout { get; set; } = 30;
}

/// <summary>
/// PostgreSQL database configuration
/// </summary>
public class PostgreSqlConfig
{
    /// <summary>
    /// Database server host/IP address
    /// </summary>
    [Required]
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Database server port (default: 5432)
    /// </summary>
    public int Port { get; set; } = 5432;

    /// <summary>
    /// Database name
    /// </summary>
    [Required]
    public string Database { get; set; } = "PasswordManager";

    /// <summary>
    /// Username for database authentication
    /// </summary>
    [Required]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Encrypted password for database authentication
    /// </summary>
    public string? EncryptedPassword { get; set; }

    /// <summary>
    /// Whether to use SSL connection
    /// </summary>
    public bool UseSsl { get; set; } = true;

    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    public int ConnectionTimeout { get; set; } = 30;
}