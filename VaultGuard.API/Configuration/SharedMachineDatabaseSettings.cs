using Microsoft.Data.SqlClient;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.API.Configuration;

public static class SharedMachineDatabaseSettings
{
    public static string NormalizeProvider(string? provider)
        => (provider ?? string.Empty)
            .Trim()
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();

    public static bool TryBuildSqlServerConnectionString(IAppSettingsService settings, out string? connectionString)
    {
        var server = settings.Get("dbserver").Trim();
        var database = settings.Get("dbname").Trim();
        var username = settings.Get("dbusername").Trim();
        var password = settings.Get("dbpassword");

        if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(database))
        {
            connectionString = null;
            return false;
        }

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = server,
            InitialCatalog = database,
            Encrypt = true,
            TrustServerCertificate = true,
            MultipleActiveResultSets = true
        };

        if (string.IsNullOrWhiteSpace(username))
        {
            builder.IntegratedSecurity = true;
        }
        else
        {
            builder.UserID = username;
            builder.Password = password ?? string.Empty;
        }

        connectionString = builder.ConnectionString;
        return true;
    }
}
