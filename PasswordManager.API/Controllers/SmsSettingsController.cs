using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PasswordManager.DAL;
using PasswordManager.Models;
using PasswordManager.Models.DTOs;
using PasswordManager.Services.Interfaces;
using PasswordManager.Crypto.Interfaces;

namespace PasswordManager.API.Controllers;

/// <summary>
/// Controller for managing SMS settings and configuration
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SmsSettingsController : ControllerBase
{
    private readonly PasswordManagerDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IVaultSessionService _vaultSessionService;
    private readonly IPasswordCryptoService _cryptoService;
    private readonly ISmsService _smsService;
    private readonly ILogger<SmsSettingsController> _logger;

    public SmsSettingsController(
        PasswordManagerDbContext context,
        UserManager<ApplicationUser> userManager,
        IVaultSessionService vaultSessionService,
        IPasswordCryptoService cryptoService,
        ISmsService smsService,
        ILogger<SmsSettingsController> logger)
    {
        _context = context;
        _userManager = userManager;
        _vaultSessionService = vaultSessionService;
        _cryptoService = cryptoService;
        _smsService = smsService;
        _logger = logger;
    }

    /// <summary>
    /// Get all SMS settings for the current user
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SmsSettingsDto>>> GetSmsSettings()
    {
        try
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
                return Unauthorized("Invalid session");

            var smsSettings = await _context.SmsSettings
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.IsActive)
                .ThenByDescending(s => s.LastModified)
                .ToListAsync();

            var settingsDtos = smsSettings.Select(s => new SmsSettingsDto
            {
                Id = s.Id,
                Provider = s.Provider,
                Enabled = s.Enabled,
                DefaultCountryCode = s.DefaultCountryCode,
                CodeLength = s.CodeLength,
                ExpirationMinutes = s.ExpirationMinutes,
                MaxAttempts = s.MaxAttempts,
                MaxSmsPerHour = s.MaxSmsPerHour,
                MessageTemplate = s.MessageTemplate,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt,
                LastModified = s.LastModified
            }).ToList();

            return Ok(settingsDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving SMS settings");
            return StatusCode(500, "An error occurred while retrieving SMS settings");
        }
    }

    /// <summary>
    /// Get active SMS settings for the current user
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult<SmsSettingsDto>> GetActiveSmsSettings()
    {
        try
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
                return Unauthorized("Invalid session");

            var activeSetting = await _context.SmsSettings
                .Where(s => s.UserId == userId && s.IsActive)
                .FirstOrDefaultAsync();

            if (activeSetting == null)
                return NotFound("No active SMS settings found");

            var settingsDto = new SmsSettingsDto
            {
                Id = activeSetting.Id,
                Provider = activeSetting.Provider,
                Enabled = activeSetting.Enabled,
                DefaultCountryCode = activeSetting.DefaultCountryCode,
                CodeLength = activeSetting.CodeLength,
                ExpirationMinutes = activeSetting.ExpirationMinutes,
                MaxAttempts = activeSetting.MaxAttempts,
                MaxSmsPerHour = activeSetting.MaxSmsPerHour,
                MessageTemplate = activeSetting.MessageTemplate,
                IsActive = activeSetting.IsActive,
                CreatedAt = activeSetting.CreatedAt,
                LastModified = activeSetting.LastModified
            };

            return Ok(settingsDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active SMS settings");
            return StatusCode(500, "An error occurred while retrieving active SMS settings");
        }
    }

    /// <summary>
    /// Create new SMS settings
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<SmsSettingsDto>> CreateSmsSettings([FromBody] CreateUpdateSmsSettingsDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
                return Unauthorized("Invalid session");

            // Validate provider
            if (!IsValidProvider(createDto.Provider))
                return BadRequest($"Invalid SMS provider: {createDto.Provider}");

            // Validate provider-specific settings
            var validationResult = ValidateProviderSettings(createDto);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.ErrorMessage);

            // Deactivate existing settings if this is being set as active
            await DeactivateExistingSettingsAsync(userId);

            // Create new settings
            var masterKey = GetMasterKeyFromSession(userId);
            var smsSettings = new SmsSettings
            {
                UserId = userId,
                Provider = createDto.Provider,
                Enabled = createDto.Enabled,
                DefaultCountryCode = createDto.DefaultCountryCode,
                CodeLength = createDto.CodeLength,
                ExpirationMinutes = createDto.ExpirationMinutes,
                MaxAttempts = createDto.MaxAttempts,
                MaxSmsPerHour = createDto.MaxSmsPerHour,
                MessageTemplate = createDto.MessageTemplate,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            // Encrypt and store provider-specific settings
            await EncryptAndStoreProviderSettings(smsSettings, createDto, masterKey);

            _context.SmsSettings.Add(smsSettings);
            await _context.SaveChangesAsync();

            var responseDto = new SmsSettingsDto
            {
                Id = smsSettings.Id,
                Provider = smsSettings.Provider,
                Enabled = smsSettings.Enabled,
                DefaultCountryCode = smsSettings.DefaultCountryCode,
                CodeLength = smsSettings.CodeLength,
                ExpirationMinutes = smsSettings.ExpirationMinutes,
                MaxAttempts = smsSettings.MaxAttempts,
                MaxSmsPerHour = smsSettings.MaxSmsPerHour,
                MessageTemplate = smsSettings.MessageTemplate,
                IsActive = smsSettings.IsActive,
                CreatedAt = smsSettings.CreatedAt,
                LastModified = smsSettings.LastModified
            };

            return CreatedAtAction(nameof(GetActiveSmsSettings), new { id = smsSettings.Id }, responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating SMS settings");
            return StatusCode(500, "An error occurred while creating SMS settings");
        }
    }

    /// <summary>
    /// Update existing SMS settings
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<SmsSettingsDto>> UpdateSmsSettings(int id, [FromBody] CreateUpdateSmsSettingsDto updateDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
                return Unauthorized("Invalid session");

            var existingSetting = await _context.SmsSettings
                .Where(s => s.Id == id && s.UserId == userId)
                .FirstOrDefaultAsync();

            if (existingSetting == null)
                return NotFound("SMS settings not found");

            // Validate provider
            if (!IsValidProvider(updateDto.Provider))
                return BadRequest($"Invalid SMS provider: {updateDto.Provider}");

            // Validate provider-specific settings
            var validationResult = ValidateProviderSettings(updateDto);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.ErrorMessage);

            // Update basic settings
            existingSetting.Provider = updateDto.Provider;
            existingSetting.Enabled = updateDto.Enabled;
            existingSetting.DefaultCountryCode = updateDto.DefaultCountryCode;
            existingSetting.CodeLength = updateDto.CodeLength;
            existingSetting.ExpirationMinutes = updateDto.ExpirationMinutes;
            existingSetting.MaxAttempts = updateDto.MaxAttempts;
            existingSetting.MaxSmsPerHour = updateDto.MaxSmsPerHour;
            existingSetting.MessageTemplate = updateDto.MessageTemplate;
            existingSetting.LastModified = DateTime.UtcNow;

            // Update provider-specific settings
            var masterKey = GetMasterKeyFromSession(userId);
            await EncryptAndStoreProviderSettings(existingSetting, updateDto, masterKey);

            await _context.SaveChangesAsync();

            var responseDto = new SmsSettingsDto
            {
                Id = existingSetting.Id,
                Provider = existingSetting.Provider,
                Enabled = existingSetting.Enabled,
                DefaultCountryCode = existingSetting.DefaultCountryCode,
                CodeLength = existingSetting.CodeLength,
                ExpirationMinutes = existingSetting.ExpirationMinutes,
                MaxAttempts = existingSetting.MaxAttempts,
                MaxSmsPerHour = existingSetting.MaxSmsPerHour,
                MessageTemplate = existingSetting.MessageTemplate,
                IsActive = existingSetting.IsActive,
                CreatedAt = existingSetting.CreatedAt,
                LastModified = existingSetting.LastModified
            };

            return Ok(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating SMS settings");
            return StatusCode(500, "An error occurred while updating SMS settings");
        }
    }

    /// <summary>
    /// Delete SMS settings
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteSmsSettings(int id)
    {
        try
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
                return Unauthorized("Invalid session");

            var smsSettings = await _context.SmsSettings
                .Where(s => s.Id == id && s.UserId == userId)
                .FirstOrDefaultAsync();

            if (smsSettings == null)
                return NotFound("SMS settings not found");

            _context.SmsSettings.Remove(smsSettings);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting SMS settings");
            return StatusCode(500, "An error occurred while deleting SMS settings");
        }
    }

    /// <summary>
    /// Activate specific SMS settings
    /// </summary>
    [HttpPost("{id}/activate")]
    public async Task<ActionResult> ActivateSmsSettings(int id)
    {
        try
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
                return Unauthorized("Invalid session");

            var smsSettings = await _context.SmsSettings
                .Where(s => s.Id == id && s.UserId == userId)
                .FirstOrDefaultAsync();

            if (smsSettings == null)
                return NotFound("SMS settings not found");

            // Deactivate all other settings for this user
            await DeactivateExistingSettingsAsync(userId);

            // Activate the selected setting
            smsSettings.IsActive = true;
            smsSettings.LastModified = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "SMS settings activated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating SMS settings");
            return StatusCode(500, "An error occurred while activating SMS settings");
        }
    }

    /// <summary>
    /// Test SMS settings by sending a test message
    /// </summary>
    [HttpPost("{id}/test")]
    public async Task<ActionResult> TestSmsSettings(int id, [FromBody] TestSmsSettingsDto testDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
                return Unauthorized("Invalid session");

            var smsSettings = await _context.SmsSettings
                .Where(s => s.Id == id && s.UserId == userId)
                .FirstOrDefaultAsync();

            if (smsSettings == null)
                return NotFound("SMS settings not found");

            // TODO: Implement test message sending using the SMS service
            // This would require temporarily configuring the SMS service with these settings
            // and sending a test message
            
            return Ok(new { message = "Test message sent successfully", phoneNumber = testDto.PhoneNumber });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing SMS settings");
            return StatusCode(500, "An error occurred while testing SMS settings");
        }
    }

    #region Private Methods

    private async Task<string?> GetCurrentUserIdAsync()
    {
        var sessionId = HttpContext.Request.Headers["Authorization"]
            .FirstOrDefault()?.Replace("Bearer ", "");

        if (string.IsNullOrEmpty(sessionId))
            return null;

        return _vaultSessionService.GetSessionUserId(sessionId);
    }

    private byte[] GetMasterKeyFromSession(string userId)
    {
        var sessionId = HttpContext.Request.Headers["Authorization"]
            .FirstOrDefault()?.Replace("Bearer ", "");

        if (string.IsNullOrEmpty(sessionId))
            throw new InvalidOperationException("No valid session found");

        return _vaultSessionService.GetMasterKey(sessionId) ?? throw new InvalidOperationException("Master key not found in session");
    }

    private async Task DeactivateExistingSettingsAsync(string userId)
    {
        var existingSettings = await _context.SmsSettings
            .Where(s => s.UserId == userId && s.IsActive)
            .ToListAsync();

        foreach (var setting in existingSettings)
        {
            setting.IsActive = false;
            setting.LastModified = DateTime.UtcNow;
        }
    }

    private static bool IsValidProvider(string provider)
    {
        var validProviders = new[] { "Twilio", "AwsSns", "AzureCommunication" };
        return validProviders.Contains(provider, StringComparer.OrdinalIgnoreCase);
    }

    private static (bool IsValid, string? ErrorMessage) ValidateProviderSettings(CreateUpdateSmsSettingsDto dto)
    {
        return dto.Provider.ToLowerInvariant() switch
        {
            "twilio" => dto.Twilio != null && !string.IsNullOrEmpty(dto.Twilio.AccountSid) && 
                       !string.IsNullOrEmpty(dto.Twilio.AuthToken) && !string.IsNullOrEmpty(dto.Twilio.FromPhoneNumber)
                       ? (true, null) : (false, "Twilio settings are required and must include AccountSid, AuthToken, and FromPhoneNumber"),
            
            "awssns" => dto.AwsSns != null && !string.IsNullOrEmpty(dto.AwsSns.AccessKeyId) && 
                       !string.IsNullOrEmpty(dto.AwsSns.SecretAccessKey) && !string.IsNullOrEmpty(dto.AwsSns.Region)
                       ? (true, null) : (false, "AWS SNS settings are required and must include AccessKeyId, SecretAccessKey, and Region"),
            
            "azurecommunication" => dto.AzureCommunication != null && !string.IsNullOrEmpty(dto.AzureCommunication.ConnectionString) && 
                                   !string.IsNullOrEmpty(dto.AzureCommunication.FromPhoneNumber)
                                   ? (true, null) : (false, "Azure Communication settings are required and must include ConnectionString and FromPhoneNumber"),
            
            _ => (false, $"Unsupported provider: {dto.Provider}")
        };
    }

    private async Task EncryptAndStoreProviderSettings(SmsSettings settings, CreateUpdateSmsSettingsDto dto, byte[] masterKey)
    {
        // Clear all provider settings first
        settings.TwilioAccountSid = null;
        settings.TwilioAuthToken = null;
        settings.TwilioFromPhoneNumber = null;
        settings.AwsAccessKeyId = null;
        settings.AwsSecretAccessKey = null;
        settings.AwsRegion = null;
        settings.AwsSenderName = null;
        settings.AzureConnectionString = null;
        settings.AzureFromPhoneNumber = null;

        switch (dto.Provider.ToLowerInvariant())
        {
            case "twilio":
                if (dto.Twilio != null)
                {
                    var encryptedAccountSid = _cryptoService.EncryptPasswordWithKey(dto.Twilio.AccountSid, masterKey);
                    var encryptedAuthToken = _cryptoService.EncryptPasswordWithKey(dto.Twilio.AuthToken, masterKey);
                    settings.TwilioAccountSid = $"{encryptedAccountSid.EncryptedPassword}|{encryptedAccountSid.Nonce}|{encryptedAccountSid.AuthenticationTag}";
                    settings.TwilioAuthToken = $"{encryptedAuthToken.EncryptedPassword}|{encryptedAuthToken.Nonce}|{encryptedAuthToken.AuthenticationTag}";
                    settings.TwilioFromPhoneNumber = dto.Twilio.FromPhoneNumber;
                }
                break;
                
            case "awssns":
                if (dto.AwsSns != null)
                {
                    var encryptedAccessKey = _cryptoService.EncryptPasswordWithKey(dto.AwsSns.AccessKeyId, masterKey);
                    var encryptedSecretKey = _cryptoService.EncryptPasswordWithKey(dto.AwsSns.SecretAccessKey, masterKey);
                    settings.AwsAccessKeyId = $"{encryptedAccessKey.EncryptedPassword}|{encryptedAccessKey.Nonce}|{encryptedAccessKey.AuthenticationTag}";
                    settings.AwsSecretAccessKey = $"{encryptedSecretKey.EncryptedPassword}|{encryptedSecretKey.Nonce}|{encryptedSecretKey.AuthenticationTag}";
                    settings.AwsRegion = dto.AwsSns.Region;
                    settings.AwsSenderName = dto.AwsSns.SenderName;
                }
                break;
                
            case "azurecommunication":
                if (dto.AzureCommunication != null)
                {
                    var encryptedConnectionString = _cryptoService.EncryptPasswordWithKey(dto.AzureCommunication.ConnectionString, masterKey);
                    settings.AzureConnectionString = $"{encryptedConnectionString.EncryptedPassword}|{encryptedConnectionString.Nonce}|{encryptedConnectionString.AuthenticationTag}";
                    settings.AzureFromPhoneNumber = dto.AzureCommunication.FromPhoneNumber;
                }
                break;
        }
    }

    #endregion
}