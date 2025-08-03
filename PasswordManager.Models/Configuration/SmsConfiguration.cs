using System.ComponentModel.DataAnnotations;

namespace PasswordManager.Models.Configuration;

public class SmsConfiguration
{
    public const string SectionName = "SmsSettings";
    
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
    /// SMS message template. Use {code} placeholder for the OTP code
    /// </summary>
    public string MessageTemplate { get; set; } = "Your Password Manager verification code is: {code}. This code will expire in {expiration} minutes.";
    
    /// <summary>
    /// Provider-specific settings
    /// </summary>
    public TwilioSettings Twilio { get; set; } = new();
    public AwsSnsSettings AwsSns { get; set; } = new();
    public AzureCommunicationSettings AzureCommunication { get; set; } = new();
}

public class TwilioSettings
{
    [Required]
    public string AccountSid { get; set; } = string.Empty;
    
    [Required]
    public string AuthToken { get; set; } = string.Empty;
    
    [Required]
    public string FromPhoneNumber { get; set; } = string.Empty;
}

public class AwsSnsSettings
{
    [Required]
    public string AccessKeyId { get; set; } = string.Empty;
    
    [Required]
    public string SecretAccessKey { get; set; } = string.Empty;
    
    [Required]
    public string Region { get; set; } = "us-east-1";
    
    public string SenderName { get; set; } = "Password Manager";
}

public class AzureCommunicationSettings
{
    [Required]
    public string ConnectionString { get; set; } = string.Empty;
    
    [Required]
    public string FromPhoneNumber { get; set; } = string.Empty;
}