namespace PasswordManager.Services.Interfaces;

/// <summary>
/// Interface for SMS service providers
/// </summary>
public interface ISmsService
{
    /// <summary>
    /// Send an SMS message
    /// </summary>
    /// <param name="phoneNumber">Phone number in E.164 format</param>
    /// <param name="message">Message content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SMS delivery result</returns>
    Task<SmsResult> SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if the provider is properly configured
    /// </summary>
    /// <returns>True if configured and ready to send</returns>
    bool IsConfigured();
    
    /// <summary>
    /// Get the provider name
    /// </summary>
    string ProviderName { get; }
}

/// <summary>
/// Result of SMS sending operation
/// </summary>
public class SmsResult
{
    public bool IsSuccess { get; set; }
    public string? MessageId { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    
    public static SmsResult Success(string? messageId = null) => new()
    {
        IsSuccess = true,
        MessageId = messageId
    };
    
    public static SmsResult Failure(string errorMessage) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage
    };
}

/// <summary>
/// Interface for OTP (One-Time Passcode) service
/// </summary>
public interface IOtpService
{
    /// <summary>
    /// Generate and send OTP code via SMS
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="phoneNumber">Phone number in E.164 format</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>OTP result</returns>
    Task<OtpResult> SendOtpAsync(string userId, string phoneNumber, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Verify OTP code
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="code">OTP code to verify</param>
    /// <param name="removeOnSuccess">Whether to remove the code after successful verification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Verification result</returns>
    Task<OtpVerificationResult> VerifyOtpAsync(string userId, string code, bool removeOnSuccess = true, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generate backup codes for account recovery
    /// </summary>
    /// <param name="count">Number of backup codes to generate</param>
    /// <returns>List of backup codes</returns>
    List<string> GenerateBackupCodes(int count = 10);
    
    /// <summary>
    /// Verify backup code
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="backupCode">Backup code to verify</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Verification result</returns>
    Task<bool> VerifyBackupCodeAsync(string userId, string backupCode, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Clear all pending OTP codes for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ClearPendingOtpAsync(string userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of OTP generation and sending
/// </summary>
public class OtpResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int AttemptsRemaining { get; set; }
    
    public static OtpResult Success(DateTime expiresAt, int attemptsRemaining) => new()
    {
        IsSuccess = true,
        ExpiresAt = expiresAt,
        AttemptsRemaining = attemptsRemaining
    };
    
    public static OtpResult Failure(string errorMessage) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage
    };
}

/// <summary>
/// Result of OTP verification
/// </summary>
public class OtpVerificationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public int AttemptsRemaining { get; set; }
    public bool IsLocked { get; set; }
    public DateTime? LockoutEndsAt { get; set; }
    
    public static OtpVerificationResult Valid() => new()
    {
        IsValid = true
    };
    
    public static OtpVerificationResult Invalid(string errorMessage, int attemptsRemaining, bool isLocked = false, DateTime? lockoutEndsAt = null) => new()
    {
        IsValid = false,
        ErrorMessage = errorMessage,
        AttemptsRemaining = attemptsRemaining,
        IsLocked = isLocked,
        LockoutEndsAt = lockoutEndsAt
    };
}