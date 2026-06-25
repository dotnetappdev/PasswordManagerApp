namespace VaultGuard.Models.Configuration;

/// <summary>
/// Configuration for Sentry.io error tracking
/// </summary>
public class SentryConfiguration
{
    /// <summary>
    /// Sentry Data Source Name (DSN) for sending error reports
    /// </summary>
    public string Dsn { get; set; } = string.Empty;
    
    /// <summary>
    /// Environment name (e.g., development, staging, production)
    /// </summary>
    public string Environment { get; set; } = "development";
    
    /// <summary>
    /// Sample rate for performance monitoring (0.0 to 1.0)
    /// </summary>
    public double TracesSampleRate { get; set; } = 1.0;
    
    /// <summary>
    /// Whether to send Personally Identifiable Information (PII)
    /// Set to false for privacy
    /// </summary>
    public bool SendDefaultPii { get; set; } = false;
    
    /// <summary>
    /// Whether to attach stack traces to messages
    /// </summary>
    public bool AttachStacktrace { get; set; } = true;
    
    /// <summary>
    /// Enable debug mode for troubleshooting
    /// </summary>
    public bool Debug { get; set; } = false;
    
    /// <summary>
    /// Check if Sentry is configured
    /// </summary>
    public bool IsConfigured => !string.IsNullOrEmpty(Dsn);
}
