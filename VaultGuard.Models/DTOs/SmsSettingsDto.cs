using System.ComponentModel.DataAnnotations;

namespace PasswordManager.Models.DTOs;

/// <summary>
/// DTO for SMS settings API operations
/// </summary>
public class SmsSettingsDto
{
    public int? Id { get; set; }

    /// <summary>
    /// SMS provider to use (Twilio, AWS SNS, Azure Communication Services, etc.)
    /// </summary>
    [Required]
    public string Provider { get; set; } = "Twilio";

    /// <summary>
    /// Whether SMS OTP is enabled globally
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Default country code for phone numbers
    /// </summary>
    public string DefaultCountryCode { get; set; } = "+1";

    /// <summary>
    /// OTP code length
    /// </summary>
    [Range(4, 8)]
    public int CodeLength { get; set; } = 6;

    /// <summary>
    /// OTP expiration time in minutes
    /// </summary>
    [Range(1, 30)]
    public int ExpirationMinutes { get; set; } = 5;

    /// <summary>
    /// Maximum OTP attempts before lockout
    /// </summary>
    [Range(3, 10)]
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Rate limiting: Maximum SMS per hour per user
    /// </summary>
    [Range(1, 100)]
    public int MaxSmsPerHour { get; set; } = 10;

    /// <summary>
    /// SMS message template
    /// </summary>
    public string MessageTemplate { get; set; } = "Your Password Manager verification code is: {code}. This code will expire in {expiration} minutes.";

    /// <summary>
    /// Whether this is the active SMS configuration
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime? CreatedAt { get; set; }
    public DateTime? LastModified { get; set; }
}

/// <summary>
/// DTO for creating or updating SMS settings
/// </summary>
public class CreateUpdateSmsSettingsDto
{
    /// <summary>
    /// SMS provider to use (Twilio, AwsSns, AzureCommunication)
    /// </summary>
    [Required]
    public string Provider { get; set; } = "Twilio";

    /// <summary>
    /// Whether SMS OTP is enabled globally
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Default country code for phone numbers
    /// </summary>
    public string DefaultCountryCode { get; set; } = "+1";

    /// <summary>
    /// OTP code length
    /// </summary>
    [Range(4, 8)]
    public int CodeLength { get; set; } = 6;

    /// <summary>
    /// OTP expiration time in minutes
    /// </summary>
    [Range(1, 30)]
    public int ExpirationMinutes { get; set; } = 5;

    /// <summary>
    /// Maximum OTP attempts before lockout
    /// </summary>
    [Range(3, 10)]
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Rate limiting: Maximum SMS per hour per user
    /// </summary>
    [Range(1, 100)]
    public int MaxSmsPerHour { get; set; } = 10;

    /// <summary>
    /// SMS message template
    /// </summary>
    public string MessageTemplate { get; set; } = "Your Password Manager verification code is: {code}. This code will expire in {expiration} minutes.";

    /// <summary>
    /// Provider-specific settings
    /// </summary>
    public TwilioSettingsDto? Twilio { get; set; }
    public AwsSnsSettingsDto? AwsSns { get; set; }
    public AzureCommunicationSettingsDto? AzureCommunication { get; set; }
}

/// <summary>
/// DTO for Twilio settings
/// </summary>
public class TwilioSettingsDto
{
    [Required]
    public string AccountSid { get; set; } = string.Empty;

    [Required]
    public string AuthToken { get; set; } = string.Empty;

    [Required]
    public string FromPhoneNumber { get; set; } = string.Empty;
}

/// <summary>
/// DTO for AWS SNS settings
/// </summary>
public class AwsSnsSettingsDto
{
    [Required]
    public string AccessKeyId { get; set; } = string.Empty;

    [Required]
    public string SecretAccessKey { get; set; } = string.Empty;

    [Required]
    public string Region { get; set; } = "us-east-1";

    public string SenderName { get; set; } = "Password Manager";
}

/// <summary>
/// DTO for Azure Communication Services settings
/// </summary>
public class AzureCommunicationSettingsDto
{
    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    [Required]
    public string FromPhoneNumber { get; set; } = string.Empty;
}

/// <summary>
/// DTO for testing SMS settings
/// </summary>
public class TestSmsSettingsDto
{
    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;

    public string? TestMessage { get; set; } = "Test message from Password Manager SMS configuration.";
}