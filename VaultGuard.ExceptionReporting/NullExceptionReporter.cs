namespace VaultGuard.ExceptionReporting;

public class NullExceptionReporter : IExceptionReporter
{
    public void CaptureException(Exception exception, IDictionary<string, string>? context = null)
    {
    }

    public void CaptureMessage(string message, ExceptionReportingLevel level = ExceptionReportingLevel.Error, IDictionary<string, string>? context = null)
    {
    }

    public void SetUser(string? userId, string? email = null)
    {
    }

    public IDisposable? BeginScope(string? operationName = null) => null;
}
