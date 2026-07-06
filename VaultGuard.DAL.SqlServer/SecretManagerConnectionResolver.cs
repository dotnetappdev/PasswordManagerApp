using System;
using System.Collections.Generic;
using System.Linq;
using Google.Api.Gax.ResourceNames;
using Google.Cloud.SecretManager.V1;
using Microsoft.Extensions.Configuration;

namespace VaultGuard.DAL.SqlServer
{
    /// <summary>
    /// Design-time helper that mirrors the API's GoogleSecretManager configuration provider: it pulls the
    /// <c>dbserver</c>/<c>dbusername</c>/<c>dbpassword</c> secrets from Google Cloud Secret Manager (scoped
    /// by the <c>env</c> label) and composes a SQL Server connection string, so <c>dotnet ef</c> migrations
    /// target the exact same database the running API uses.
    ///
    /// Kept self-contained — and referenced with <c>PrivateAssets="all"</c> — so the Secret Manager SDK is
    /// available to EF tooling but never flows transitively into the desktop/web clients that reference this
    /// project only for the SQL Server EF provider.
    /// </summary>
    public static class SecretManagerConnectionResolver
    {
        /// <summary>
        /// Resolves the SQL Server connection string: from Secret Manager when the <c>GoogleSecretManager</c>
        /// section is enabled and the db* secrets are present, otherwise from <c>ConnectionStrings:DefaultConnection</c>.
        /// </summary>
        public static string? Resolve(IConfiguration config)
        {
            var section = config.GetSection("GoogleSecretManager");

            // Project id / env label come from config, falling back to env vars so `dotnet ef` works even
            // when run from a directory without the API's appsettings.json.
            var projectId = section["ProjectId"]
                ?? Environment.GetEnvironmentVariable("GOOGLE_SECRET_MANAGER_PROJECT_ID");
            var envLabel = section["EnvironmentLabel"]
                ?? Environment.GetEnvironmentVariable("GOOGLE_SECRET_MANAGER_ENV");

            var fallback = config.GetConnectionString("DefaultConnection");

            if (!section.GetValue("Enabled", false) || string.IsNullOrWhiteSpace(projectId))
                return fallback;

            try
            {
                var client = SecretManagerServiceClient.Create();
                var request = new ListSecretsRequest { ParentAsProjectName = new ProjectName(projectId) };
                if (!string.IsNullOrWhiteSpace(envLabel))
                    request.Filter = $"labels.env={envLabel}";

                var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                foreach (var secret in client.ListSecrets(request))
                {
                    var secretId = secret.SecretName.SecretId;
                    try
                    {
                        var version = client.AccessSecretVersion(
                            new SecretVersionName(projectId, secretId, "latest"));
                        data[secretId] = version.Payload.Data.ToStringUtf8();
                    }
                    catch
                    {
                        // Secret with no enabled/latest version — skip it.
                    }
                }

                string? Pick(params string[] names)
                    => names.Select(n => data.TryGetValue(n, out var v) ? v : null)
                            .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

                var server = Pick("dbserver");
                var user = Pick("dbusername", "dbuser");
                var password = Pick("dbpassword", "dbpass");
                var database = Pick("dbname") ?? user; // smartasp.net: database name == user id

                if (!string.IsNullOrWhiteSpace(server) && !string.IsNullOrWhiteSpace(user) &&
                    !string.IsNullOrWhiteSpace(password))
                {
                    return $"Server={server};Database={database};User Id={user};Password={password};" +
                           "TrustServerCertificate=true;MultipleActiveResultSets=true;Encrypt=True";
                }

                Console.Error.WriteLine(
                    "[SqlServerContextFactory] GoogleSecretManager enabled but dbserver/dbusername/dbpassword " +
                    "not all found; falling back to ConnectionStrings:DefaultConnection.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"[SqlServerContextFactory] Secret Manager load failed: {ex.Message}. " +
                    "Falling back to ConnectionStrings:DefaultConnection.");
            }

            return fallback;
        }
    }
}
