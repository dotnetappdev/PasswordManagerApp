using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using PasswordManager.Services.Interfaces;
using PasswordManager.API.DTOs;
using PasswordManager.Models.DTOs;
using PasswordManager.Models;
using PasswordManager.Crypto.Interfaces;
using PasswordManager.Crypto.Services;
using ApiDtos = PasswordManager.API.DTOs;

namespace PasswordManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PasswordItemsController : ControllerBase
{
    private readonly IPasswordItemApiService _passwordItemService;
    private readonly IPasswordEncryptionService _passwordEncryptionService;
    private readonly IVaultSessionService _vaultSessionService;
    private readonly IPasswordCryptoService _passwordCryptoService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<PasswordItemsController> _logger;

    public PasswordItemsController(
        IPasswordItemApiService passwordItemService,
        IPasswordEncryptionService passwordEncryptionService,
        IVaultSessionService vaultSessionService,
        IPasswordCryptoService passwordCryptoService,
        UserManager<ApplicationUser> userManager,
        ILogger<PasswordItemsController> logger)
    {
        _passwordItemService = passwordItemService;
        _passwordEncryptionService = passwordEncryptionService;
        _vaultSessionService = vaultSessionService;
        _passwordCryptoService = passwordCryptoService;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Get all password items
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PasswordItemDto>>> GetAll()
    {
        try
        {
            var items = await _passwordItemService.GetAllAsync();
            return Ok(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all password items");
            return StatusCode(500, "An error occurred while retrieving password items");
        }
    }

    /// <summary>
    /// Get a password item by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<PasswordItemDto>> GetById(int id)
    {
        try
        {
            var item = await _passwordItemService.GetByIdAsync(id);
            if (item == null)
                return NotFound($"Password item with ID {id} not found");

            return Ok(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving password item with ID {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the password item");
        }
    }

    /// <summary>
    /// Get password items by collection ID
    /// </summary>
    [HttpGet("collection/{collectionId}")]
    public async Task<ActionResult<IEnumerable<PasswordItemDto>>> GetByCollectionId(int collectionId)
    {
        try
        {
            var items = await _passwordItemService.GetByCollectionIdAsync(collectionId);
            return Ok(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving password items for collection {CollectionId}", collectionId);
            return StatusCode(500, "An error occurred while retrieving password items");
        }
    }

    /// <summary>
    /// Get password items by category ID
    /// </summary>
    [HttpGet("category/{categoryId}")]
    public async Task<ActionResult<IEnumerable<PasswordItemDto>>> GetByCategoryId(int categoryId)
    {
        try
        {
            var items = await _passwordItemService.GetByCategoryIdAsync(categoryId);
            return Ok(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving password items for category {CategoryId}", categoryId);
            return StatusCode(500, "An error occurred while retrieving password items");
        }
    }

    /// <summary>
    /// Get password items by tag ID
    /// </summary>
    [HttpGet("tag/{tagId}")]
    public async Task<ActionResult<IEnumerable<PasswordItemDto>>> GetByTagId(int tagId)
    {
        try
        {
            var items = await _passwordItemService.GetByTagIdAsync(tagId);
            return Ok(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving password items for tag {TagId}", tagId);
            return StatusCode(500, "An error occurred while retrieving password items");
        }
    }

    /// <summary>
    /// Search password items
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<PasswordItemDto>>> Search([FromQuery] string searchTerm)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return BadRequest("Search term cannot be empty");

            var items = await _passwordItemService.SearchAsync(searchTerm);
            return Ok(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching password items with term {SearchTerm}", searchTerm);
            return StatusCode(500, "An error occurred while searching password items");
        }
    }

    /// <summary>
    /// Create a new password item
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PasswordItemDto>> Create([FromBody] CreatePasswordItemDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var item = await _passwordItemService.CreateAsync(createDto);
            return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating password item");
            return StatusCode(500, "An error occurred while creating the password item");
        }
    }

    /// <summary>
    /// Update a password item
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<PasswordItemDto>> Update(int id, [FromBody] UpdatePasswordItemDto updateDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var item = await _passwordItemService.UpdateAsync(id, updateDto);
            if (item == null)
                return NotFound($"Password item with ID {id} not found");

            return Ok(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating password item with ID {Id}", id);
            return StatusCode(500, "An error occurred while updating the password item");
        }
    }

    /// <summary>
    /// Delete a password item permanently
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            var success = await _passwordItemService.DeleteAsync(id);
            if (!success)
                return NotFound($"Password item with ID {id} not found");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting password item with ID {Id}", id);
            return StatusCode(500, "An error occurred while deleting the password item");
        }
    }

    /// <summary>
    /// Soft delete a password item
    /// </summary>
    [HttpPatch("{id}/soft-delete")]
    public async Task<ActionResult> SoftDelete(int id)
    {
        try
        {
            var success = await _passwordItemService.SoftDeleteAsync(id);
            if (!success)
                return NotFound($"Password item with ID {id} not found");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error soft deleting password item with ID {Id}", id);
            return StatusCode(500, "An error occurred while soft deleting the password item");
        }
    }

    /// <summary>
    /// Restore a soft deleted password item
    /// </summary>
    [HttpPatch("{id}/restore")]
    public async Task<ActionResult> Restore(int id)
    {
        try
        {
            var success = await _passwordItemService.RestoreAsync(id);
            if (!success)
                return NotFound($"Password item with ID {id} not found or not deleted");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring password item with ID {Id}", id);
            return StatusCode(500, "An error occurred while restoring the password item");
        }
    }

    /// <summary>
    /// Toggle favorite status of a password item
    /// </summary>
    [HttpPatch("{id}/toggle-favorite")]
    public async Task<ActionResult> ToggleFavorite(int id)
    {
        try
        {
            var success = await _passwordItemService.ToggleFavoriteAsync(id);
            if (!success)
                return NotFound($"Password item with ID {id} not found");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling favorite for password item with ID {Id}", id);
            return StatusCode(500, "An error occurred while toggling favorite status");
        }
    }

    /// <summary>
    /// Archive a password item
    /// </summary>
    [HttpPatch("{id}/archive")]
    public async Task<ActionResult> Archive(int id)
    {
        try
        {
            var success = await _passwordItemService.ArchiveAsync(id);
            if (!success)
                return NotFound($"Password item with ID {id} not found");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving password item with ID {Id}", id);
            return StatusCode(500, "An error occurred while archiving the password item");
        }
    }

    /// <summary>
    /// Unarchive a password item
    /// </summary>
    [HttpPatch("{id}/unarchive")]
    public async Task<ActionResult> Unarchive(int id)
    {
        try
        {
            var success = await _passwordItemService.UnarchiveAsync(id);
            if (!success)
                return NotFound($"Password item with ID {id} not found");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unarchiving password item with ID {Id}", id);
            return StatusCode(500, "An error occurred while unarchiving the password item");
        }
    }

    /// <summary>
    /// Decrypt and retrieve password data for a specific item using session-based vault
    /// </summary>
    [HttpPost("{id}/decrypt")]
    public async Task<ActionResult<ApiDtos.DecryptedPasswordItemDto>> DecryptPassword(int id)
    {
        try
        {
            // Get session ID from Authorization header
            var sessionId = HttpContext.Request.Headers["Authorization"]
                .FirstOrDefault()?.Replace("Bearer ", "");

            if (string.IsNullOrEmpty(sessionId))
                return Unauthorized("Session token required");

            // Get the password item from database
            var item = await _passwordItemService.GetByIdAsync(id);
            if (item == null)
                return NotFound($"Password item with ID {id} not found");

            if (item.LoginItem != null)
            {
                // Convert the stored LoginItem to our model
                var loginItem = new Models.LoginItem
                {
                    Id = item.LoginItem.Id,
                    PasswordItemId = item.LoginItem.PasswordItemId,
                    Website = item.LoginItem.Website,
                    Username = item.LoginItem.Username,
                    Email = item.LoginItem.Email,
                    PhoneNumber = item.LoginItem.PhoneNumber,
                    TwoFactorType = item.LoginItem.TwoFactorType,
                    Password = item.LoginItem.Password, // This will be the encrypted password
                    TotpSecret = item.LoginItem.TotpSecret,
                    SecurityAnswer1 = item.LoginItem.SecurityAnswer1,
                    SecurityAnswer2 = item.LoginItem.SecurityAnswer2,
                    SecurityAnswer3 = item.LoginItem.SecurityAnswer3,
                    Notes = item.LoginItem.Notes
                };

                // Decrypt the login item using session-based approach
                var decryptedLoginItem = await _passwordEncryptionService.DecryptLoginItemAsync(
                    loginItem, sessionId);

                // Return decrypted data
                return Ok(new ApiDtos.DecryptedPasswordItemDto
                {
                    Id = item.Id,
                    Title = item.Title,
                    Description = item.Description,
                    Type = item.Type,
                    CreatedAt = item.CreatedAt,
                    LastModified = item.LastModified,
                    IsFavorite = item.IsFavorite,
                    IsArchived = item.IsArchived,
                    IsDeleted = item.IsDeleted,
                    UserId = item.UserId,
                    CategoryId = item.CategoryId ?? 0,
                    CollectionId = item.CollectionId ?? 0,
                    LoginItem = new ApiDtos.DecryptedLoginItemDto
                    {
                        Id = decryptedLoginItem.Id,
                        Website = decryptedLoginItem.Website,
                        Username = decryptedLoginItem.Username,
                        Password = decryptedLoginItem.Password,
                        Email = decryptedLoginItem.Email,
                        PhoneNumber = decryptedLoginItem.PhoneNumber,
                        TotpSecret = decryptedLoginItem.TotpSecret,
                        TwoFactorType = decryptedLoginItem.TwoFactorType,
                        SecurityQuestion1 = decryptedLoginItem.SecurityQuestion1,
                        SecurityAnswer1 = decryptedLoginItem.SecurityAnswer1,
                        SecurityQuestion2 = decryptedLoginItem.SecurityQuestion2,
                        SecurityAnswer2 = decryptedLoginItem.SecurityAnswer2,
                        SecurityQuestion3 = decryptedLoginItem.SecurityQuestion3,
                        SecurityAnswer3 = decryptedLoginItem.SecurityAnswer3,
                        RecoveryEmail = decryptedLoginItem.RecoveryEmail,
                        RecoveryPhone = decryptedLoginItem.RecoveryPhone,
                        LoginUrl = decryptedLoginItem.LoginUrl,
                        SupportUrl = decryptedLoginItem.SupportUrl,
                        AdminConsoleUrl = decryptedLoginItem.AdminConsoleUrl,
                        PasswordLastChanged = decryptedLoginItem.PasswordLastChanged,
                        RequiresPasswordChange = decryptedLoginItem.RequiresPasswordChange,
                        LastUsed = decryptedLoginItem.LastUsed,
                        UsageCount = decryptedLoginItem.UsageCount,
                        CompanyName = decryptedLoginItem.CompanyName,
                        Department = decryptedLoginItem.Department,
                        JobTitle = decryptedLoginItem.JobTitle,
                        Notes = decryptedLoginItem.Notes
                    },
                    Tags = item.Tags?.Select(t => new ApiDtos.ApiTagDto 
                    { 
                        Id = t.Id, 
                        Name = t.Name, 
                        Color = t.Color 
                    }).ToList() ?? new List<ApiDtos.ApiTagDto>()
                });
            }

            return BadRequest("Password item does not contain login data");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting password for item ID {Id}", id);
            return StatusCode(500, "An error occurred while decrypting the password");
        }
    }

    /// <summary>
    /// Create a new encrypted password item using session-based vault
    /// </summary>
    [HttpPost("encrypted")]
    public async Task<ActionResult<PasswordItemDto>> CreateEncrypted([FromBody] ApiDtos.CreateEncryptedPasswordItemDto createDto)
    {
        try
        {
            // Get session ID from Authorization header
            var sessionId = HttpContext.Request.Headers["Authorization"]
                .FirstOrDefault()?.Replace("Bearer ", "");

            if (string.IsNullOrEmpty(sessionId))
                return Unauthorized("Session token required");

            // Get user ID from session
            var userId = _vaultSessionService.GetSessionUserId(sessionId);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Invalid session");

            // Get user from database to retrieve salt
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Unauthorized("User not found");

            var userSalt = Convert.FromBase64String(user.UserSalt);

            // Create the password item model
            var passwordItem = new Models.PasswordItem
            {
                Title = createDto.Title,
                Description = createDto.Description,
                Type = createDto.Type,
                IsFavorite = createDto.IsFavorite,
                IsArchived = createDto.IsArchived,
                CategoryId = createDto.CategoryId,
                CollectionId = createDto.CollectionId,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            if (createDto.LoginItem != null)
            {
                var loginItem = new Models.LoginItem
                {
                    Website = createDto.LoginItem.Website,
                    Username = createDto.LoginItem.Username,
                    Email = createDto.LoginItem.Email,
                    PhoneNumber = createDto.LoginItem.PhoneNumber,
                    TwoFactorType = createDto.LoginItem.TwoFactorType,
                    SecurityQuestion1 = createDto.LoginItem.SecurityQuestion1,
                    SecurityQuestion2 = createDto.LoginItem.SecurityQuestion2,
                    SecurityQuestion3 = createDto.LoginItem.SecurityQuestion3,
                    RecoveryEmail = createDto.LoginItem.RecoveryEmail,
                    RecoveryPhone = createDto.LoginItem.RecoveryPhone,
                    LoginUrl = createDto.LoginItem.LoginUrl,
                    SupportUrl = createDto.LoginItem.SupportUrl,
                    AdminConsoleUrl = createDto.LoginItem.AdminConsoleUrl,
                    PasswordLastChanged = createDto.LoginItem.PasswordLastChanged,
                    RequiresPasswordChange = createDto.LoginItem.RequiresPasswordChange,
                    CompanyName = createDto.LoginItem.CompanyName,
                    Department = createDto.LoginItem.Department,
                    JobTitle = createDto.LoginItem.JobTitle,
                    // Set temporary properties for encryption
                    Password = createDto.LoginItem.Password,
                    TotpSecret = createDto.LoginItem.TotpSecret,
                    SecurityAnswer1 = createDto.LoginItem.SecurityAnswer1,
                    SecurityAnswer2 = createDto.LoginItem.SecurityAnswer2,
                    SecurityAnswer3 = createDto.LoginItem.SecurityAnswer3,
                    Notes = createDto.LoginItem.Notes
                };

                // Encrypt all sensitive fields using the encryption service
                await _passwordEncryptionService.EncryptLoginItemWithMasterPasswordAsync(loginItem, createDto.MasterPassword, userSalt);

                // Convert to DTO format for the existing service
                // Use mapping helper to convert LoginItem to CreateLoginItemDto if needed
                var loginItemDto = new CreateLoginItemDto
                {
                    Website = loginItem.Website,
                    Username = loginItem.Username,
                    Email = loginItem.Email,
                    Password = loginItem.Password,
                    PhoneNumber = loginItem.PhoneNumber,
                    TotpSecret = loginItem.TotpSecret,
                    TwoFactorType = loginItem.TwoFactorType,
                    SecurityQuestion1 = loginItem.SecurityQuestion1,
                    SecurityAnswer1 = loginItem.SecurityAnswer1,
                    SecurityQuestion2 = loginItem.SecurityQuestion2,
                    SecurityAnswer2 = loginItem.SecurityAnswer2,
                    SecurityQuestion3 = loginItem.SecurityQuestion3,
                    SecurityAnswer3 = loginItem.SecurityAnswer3,
                    RecoveryEmail = loginItem.RecoveryEmail,
                    RecoveryPhone = loginItem.RecoveryPhone,
                    LoginUrl = loginItem.LoginUrl,
                    SupportUrl = loginItem.SupportUrl,
                    AdminConsoleUrl = loginItem.AdminConsoleUrl,
                    PasswordLastChanged = loginItem.PasswordLastChanged,
                    RequiresPasswordChange = loginItem.RequiresPasswordChange,
                    CompanyName = loginItem.CompanyName,
                    Department = loginItem.Department,
                    JobTitle = loginItem.JobTitle,
                    Notes = loginItem.Notes
                };
                passwordItem.LoginItem = loginItemDto;
            }

            // Convert to DTO format for the service
            var itemDto = new CreatePasswordItemDto
            {
                Title = passwordItem.Title,
                Description = passwordItem.Description,
                Type = passwordItem.Type,
                IsFavorite = passwordItem.IsFavorite,
                IsArchived = passwordItem.IsArchived,
                CategoryId = passwordItem.CategoryId,
                CollectionId = passwordItem.CollectionId,
                LoginItem = createDto.LoginItem, // Use the original DTO
                TagIds = createDto.TagIds
            };

            var createdItem = await _passwordItemService.CreateAsync(itemDto);
            return CreatedAtAction(nameof(GetById), new { id = createdItem.Id }, createdItem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating encrypted password item");
            return StatusCode(500, "An error occurred while creating the password item");
        }
    }

    /// <summary>
    /// Reveal password for a specific item using master password (Bitwarden-style)
    /// </summary>
    [HttpPost("{id}/reveal")]
    public async Task<ActionResult<ApiDtos.RevealPasswordResponseDto>> RevealPassword(int id, [FromBody] ApiDtos.RevealPasswordRequestDto request)
    {
        try
        {
            // Get session ID from Authorization header
            var sessionId = HttpContext.Request.Headers["Authorization"]
                .FirstOrDefault()?.Replace("Bearer ", "");

            if (string.IsNullOrEmpty(sessionId))
                return Unauthorized("Session token required");

            // Get user ID from session
            var userId = _vaultSessionService.GetSessionUserId(sessionId);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Invalid session");

            // Get user from database
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Unauthorized("User not found");

            // Verify master password using PBKDF2 with 600,000 iterations
            if (!_passwordCryptoService.VerifyMasterPassword(
                request.MasterPassword, 
                user.MasterPasswordHash, 
                Convert.FromBase64String(user.UserSalt), 
                user.MasterPasswordIterations))
            {
                return Unauthorized("Invalid master password");
            }

            // Get the password item from database
            var item = await _passwordItemService.GetByIdAsync(id);
            if (item == null)
                return NotFound($"Password item with ID {id} not found");

            // Verify the item belongs to the user
            if (item.UserId != userId)
                return Forbid("Access denied");

            if (item.LoginItem != null && !string.IsNullOrEmpty(item.LoginItem.EncryptedPassword))
            {
                // Decrypt the password using master password and user salt
                var encryptedPasswordData = new EncryptedPasswordData
                {
                    EncryptedPassword = item.LoginItem.EncryptedPassword,
                    Nonce = item.LoginItem.PasswordNonce,
                    AuthenticationTag = item.LoginItem.PasswordAuthTag
                };

                var decryptedPassword = _passwordCryptoService.DecryptPassword(
                    encryptedPasswordData, 
                    request.MasterPassword, 
                    Convert.FromBase64String(user.UserSalt));

                return Ok(new ApiDtos.RevealPasswordResponseDto
                {
                    Password = decryptedPassword,
                    ItemId = id,
                    RevealedAt = DateTime.UtcNow
                });
            }

            return BadRequest("Password item does not contain encrypted password data");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revealing password for item ID {Id}", id);
            return StatusCode(500, "An error occurred while revealing the password");
        }
    }
}
