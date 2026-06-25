using Sentry;
using VaultGuard.ExceptionReporting;

namespace VaultGuard.ExceptionReporting.Sentry;

public class SentryExceptionReporter : IExceptionReporter
{
    public void CaptureException(Exception exception, IDictionary<string, string>? context = null)
    {
        if (context == null || context.Count == 0)
        {
            SentrySdk.CaptureException(exception);
            return;
        }

        SentrySdk.CaptureException(exception, scope =>
        {
            foreach (var kvp in context)
            {
                scope.SetExtra(kvp.Key, kvp.Value);
            }
        });
    }

    public void CaptureMessage(string message, ExceptionReportingLevel level = ExceptionReportingLevel.Error, IDictionary<string, string>? context = null)
    {
        var sentryLevel = MapLevel(level);

        if (context == null || context.Count == 0)
        {
            SentrySdk.CaptureMessage(message, sentryLevel);
            return;
        }

        using (SentrySdk.PushScope())
        {
            SentrySdk.ConfigureScope(scope =>
            {
                foreach (var kvp in context)
                {
                    scope.SetExtra(kvp.Key, kvp.Value);
                }
            });
            SentrySdk.CaptureMessage(message, sentryLevel);
        }
    }

    public void SetUser(string? userId, string? email = null)
    {
        SentrySdk.ConfigureScope(scope =>
        {
            scope.User = new SentryUser
            {
                Id = userId,
                Email = email
            };
        });
    }

    public IDisposable? BeginScope(string? operationName = null)
    {
        if (operationName == null) return null;

        var disposable = SentrySdk.PushScope();
        SentrySdk.ConfigureScope(scope => scope.SetTag("operation", operationName));
        return disposable;
    }

    private static SentryLevel MapLevel(ExceptionReportingLevel level) => level switch
    {
        ExceptionReportingLevel.Debug => SentryLevel.Debug,
        ExceptionReportingLevel.Info => SentryLevel.Info,
        ExceptionReportingLevel.Warning => SentryLevel.Warning,
        ExceptionReportingLevel.Error => SentryLevel.Error,
        ExceptionReportingLevel.Fatal => SentryLevel.Fatal,
        _ => SentryLevel.Error
    };
}
