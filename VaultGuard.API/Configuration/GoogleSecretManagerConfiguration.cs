using Google.Api.Gax.ResourceNames;
using Google.Cloud.SecretManager.V1;
using Microsoft.Extensions.Configuration;

namespace VaultGuard.API.Configuration;

/// <summary>
/// Options for the Google Cloud Secret Manager integration, bound from the "GoogleSecretManager" config
/// section. Secrets live in a GCP project named per-environment: <c>vaultguard-dev</c> for development and
/// <c>vaultguard-prod</c> for production. No credentials are committed — authentication uses Application
/// Default Credentials (an attached service account on GCP; <c>gcloud auth application-default login</c> or
/// a <c>GOOGLE_APPLICATION_CREDENTIALS</c> service-account key file locally).
/// </summary>
public sealed class GoogleSecretManagerOptions
{
    public const string SectionName = "GoogleSecretManager";

    /// <summary>When false the provider is a no-op and appsettings.json values are used as-is.</summary>
    public bool Enabled { get; set; }

    /// <summary>The GCP project id holding the secrets, e.g. <c>project-e96017a0-d8f6-420f-955</c>.</summary>
    public string? ProjectId { get; set; }

    /// <summary>
    /// Optional label filter. When set, only secrets carrying the label <c>env={EnvironmentLabel}</c> are
    /// loaded (e.g. "dev" or "prod"). This lets a single project hold several environments' secrets, or simply
    /// scopes the load to the intended environment. When null/empty, all secrets in the project are loaded.
    /// </summary>
    public string? EnvironmentLabel { get; set; }

    /// <summary>
    /// When true, a failure to reach Secret Manager or authenticate is logged and startup continues using
    /// appsettings values. When false, such a failure throws and stops the app (fail-closed).
    /// </summary>
    public bool Optional { get; set; } = true;
}

/// <summary>
/// Loads secrets from Google Cloud Secret Manager into <see cref="IConfiguration"/> so existing
/// <c>Configuration["..."]</c> reads (connection strings, JWT key, Sentry DSN, SMS/Supabase credentials)
/// transparently resolve from the project's secrets. Secret ids may contain letters, digits, underscores
/// and dashes, so nesting is expressed with a double underscore — e.g. the secret <c>JwtSettings__SecretKey</c>
/// becomes the config key <c>JwtSettings:SecretKey</c>. The three database part secrets
/// (<c>dbserver</c>, <c>dbusername</c>, <c>dbpassword</c>) are composed into a SQL Server connection string.
/// </summary>
public sealed class GoogleSecretManagerConfigurationSource : IConfigurationSource
{
    private readonly GoogleSecretManagerOptions _options;

    public GoogleSecretManagerConfigurationSource(GoogleSecretManagerOptions options) => _options = options;

    public IConfigurationProvider Build(IConfigurationBuilder builder)
        => new GoogleSecretManagerConfigurationProvider(_options);
}

public sealed class GoogleSecretManagerConfigurationProvider : ConfigurationProvider
{
    private readonly GoogleSecretManagerOptions _options;

    public GoogleSecretManagerConfigurationProvider(GoogleSecretManagerOptions options) => _options = options;

    public override void Load()
    {
        if (!_options.Enabled)
            return;

        if (string.IsNullOrWhiteSpace(_options.ProjectId))
        {
            Fail("Google Secret Manager is enabled but ProjectId is not set.");
            return;
        }

        try
        {
            var client = SecretManagerServiceClient.Create();
            var projectName = new ProjectName(_options.ProjectId);

            // Optionally scope the load to one environment's secrets via the "env" label
            // (e.g. labels.env=dev), so a build only ever sees its own credentials.
            var request = new ListSecretsRequest { ParentAsProjectName = projectName };
            if (!string.IsNullOrWhiteSpace(_options.EnvironmentLabel))
                request.Filter = $"labels.env={_options.EnvironmentLabel}";

            var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (Secret secret in client.ListSecrets(request))
            {
                var secretId = secret.SecretName.SecretId;
                try
                {
                    // Read the current ("latest") enabled version's payload.
                    var versionName = new SecretVersionName(_options.ProjectId, secretId, "latest");
                    AccessSecretVersionResponse response = client.AccessSecretVersion(versionName);
                    var value = response.Payload.Data.ToStringUtf8();

                    // Map "Section__Key" → "Section:Key" so nested config binds correctly (Secret Manager
                    // ids can't contain ':', so a double underscore is the nesting convention).
                    var configKey = secretId.Replace("__", ConfigurationPath.KeyDelimiter);
                    data[configKey] = value;
                }
                catch (Exception ex)
                {
                    // A secret with no enabled/latest version — skip it rather than aborting the whole load.
                    Console.Error.WriteLine($"[GoogleSecretManager] Skipping secret '{secretId}': {ex.Message}");
                }
            }

            ComposeSqlServerConnection(data);

            Data = data;
            Console.WriteLine($"[GoogleSecretManager] Loaded {data.Count} secret(s) from project '{_options.ProjectId}'.");
        }
        catch (Exception ex)
        {
            Fail($"Failed to load secrets from Google Secret Manager: {ex.Message}");
        }
    }

    /// <summary>
    /// If the project holds the database as separate parts rather than a full connection string, assemble
    /// <c>ConnectionStrings:DefaultConnection</c> from them and select the SQL Server provider. Uses the
    /// three agreed secret names (case-insensitive): <c>dbserver</c>, <c>dbusername</c>, <c>dbpassword</c>.
    /// Following the smartasp.net convention the database name doubles as the SQL login, so the database
    /// defaults to <c>dbusername</c> unless an explicit <c>dbname</c> secret is present. A full
    /// <c>ConnectionStrings__DefaultConnection</c> secret, if present, always wins.
    /// </summary>
    private static void ComposeSqlServerConnection(IDictionary<string, string?> data)
    {
        if (data.ContainsKey("ConnectionStrings:DefaultConnection"))
            return; // an explicit connection string secret takes precedence

        string? Pick(params string[] names)
            => names.Select(n => data.TryGetValue(n, out var v) ? v : null)
                    .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

        var server = Pick("dbserver");
        var user = Pick("dbusername", "dbuser");
        var password = Pick("dbpassword", "dbpass");
        var database = Pick("dbname") ?? user; // smartasp.net: database name == user id

        if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(user) ||
            string.IsNullOrWhiteSpace(password))
            return; // not enough parts to build a connection string

        data["ConnectionStrings:DefaultConnection"] =
            $"Server={server};Database={database};User Id={user};Password={password};" +
            "TrustServerCertificate=true;MultipleActiveResultSets=true;Encrypt=True";

        // Only steer the provider to SQL Server if the project didn't set it explicitly.
        if (!data.ContainsKey("DatabaseProvider"))
            data["DatabaseProvider"] = "SqlServer";

        Console.WriteLine("[GoogleSecretManager] Composed SQL Server connection string from db* secret parts.");
    }

    private void Fail(string message)
    {
        if (_options.Optional)
            Console.Error.WriteLine($"[GoogleSecretManager] {message} Continuing with local configuration.");
        else
            throw new InvalidOperationException($"[GoogleSecretManager] {message}");
    }
}

/// <summary>Extension helpers for wiring Google Secret Manager into the configuration pipeline.</summary>
public static class GoogleSecretManagerConfigurationExtensions
{
    /// <summary>
    /// Adds Google Cloud Secret Manager as a configuration source. Reads the "GoogleSecretManager" section
    /// from the configuration built so far (so it can itself be configured via appsettings/env), then layers
    /// the fetched secrets on top so they override local defaults.
    /// </summary>
    public static IConfigurationBuilder AddGoogleSecretManager(this IConfigurationBuilder builder)
    {
        var options = builder.Build().GetSection(GoogleSecretManagerOptions.SectionName).Get<GoogleSecretManagerOptions>()
                      ?? new GoogleSecretManagerOptions();
        if (!options.Enabled)
            return builder;

        return builder.Add(new GoogleSecretManagerConfigurationSource(options));
    }
}
