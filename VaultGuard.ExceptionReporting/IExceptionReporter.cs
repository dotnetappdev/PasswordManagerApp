namespace VaultGuard.ExceptionReporting;

public interface IExceptionReporter
{
    void CaptureException(Exception exception, IDictionary<string, string>? context = null);

    void CaptureMessage(string message, ExceptionReportingLevel level = ExceptionReportingLevel.Error, IDictionary<string, string>? context = null);

    void SetUser(string? userId, string? email = null);

    IDisposable? BeginScope(string? operationName = null);
}
