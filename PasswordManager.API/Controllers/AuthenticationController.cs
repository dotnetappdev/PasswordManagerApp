using Microsoft.AspNetCore.Mvc;
using PasswordManager.Services.Interfaces;
using PasswordManager.Models;
using System.ComponentModel.DataAnnotations;

namespace PasswordManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly IApiKeyService _apiKeyService;
        private readonly IUserProfileService _userProfileService;
        private readonly ILogger<AuthenticationController> _logger;

        public AuthenticationController(
            IApiKeyService apiKeyService,
            IUserProfileService userProfileService,
            ILogger<AuthenticationController> logger)
        {
            _apiKeyService = apiKeyService;
            _userProfileService = userProfileService;
            _logger = logger;
        }

        /// <summary>
        /// Generate an API key for a user (for app/web client authentication)
        /// </summary>
        [HttpPost("generate-api-key")]
        public async Task<ActionResult<ApiKeyGenerationResponse>> GenerateApiKey([FromBody] ApiKeyGenerationRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // For now, we'll create a simple user identification method
                // In production, this should be more secure (e.g., verify user email/password)
                var userId = string.IsNullOrEmpty(request.UserId) ? Guid.NewGuid().ToString() : request.UserId;
                
                // Create API key for the user
                var apiKey = await _apiKeyService.CreateApiKeyAsync(request.Name, userId);

                _logger.LogInformation("API key generated for user {UserId} with name {KeyName}", 
                    userId, request.Name);

                return Ok(new ApiKeyGenerationResponse
                {
                    ApiKey = apiKey.KeyHash, // This contains the unhashed value temporarily
                    UserId = userId,
                    KeyName = request.Name,
                    ExpiresAt = null, // No expiration for now
                    Instructions = "Store this API key securely. You won't be able to see it again. " +
                                  "Include it in the 'X-API-Key' header when making requests to the sync API."
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
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = "";

        [MaxLength(450)]
        public string? UserId { get; set; } // Optional - if not provided, a new user ID will be generated
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