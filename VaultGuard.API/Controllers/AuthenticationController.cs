using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VaultGuard.Crypto.Interfaces;
using VaultGuard.Services.Interfaces;
using VaultGuard.Models;
using System.ComponentModel.DataAnnotations;

namespace VaultGuard.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly IApiKeyService _apiKeyService;
        private readonly IUserProfileService _userProfileService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPasswordCryptoService _passwordCryptoService;
        private readonly ILogger<AuthenticationController> _logger;

        public AuthenticationController(
            IApiKeyService apiKeyService,
            IUserProfileService userProfileService,
            UserManager<ApplicationUser> userManager,
            IPasswordCryptoService passwordCryptoService,
            ILogger<AuthenticationController> logger)
        {
            _apiKeyService = apiKeyService;
            _userProfileService = userProfileService;
            _userManager = userManager;
            _passwordCryptoService = passwordCryptoService;
            _logger = logger;
        }

        /// <summary>
        /// Generate an API key for a user (for app/web client authentication). The caller proves ownership
        /// by supplying the account email + master password; the key is bound to that user. This endpoint is
        /// exempt from the X-API-Key gate so a new client can bootstrap its key.
        /// </summary>
        [HttpPost("generate-api-key")]
        public async Task<ActionResult<ApiKeyGenerationResponse>> GenerateApiKey([FromBody] ApiKeyGenerationRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Verify the account with email + master password before issuing a key.
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null || string.IsNullOrEmpty(user.MasterPasswordHash) || string.IsNullOrEmpty(user.UserSalt) ||
                    !_passwordCryptoService.VerifyMasterPassword(
                        request.MasterPassword, user.MasterPasswordHash,
                        Convert.FromBase64String(user.UserSalt), user.MasterPasswordIterations))
                {
                    return Unauthorized("Invalid email or master password.");
                }

                // Create API key bound to the verified user.
                var apiKey = await _apiKeyService.CreateApiKeyAsync(request.Name, user.Id);

                _logger.LogInformation("API key generated for user {UserId} with name {KeyName}",
                    user.Id, request.Name);

                return Ok(new ApiKeyGenerationResponse
                {
                    ApiKey = apiKey.KeyHash, // This contains the unhashed value temporarily
                    UserId = user.Id,
                    KeyName = request.Name,
                    ExpiresAt = null, // No expiration for now
                    Instructions = "Store this API key securely. You won't be able to see it again. " +
                                  "Include it in the 'X-API-Key' header when making requests to the API."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating API key");
                return StatusCode(500, "An error occurred while generating the API key");
            }
        }

        /// <summary>
        /// Validate an API key and return user information
        /// </summary>
        [HttpPost("validate-api-key")]
        public async Task<ActionResult<ApiKeyValidationResponse>> ValidateApiKey([FromBody] ApiKeyValidationRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.ApiKey))
                    return BadRequest("API key is required");

                var validApiKey = await _apiKeyService.ValidateApiKeyAsync(request.ApiKey);
                if (validApiKey == null)
                    return Unauthorized("Invalid API key");

                return Ok(new ApiKeyValidationResponse
                {
                    IsValid = true,
                    UserId = validApiKey.UserId,
                    KeyName = validApiKey.Name,
                    CreatedAt = validApiKey.CreatedAt,
                    LastUsedAt = validApiKey.LastUsedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating API key");
                return StatusCode(500, "An error occurred while validating the API key");
            }
        }
    }

    public class ApiKeyGenerationRequest
    {
        /// <summary>A friendly name for the key, e.g. "My Pixel 8".</summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = "";

        /// <summary>The account's email address.</summary>
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        /// <summary>The account's master password (verifies ownership; the key is bound to this user).</summary>
        [Required]
        public string MasterPassword { get; set; } = "";
    }

    public class ApiKeyGenerationResponse
    {
        public string ApiKey { get; set; } = "";
        public string UserId { get; set; } = "";
        public string KeyName { get; set; } = "";
        public DateTime? ExpiresAt { get; set; }
        public string Instructions { get; set; } = "";
    }

    public class ApiKeyValidationRequest
    {
        [Required]
        public string ApiKey { get; set; } = "";
    }

    public class ApiKeyValidationResponse
    {
        public bool IsValid { get; set; }
        public string UserId { get; set; } = "";
        public string KeyName { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime? LastUsedAt { get; set; }
    }
}