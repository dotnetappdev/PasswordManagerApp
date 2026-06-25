using System.ComponentModel.DataAnnotations;

namespace VaultGuard.Models.Configuration;

public enum DatabaseProvider
{
    Sqlite,
    SqlServer,
    MySql,
    PostgreSql,
    Supabase
}

public enum AuthenticationMode
{
    LocalDatabase,
    ApiEndpoint
}

public enum SqlServerAuthMode
{
    WindowsAuthentication,
    SqlServerAuthentication
}

public enum SqlServerEncryptionMode
{
    None,
    Optional,
    Mandatory,
    Strict
}

public enum SqlServerNetworkProtocol
{
    Default,
    TcpIp,
    NamedPipes,
    SharedMemory
}

public enum SqlServerApplicationIntent
{
    ReadWrite,
    ReadOnly
}

public enum MySqlSslMode
{
    None,
    Preferred,
    Required,
    VerifyCA,
    VerifyFull
}

public enum PostgreSqlSslMode
{
    Disable,
    Allow,
    Prefer,
    Require,
    VerifyCA,
    VerifyFull
}

public class DatabaseConfiguration
{
    public DatabaseProvider Provider { get; set; } = DatabaseProvider.Sqlite;
    public AuthenticationMode AuthenticationMode { get; set; } = AuthenticationMode.LocalDatabase;
    public bool IsFirstRun { get; set; } = true;
    public string? ApiUrl { get; set; }
    public string? ApiKey { get; set; }
    public SqliteConfig? Sqlite { get; set; }
    public SqlServerConfig? SqlServer { get; set; }
    public MySqlConfig? MySql { get; set; }
    public PostgreSqlConfig? PostgreSql { get; set; }
    public SupabaseConfig? Supabase { get; set; }
}

public class SqliteConfig
{
    public string DatabasePath { get; set; } = "passwordmanager.db";
    public string? ApiUrl { get; set; }
    public string? ApiKey { get; set; }
}

public class SqlServerConfig
{
    // --- Login ---
    [Required]
    public string Host { get; set; } = string.Empty;

    public string? InstanceName { get; set; }

    public int Port { get; set; } = 1433;

    [Required]
    public string Database { get; set; } = "VaultGuard";

    public SqlServerAuthMode AuthMode { get; set; } = SqlServerAuthMode.SqlServerAuthentication;

    public string? Username { get; set; }

    public string? EncryptedPassword { get; set; }

    // --- Security ---
    public SqlServerEncryptionMode Encryption { get; set; } = SqlServerEncryptionMode.Optional;

    public bool TrustServerCertificate { get; set; } = false;

    /// <summary>Path or thumbprint of the server certificate to trust (used with Strict encryption)</summary>
    public string? ServerCertificate { get; set; }

    // --- Connection Properties ---
    public SqlServerNetworkProtocol NetworkProtocol { get; set; } = SqlServerNetworkProtocol.Default;

    public int PacketSize { get; set; } = 4096;

    public int ConnectionTimeout { get; set; } = 15;

    public int CommandTimeout { get; set; } = 30;

    public string ApplicationName { get; set; } = "VaultGuard";

    public string? WorkstationId { get; set; }

    // --- Advanced ---
    public bool MultipleActiveResultSets { get; set; } = false;

    public SqlServerApplicationIntent ApplicationIntent { get; set; } = SqlServerApplicationIntent.ReadWrite;

    public bool MultiSubnetFailover { get; set; } = false;

    public string? FailoverPartner { get; set; }

    public bool Pooling { get; set; } = true;

    public int MinPoolSize { get; set; } = 0;

    public int MaxPoolSize { get; set; } = 100;

    /// <summary>Extra key=value pairs appended to the connection string verbatim, as in SSMS Additional Parameters</summary>
    public string? AdditionalParameters { get; set; }

    public string? ApiUrl { get; set; }
    public string? ApiKey { get; set; }
}

public class MySqlConfig
{
    // --- General ---
    [Required]
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 3306;

    [Required]
    public string Database { get; set; } = "VaultGuard";

    [Required]
    public string Username { get; set; } = string.Empty;

    public string? EncryptedPassword { get; set; }

    // --- SSL ---
    public MySqlSslMode SslMode { get; set; } = MySqlSslMode.Preferred;

    public string? SslCaPath { get; set; }

    public string? SslCertPath { get; set; }

    public string? SslKeyPath { get; set; }

    // --- Advanced ---
    public int ConnectionTimeout { get; set; } = 30;

    public int CommandTimeout { get; set; } = 30;

    public bool AllowZeroDateTime { get; set; } = false;

    public bool AllowUserVariables { get; set; } = false;

    public string CharacterSet { get; set; } = "utf8mb4";

    public bool Pooling { get; set; } = true;

    public int MinPoolSize { get; set; } = 0;

    public int MaxPoolSize { get; set; } = 100;

    public string? ApiUrl { get; set; }
    public string? ApiKey { get; set; }
}

public class PostgreSqlConfig
{
    // --- General ---
    [Required]
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 5432;

    [Required]
    public string Database { get; set; } = "VaultGuard";

    [Required]
    public string Username { get; set; } = string.Empty;

    public string? EncryptedPassword { get; set; }

    // --- SSL ---
    public PostgreSqlSslMode SslMode { get; set; } = PostgreSqlSslMode.Prefer;

    public string? SslCertPath { get; set; }

    public string? SslKeyPath { get; set; }

    public string? SslRootCertPath { get; set; }

    // --- Advanced ---
    public int ConnectionTimeout { get; set; } = 30;

    public int CommandTimeout { get; set; } = 30;

    public string ApplicationName { get; set; } = "VaultGuard";

    public string? SearchPath { get; set; }

    public bool Pooling { get; set; } = true;

    public int MinPoolSize { get; set; } = 1;

    public int MaxPoolSize { get; set; } = 100;

    public string? ApiUrl { get; set; }
    public string? ApiKey { get; set; }
}

public class SupabaseConfig
{
    [Required]
    public string Url { get; set; } = string.Empty;

    [Required]
    public string ServiceKey { get; set; } = string.Empty;

    public int ConnectionTimeout { get; set; } = 30;

    public string? ApiUrl { get; set; }
    public string? ApiKey { get; set; }
}
