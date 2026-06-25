using Microsoft.AspNetCore.Mvc;
using PasswordManager.Services.Interfaces;
using PasswordManager.Models;
using System.Security.Claims;

namespace PasswordManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApiKeysController : ControllerBase
    {
        private readonly IApiKeyService _apiKeyService;
        
        public ApiKeysController(IApiKeyService apiKeyService)
        {
            _apiKeyService = apiKeyService;
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

    public class ApiKeyResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string KeyValue { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}
