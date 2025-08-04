using System.ComponentModel.DataAnnotations;

namespace PasswordManager.Models;

/// <summary>
/// Database entity for storing SMS configuration settings
/// </summary>
public class SmsSettings
{
    /// <summary>
    /// Primary key for the settings record
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// SMS provider to use (Twilio, AWS SNS, Azure Communication Services, etc.)
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Provider { get; set; } = "Twilio";

    /// <summary>
    /// Whether SMS OTP is enabled globally
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Default country code for phone numbers
    /// </summary>
    [StringLength(5)]
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
    /// SMS message template. Use {code} placeholder for the OTP code and {expiration} for expiration time
    /// </summary>
    [StringLength(500)]
    public string MessageTemplate { get; set; } = "Your Password Manager verification code is: {code}. This code will expire in {expiration} minutes.";

    /// <summary>
    /// Twilio Account SID (encrypted)
    /// </summary>
    [StringLength(1000)]
    public string? TwilioAccountSid { get; set; }

    /// <summary>
    /// Twilio Auth Token (encrypted)
    /// </summary>
    [StringLength(1000)]
    public string? TwilioAuthToken { get; set; }

    /// <summary>
    /// Twilio From Phone Number
    /// </summary>
    [StringLength(20)]
    public string? TwilioFromPhoneNumber { get; set; }

    /// <summary>
    /// AWS SNS Access Key ID (encrypted)
    /// </summary>
    [StringLength(1000)]
    public string? AwsAccessKeyId { get; set; }

    /// <summary>
    /// AWS SNS Secret Access Key (encrypted)
    /// </summary>
    [StringLength(1000)]
    public string? AwsSecretAccessKey { get; set; }

    /// <summary>
    /// AWS SNS Region
    /// </summary>
    [StringLength(50)]
    public string? AwsRegion { get; set; } = "us-east-1";

    /// <summary>
    /// AWS SNS Sender Name
    /// </summary>
    [StringLength(100)]
    public string? AwsSenderName { get; set; } = "Password Manager";

    /// <summary>
    /// Azure Communication Services Connection String (encrypted)
    /// </summary>
    [StringLength(1000)]
    public string? AzureConnectionString { get; set; }

    /// <summary>
    /// Azure Communication Services From Phone Number
    /// </summary>
    [StringLength(20)]
    public string? AzureFromPhoneNumber { get; set; }

    /// <summary>
    /// When the settings were created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the settings were last modified
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who created/modified these settings
    /// </summary>
    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is the active SMS configuration
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Navigation property to the user who owns these settings
    /// </summary>
    public ApplicationUser User { get; set; } = null!;
}