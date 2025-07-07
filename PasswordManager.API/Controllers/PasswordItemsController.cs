using Microsoft.AspNetCore.Mvc;
using PasswordManager.API.Interfaces;
using PasswordManager.API.DTOs;
using PasswordManager.Models.DTOs;
using ApiDtos = PasswordManager.API.DTOs;

namespace PasswordManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PasswordItemsController : ControllerBase
{
    private readonly IPasswordItemApiService _passwordItemService;
    private readonly IPasswordEncryptionService _passwordEncryptionService;
    private readonly ILogger<PasswordItemsController> _logger;

    public PasswordItemsController(
        IPasswordItemApiService passwordItemService,
        IPasswordEncryptionService passwordEncryptionService,
        ILogger<PasswordItemsController> logger)
    {
        _passwordItemService = passwordItemService;
        _passwordEncryptionService = passwordEncryptionService;
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
    /// Decrypt and retrieve password data for a specific item
    /// </summary>
    [HttpPost("{id}/decrypt")]
    public async Task<ActionResult<ApiDtos.DecryptedPasswordItemDto>> DecryptPassword(int id, [FromBody] ApiDtos.DecryptPasswordRequestDto request)
    {
        try
        {
            if (request.PasswordItemId != id)
                return BadRequest("Password item ID in URL does not match request body");

            if (string.IsNullOrEmpty(request.MasterPassword))
                return BadRequest("Master password is required");

            // Get the password item from database
            var item = await _passwordItemService.GetByIdAsync(id);
            if (item == null)
                return NotFound($"Password item with ID {id} not found");

            // TODO: Get user salt from current user (requires authentication)
            // For now, this is a placeholder - you'll need to implement user authentication
            // and retrieve the user's salt from the database
            var userSalt = new byte[32]; // This should come from the authenticated user
            
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
                    // Add encrypted fields here when they exist in your DTO
                };

                // Decrypt the login item
                var decryptedLoginItem = await _passwordEncryptionService.DecryptLoginItemAsync(
                    loginItem, request.MasterPassword, userSalt);

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
                    CategoryId = item.CategoryId,
                    CollectionId = item.CollectionId,
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
    /// Create a new encrypted password item
    /// </summary>
    [HttpPost("encrypted")]
    public async Task<ActionResult<PasswordItemDto>> CreateEncrypted([FromBody] ApiDtos.CreateEncryptedPasswordItemDto createDto)
    {
        try
        {
            if (string.IsNullOrEmpty(createDto.MasterPassword))
                return BadRequest("Master password is required for encryption");

            // TODO: Get user salt from authenticated user
            var userSalt = new byte[32]; // This should come from the authenticated user

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
                    JobTitle = createDto.LoginItem.JobTitle
                };

                // Set plaintext fields that will be encrypted (temporarily)
                // These will be cleared after encryption
                var tempPassword = createDto.LoginItem.Password;
                var tempTotpSecret = createDto.LoginItem.TotpSecret;
                var tempSecurityAnswer1 = createDto.LoginItem.SecurityAnswer1;
                var tempSecurityAnswer2 = createDto.LoginItem.SecurityAnswer2;
                var tempSecurityAnswer3 = createDto.LoginItem.SecurityAnswer3;
                var tempNotes = createDto.LoginItem.Notes;

                // Encrypt sensitive fields
                if (!string.IsNullOrEmpty(tempPassword))
                {
                    var encryptedPassword = await _passwordEncryptionService.EncryptFieldAsync(tempPassword, createDto.MasterPassword, userSalt);
                    loginItem.EncryptedPassword = encryptedPassword.EncryptedPassword;
                    loginItem.PasswordNonce = encryptedPassword.Nonce;
                    loginItem.PasswordAuthTag = encryptedPassword.AuthenticationTag;
                }

                if (!string.IsNullOrEmpty(tempTotpSecret))
                {
                    var encryptedTotp = await _passwordEncryptionService.EncryptFieldAsync(tempTotpSecret, createDto.MasterPassword, userSalt);
                    loginItem.EncryptedTotpSecret = encryptedTotp.EncryptedPassword;
                    loginItem.TotpNonce = encryptedTotp.Nonce;
                    loginItem.TotpAuthTag = encryptedTotp.AuthenticationTag;
                }

                if (!string.IsNullOrEmpty(tempSecurityAnswer1))
                {
                    var encrypted = await _passwordEncryptionService.EncryptFieldAsync(tempSecurityAnswer1, createDto.MasterPassword, userSalt);
                    loginItem.EncryptedSecurityAnswer1 = encrypted.EncryptedPassword;
                    loginItem.SecurityAnswer1Nonce = encrypted.Nonce;
                    loginItem.SecurityAnswer1AuthTag = encrypted.AuthenticationTag;
                }

                if (!string.IsNullOrEmpty(tempSecurityAnswer2))
                {
                    var encrypted = await _passwordEncryptionService.EncryptFieldAsync(tempSecurityAnswer2, createDto.MasterPassword, userSalt);
                    loginItem.EncryptedSecurityAnswer2 = encrypted.EncryptedPassword;
                    loginItem.SecurityAnswer2Nonce = encrypted.Nonce;
                    loginItem.SecurityAnswer2AuthTag = encrypted.AuthenticationTag;
                }

                if (!string.IsNullOrEmpty(tempSecurityAnswer3))
                {
                    var encrypted = await _passwordEncryptionService.EncryptFieldAsync(tempSecurityAnswer3, createDto.MasterPassword, userSalt);
                    loginItem.EncryptedSecurityAnswer3 = encrypted.EncryptedPassword;
                    loginItem.SecurityAnswer3Nonce = encrypted.Nonce;
                    loginItem.SecurityAnswer3AuthTag = encrypted.AuthenticationTag;
                }

                if (!string.IsNullOrEmpty(tempNotes))
                {
                    var encryptedNotes = await _passwordEncryptionService.EncryptFieldAsync(tempNotes, createDto.MasterPassword, userSalt);
                    loginItem.EncryptedNotes = encryptedNotes.EncryptedPassword;
                    loginItem.NotesNonce = encryptedNotes.Nonce;
                    loginItem.NotesAuthTag = encryptedNotes.AuthenticationTag;
                }

                // Convert to DTO format for the existing service
                var loginItemDto = new CreateLoginItemDto
                {
                    Website = loginItem.Website,
                    Username = loginItem.Username,
                    Email = loginItem.Email,
                    PhoneNumber = loginItem.PhoneNumber,
                    TwoFactorType = loginItem.TwoFactorType,
                    SecurityQuestion1 = loginItem.SecurityQuestion1,
                    SecurityQuestion2 = loginItem.SecurityQuestion2,
                    SecurityQuestion3 = loginItem.SecurityQuestion3,
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
                    // Note: Don't pass the plain text passwords to the DTO
                    // The encrypted versions will be handled separately
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
                LoginItem = passwordItem.LoginItem,
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
}
