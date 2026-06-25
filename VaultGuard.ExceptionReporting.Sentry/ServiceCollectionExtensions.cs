using Microsoft.Extensions.DependencyInjection;
using VaultGuard.ExceptionReporting;

namespace VaultGuard.ExceptionReporting.Sentry;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSentryExceptionReporting(this IServiceCollection services, string? dsn, string environment = "Production")
    {
        if (!string.IsNullOrWhiteSpace(dsn))
        {
            SentryReporterSetup.Initialize(dsn, environment);
            services.AddSingleton<IExceptionReporter, SentryExceptionReporter>();
        }
        else
        {
            services.AddSingleton<IExceptionReporter, NullExceptionReporter>();
        }

        return services;
    }
}
