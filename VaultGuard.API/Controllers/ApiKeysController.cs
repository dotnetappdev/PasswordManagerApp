using Microsoft.AspNetCore.Mvc;
using VaultGuard.Services.Interfaces;
using VaultGuard.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using VaultGuard.Crypto.Interfaces;

namespace VaultGuard.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApiKeysController : ControllerBase
    {
        private readonly IApiKeyService _apiKeyService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPasswordCryptoService _passwordCryptoService;
        private readonly ILogger<ApiKeysController> _logger;

        public ApiKeysController(
            IApiKeyService apiKeyService,
            UserManager<ApplicationUser> userManager,
            IPasswordCryptoService passwordCryptoService,
            ILogger<ApiKeysController> logger)
        {
            _apiKeyService = apiKeyService;
            _userManager = userManager;
            _passwordCryptoService = passwordCryptoService;
            _logger = logger;
        }

        /// <summary>
        /// Issue a REAL API key that authenticates against THIS API server, using just the account's
        /// email + master password. This is the endpoint the desktop/web/mobile "Generate key from server"
        /// / "Test connection" flows call: the key is created in this API's own database (so it actually
        /// works for X-API-Key calls here), and the plaintext is returned exactly once. A key generated
        /// locally in a client app is stored in that client's local database and will NOT authenticate here,
        /// which is why testing it returns 401/403.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("issue")]
        public async Task<ActionResult<ApiKeyResponse>> IssueApiKey([FromBody] IssueApiKeyRequest request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.MasterPassword))
                return BadRequest("Email and master password are required.");

            // Validate credentials the same way login does — find the user, verify the master password
            // against the stored hash/salt/iterations. Never reveal which of the two was wrong.
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user is null || string.IsNullOrEmpty(user.UserSalt))
            {
                _logger.LogWarning("API key issue rejected: unknown account for {Email}", request.Email);
                return Unauthorized("Invalid email or master password.");
            }

            bool valid;
            try
            {
                valid = _passwordCryptoService.VerifyMasterPassword(
                    request.MasterPassword,
                    user.MasterPasswordHash,
                    Convert.FromBase64String(user.UserSalt),
                    user.MasterPasswordIterations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API key issue: master password verification error for {Email}", request.Email);
                return Unauthorized("Invalid email or master password.");
            }

            if (!valid)
            {
                _logger.LogWarning("API key issue rejected: bad master password for {Email}", request.Email);
                return Unauthorized("Invalid email or master password.");
            }

            var name = string.IsNullOrWhiteSpace(request.Name) ? $"Test connection ({DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC)" : request.Name;
            var apiKey = await _apiKeyService.CreateApiKeyAsync(name, user.Id);

            return Ok(new ApiKeyResponse
            {
                Id = apiKey.Id,
                Name = apiKey.Name,
                KeyValue = apiKey.KeyHash, // plaintext, returned once
                CreatedAt = apiKey.CreatedAt
            });
        }

        [HttpGet]
        public async Task<ActionResult<List<ApiKey>>> GetUserApiKeys()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var apiKeys = await _apiKeyService.GetUserApiKeysAsync(userId);
            
            // Remove sensitive data before returning
            foreach (var key in apiKeys)
            {
                key.KeyHash = "***"; // Hide the actual hash
            }
            
            return Ok(apiKeys);
        }

        [HttpPost]
        public async Task<ActionResult<ApiKeyResponse>> CreateApiKey([FromBody] CreateApiKeyRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (string.IsNullOrEmpty(request.Name))
                return BadRequest("API key name is required");

            var apiKey = await _apiKeyService.CreateApiKeyAsync(request.Name, userId);
            
            return Ok(new ApiKeyResponse
            {
                Id = apiKey.Id,
                Name = apiKey.Name,
                KeyValue = apiKey.KeyHash, // This contains the unhashed value temporarily
                CreatedAt = apiKey.CreatedAt
            });
        }

        [HttpDelete("{keyId}")]
        public async Task<ActionResult> DeleteApiKey(Guid keyId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _apiKeyService.DeleteApiKeyAsync(keyId, userId);
            
            if (!result)
                return NotFound();

            return Ok();
        }
    }

    public class CreateApiKeyRequest
    {
        public string Name { get; set; } = "";
    }

    public class IssueApiKeyRequest
    {
        public string Email { get; set; } = "";
        public string MasterPassword { get; set; } = "";
        public string Name { get; set; } = "";
    }

    public class ApiKeyResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string KeyValue { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}
