namespace VaultGuard.ExceptionReporting.Sentry;

public static class SentryReporterSetup
{
    public static IDisposable? Initialize(string? dsn, string environment = "Production")
    {
        if (string.IsNullOrWhiteSpace(dsn)) return null;

        return global::Sentry.SentrySdk.Init(o =>
        {
            o.Dsn = dsn;
            o.Environment = environment;
        });
    }
}
