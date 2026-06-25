using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PasswordManager.Models.Configuration;
using PasswordManager.Services.Interfaces;
using System.Text;
using System.Text.Json;

namespace PasswordManager.Services.Services;

/// <summary>
/// Twilio SMS service implementation
/// </summary>
public class TwilioSmsService : ISmsService
{
    private readonly SmsConfiguration _config;
    private readonly ILogger<TwilioSmsService> _logger;
    private readonly HttpClient _httpClient;

    public string ProviderName => "Twilio";

    public TwilioSmsService(
        IOptions<SmsConfiguration> config,
        ILogger<TwilioSmsService> logger,
        HttpClient httpClient)
    {
        _config = config.Value;
        _logger = logger;
        _httpClient = httpClient;
        
        // Configure HTTP client for Twilio API
        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_config.Twilio.AccountSid}:{_config.Twilio.AuthToken}"));
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
    }

    public async Task<SmsResult> SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!IsConfigured())
            {
                _logger.LogError("Twilio SMS service is not properly configured");
                return SmsResult.Failure("SMS service is not configured");
            }

            if (!IsValidPhoneNumber(phoneNumber))
            {
                _logger.LogWarning("Invalid phone number format: {PhoneNumber}", phoneNumber);
                return SmsResult.Failure("Invalid phone number format");
            }

            var url = $"https://api.twilio.com/2010-04-01/Accounts/{_config.Twilio.AccountSid}/Messages.json";
            
            var formParams = new List<KeyValuePair<string, string>>
            {
                new("To", phoneNumber),
                new("From", _config.Twilio.FromPhoneNumber),
                new("Body", message)
            };

            var formContent = new FormUrlEncodedContent(formParams);
            
            _logger.LogInformation("Sending SMS to {PhoneNumber} via Twilio", MaskPhoneNumber(phoneNumber));
            
            var response = await _httpClient.PostAsync(url, formContent, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var twilioResponse = JsonSerializer.Deserialize<TwilioSmsResponse>(responseContent);
                _logger.LogInformation("SMS sent successfully to {PhoneNumber}, MessageSid: {MessageSid}", 
                    MaskPhoneNumber(phoneNumber), twilioResponse?.Sid);
                
                return SmsResult.Success(twilioResponse?.Sid);
            }
            else
            {
                _logger.LogError("Failed to send SMS via Twilio. Status: {StatusCode}, Response: {Response}", 
                    response.StatusCode, responseContent);
                
                var errorResponse = JsonSerializer.Deserialize<TwilioErrorResponse>(responseContent);
                var errorMessage = errorResponse?.Message ?? "Failed to send SMS";
                
                return SmsResult.Failure(errorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while sending SMS to {PhoneNumber}", MaskPhoneNumber(phoneNumber));
            return SmsResult.Failure("An error occurred while sending SMS");
        }
    }

    public bool IsConfigured()
    {
        return !string.IsNullOrEmpty(_config.Twilio.AccountSid) &&
               !string.IsNullOrEmpty(_config.Twilio.AuthToken) &&
               !string.IsNullOrEmpty(_config.Twilio.FromPhoneNumber) &&
               _config.Enabled;
    }

    private static bool IsValidPhoneNumber(string phoneNumber)
    {
        // Basic E.164 format validation
        return !string.IsNullOrEmpty(phoneNumber) &&
               phoneNumber.StartsWith("+") &&
               phoneNumber.Length >= 8 &&
               phoneNumber.Length <= 17 &&
               phoneNumber.Skip(1).All(char.IsDigit);
    }

    private static string MaskPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 4)
            return "****";
        
        return phoneNumber[..^4] + "****";
    }

    private class TwilioSmsResponse
    {
        public string? Sid { get; set; }
        public string? Status { get; set; }
        public string? To { get; set; }
        public string? From { get; set; }
        public string? Body { get; set; }
    }

    private class TwilioErrorResponse
    {
        public int Code { get; set; }
        public string? Message { get; set; }
        public string? MoreInfo { get; set; }
    }
}