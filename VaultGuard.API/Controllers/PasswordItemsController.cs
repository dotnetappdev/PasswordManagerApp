using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using VaultGuard.Services.Interfaces;
using VaultGuard.API.DTOs;
using VaultGuard.Models.DTOs;
using VaultGuard.Models;
using VaultGuard.Models.Authorization;
using VaultGuard.Crypto.Interfaces;
using VaultGuard.Crypto.Services;
using System.Security.Claims;
using ApiDtos = VaultGuard.API.DTOs;

namespace VaultGuard.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PasswordItemsController : ControllerBase
{
    private readonly IPasswordItemApiService _passwordItemService;
    private readonly IPasswordEncryptionService _passwordEncryptionService;
    private readonly IVaultSessionService _vaultSessionService;
    private readonly IPasswordCryptoService _passwordCryptoService;
    private readonly IPermissionService _permissionService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<PasswordItemsController> _logger;

    public PasswordItemsController(
        IPasswordItemApiService passwordItemService,
        IPasswordEncryptionService passwordEncryptionService,
        IVaultSessionService vaultSessionService,
        IPasswordCryptoService passwordCryptoService,
        IPermissionService permissionService,
        UserManager<ApplicationUser> userManager,
        ILogger<PasswordItemsController> logger)
    {
        _passwordItemService = passwordItemService;
        _passwordEncryptionService = passwordEncryptionService;
        _vaultSessionService = vaultSessionService;
        _passwordCryptoService = passwordCryptoService;
        _permissionService = permissionService;
        _userManager = userManager;
        _logger = logger;
    }

    // Resolves the calling user from the identity/API-key claims.
    private string? GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    /// <summary>
    /// Loads a password item and confirms the caller is allowed to act on it. Returns 404 (not 403)
    /// on a missing item OR an ownership/permission mismatch, so we never disclose that another
    /// user's item id exists. This is the single guard used by every id-scoped endpoint to prevent
    /// IDOR (one user reading/mutating another user's items by guessing integer ids).
    /// </summary>
    private async Task<(PasswordItemDto? Item, ActionResult? Error)> LoadOwnedItemAsync(int id, string permission)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return (null, Unauthorized());

        var item = await _passwordItemService.GetByIdAsync(id);
        if (item == null || !await _permissionService.CanAccessResourceAsync(userId, item.UserId, permission))
            return (null, NotFound($"Password item with ID {id} not found"));

        return (item, null);
    }

    /// <summary>
    /// Get all password items (with permission checks)
    /// </summary>
    [HttpGet]
    [ChildSafeOperation]
    public async Task<ActionResult<IEnumerable<PasswordItemDto>>> GetAll()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Check if user has permission to view passwords
            if (!await _permissionService.HasPermissionAsync(userId, Permissions.Passwords.View))
            {
                return StatusCode(StatusCodes.Status403Forbidden, "You don't have permission to view passwords");
            }

            // For child users, check additional restrictions
            if (await _permissionService.IsInRoleAsync(userId, ApplicationRoles.Child))
            {
                if (!await _permissionService.ChildCanPerformPasswordOperationAsync(userId, "view"))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, "Child access is currently restricted");
                }
            }

            var items = await _passwordItemService.GetAllAsync();
            return Ok(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all password items for user {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
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
            var (item, error) = await LoadOwnedItemAsync(id, Permissions.Passwords.View);
            if (error != null)
                return error;

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
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // Scope to the caller's own items so one user can't enumerate another's via a shared id.
            var items = await _passwordItemService.GetByCollectionIdAsync(collectionId);
            return Ok(items.Where(i => i.UserId == userId));
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
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var items = await _passwordItemService.GetByCategoryIdAsync(categoryId);
            return Ok(items.Where(i => i.UserId == userId));
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
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var items = await _passwordItemService.GetByTagIdAsync(tagId);
            return Ok(items.Where(i => i.UserId == userId));
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

            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var items = await _passwordItemService.SearchAsync(searchTerm);
            return Ok(items.Where(i => i.UserId == userId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching password items with term {SearchTerm}", searchTerm);
            return StatusCode(500, "An error occurred while searching password items");
        }
    }

    /// <summary>
    /// Create a new password item (restricted for children)
    /// </summary>
    [HttpPost]
    [RestrictedFromChildren]
    public async Task<ActionResult<PasswordItemDto>> Create([FromBody] CreatePasswordItemDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Check if user has permission to create passwords
            if (!await _permissionService.HasPermissionAsync(userId, Permissions.Passwords.Create))
            {
                return StatusCode(StatusCodes.Status403Forbidden, "You don't have permission to create passwords");
            }

            // Additional check for child users
            if (await _permissionService.IsInRoleAsync(userId, ApplicationRoles.Child))
            {
                if (!await _permissionService.ChildCanPerformPasswordOperationAsync(userId, "create"))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, "Children are not allowed to create passwords");
                }
            }

            var item = await _passwordItemService.CreateAsync(createDto, userId);
            return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
        }
        catch (VaultGuard.Services.Security.VaultKeyRequiredException e)
        {
            return BadRequest(e.Message); // secrets are never stored without being encrypted
        }
        catch (UnauthorizedAccessException)
        {
            return NotFound(); // don't disclose other users' items
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating password item for user {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            return StatusCode(500, "An error occurred while creating the password item");
        }
    }

    /// <summary>
    /// Update a password item (restricted for children)
    /// </summary>
    [HttpPut("{id}")]
    [RestrictedFromChildren]
    public async Task<ActionResult<PasswordItemDto>> Update(int id, [FromBody] UpdatePasswordItemDto updateDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Get the item to check ownership
            var existingItem = await _passwordItemService.GetByIdAsync(id);
            if (existingItem == null)
                return NotFound($"Password item with ID {id} not found");

            // Check permissions
            if (!await _permissionService.CanAccessResourceAsync(userId, existingItem.UserId, Permissions.Passwords.Edit))
            {
                return StatusCode(StatusCodes.Status403Forbidden, "You don't have permission to edit this password");
            }

            // Additional check for child users
            if (await _permissionService.IsInRoleAsync(userId, ApplicationRoles.Child))
            {
                if (!await _permissionService.ChildCanPerformPasswordOperationAsync(userId, "edit"))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, "Children are not allowed to edit passwords");
                }
            }

            var item = await _passwordItemService.UpdateAsync(id, updateDto);
            if (item == null)
                return NotFound($"Password item with ID {id} not found");

            return Ok(item);
        }
        catch (VaultGuard.Services.Security.VaultKeyRequiredException e)
        {
            return BadRequest(e.Message); // secrets are never stored without being encrypted
        }
        catch (UnauthorizedAccessException)
        {
            return NotFound(); // don't disclose other users' items
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating password item with ID {Id} for user {UserId}", id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
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
            var (_, error) = await LoadOwnedItemAsync(id, Permissions.Passwords.Delete);
            if (error != null)
                return error;

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
            var (_, error) = await LoadOwnedItemAsync(id, Permissions.Passwords.Delete);
            if (error != null)
                return error;

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
            var (_, error) = await LoadOwnedItemAsync(id, Permissions.Passwords.Delete);
            if (error != null)
                return error;

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
            var (_, error) = await LoadOwnedItemAsync(id, Permissions.Passwords.Edit);
            if (error != null)
                return error;

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
            var (_, error) = await LoadOwnedItemAsync(id, Permissions.Passwords.Edit);
            if (error != null)
                return error;

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
            var (_, error) = await LoadOwnedItemAsync(id, Permissions.Passwords.Edit);
            if (error != null)
                return error;

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
        // Decrypts one of the caller's own items with their unlocked vault session (from login/masterkey).
        // The session id comes from "X-Vault-Session" (or, for older clients, "Authorization: Bearer").
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        if (!await _permissionService.HasPermissionAsync(userId, Permissions.Passwords.ViewSensitive)
            || await _permissionService.IsInRoleAsync(userId, ApplicationRoles.Child))
            return StatusCode(StatusCodes.Status403Forbidden, "You don't have permission to view password details");

        var sessionId = VaultSessionIdFromRequest();
        // A session only works for the user it belongs to.
        if (string.IsNullOrEmpty(sessionId) || !_vaultSessionService.IsVaultUnlocked(sessionId)
            || _vaultSessionService.GetSessionUserId(sessionId) != userId)
            return Unauthorized("An unlocked vault session for this user is required");

        // RevealSecretsAsync only finds the caller's own items (owner-scoped), so it is the ownership check.
        var cipher = VaultGuard.Services.Security.VaultCipher.ForSession(_vaultSessionService, sessionId)!;
        var secrets = await _passwordItemService.RevealSecretsAsync(id, cipher);
        var item = secrets == null ? null : await _passwordItemService.GetByIdAsync(id);
        if (secrets == null || item == null) return NotFound($"Password item with ID {id} not found");
        string? S(string key) => secrets.TryGetValue(key, out var v) ? v : null;

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
            LoginItem = item.LoginItem == null ? null : new ApiDtos.DecryptedLoginItemDto
            {
                Id = item.LoginItem.Id,
                Website = item.LoginItem.Website,
                Username = item.LoginItem.Username,
                Email = item.LoginItem.Email,
                PhoneNumber = item.LoginItem.PhoneNumber,
                TwoFactorType = item.LoginItem.TwoFactorType,
                Password = S("Login.Password"),
                TotpSecret = S("Login.TotpSecret") ?? S("Custom.TOTP Secret"),
                SecurityAnswer1 = S("Login.SecurityAnswer1"),
                SecurityAnswer2 = S("Login.SecurityAnswer2"),
                SecurityAnswer3 = S("Login.SecurityAnswer3"),
                Notes = S("Login.Notes")
            }
        });
    }

    private string? VaultSessionIdFromRequest()
    {
        var header = HttpContext.Request.Headers["X-Vault-Session"].FirstOrDefault();
        if (!string.IsNullOrEmpty(header)) return header;
        var auth = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
        return auth != null && auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? auth.Substring(7).Trim() : null;
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

            // Declare itemDto variable
            CreatePasswordItemDto itemDto;

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
                
                // Convert to DTO format for the service
                itemDto = new CreatePasswordItemDto
                {
                    Title = passwordItem.Title,
                    Description = passwordItem.Description,
                    Type = passwordItem.Type,
                    IsFavorite = passwordItem.IsFavorite,
                    IsArchived = passwordItem.IsArchived,
                    CategoryId = passwordItem.CategoryId,
                    CollectionId = passwordItem.CollectionId,
                    LoginItem = loginItemDto, // Use the converted DTO
                    TagIds = createDto.TagIds
                };
            }
            else
            {
                // Convert to DTO format for the service when no LoginItem
                itemDto = new CreatePasswordItemDto
                {
                    Title = passwordItem.Title,
                    Description = passwordItem.Description,
                    Type = passwordItem.Type,
                    IsFavorite = passwordItem.IsFavorite,
                    IsArchived = passwordItem.IsArchived,
                    CategoryId = passwordItem.CategoryId,
                    CollectionId = passwordItem.CollectionId,
                    TagIds = createDto.TagIds
                };
            }

            var createdItem = await _passwordItemService.CreateAsync(itemDto, userId);
            return CreatedAtAction(nameof(GetById), new { id = createdItem.Id }, createdItem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating encrypted password item");
            return StatusCode(500, "An error occurred while creating the password item");
        }
    }

    /// <summary>
    /// Reveal password for a specific item using master password (restricted for children)
    /// </summary>
    [HttpPost("{id}/reveal")]
    [RestrictedFromChildren]
    public async Task<ActionResult<ApiDtos.RevealPasswordResponseDto>> RevealPassword(int id, [FromBody] ApiDtos.RevealPasswordRequestDto request)
    {
        // Reveals the login password of one of the caller's own items, using their master password.
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        if (!await _permissionService.HasPermissionAsync(userId, Permissions.Passwords.ViewSensitive))
            return StatusCode(StatusCodes.Status403Forbidden, "You don't have permission to reveal passwords");
        if (await _permissionService.IsInRoleAsync(userId, ApplicationRoles.Child))
            return StatusCode(StatusCodes.Status403Forbidden, "Children are not allowed to reveal password details");

        try
        {
            var secrets = await _passwordItemService.RevealSecretsAsync(id, request.MasterPassword);
            if (secrets == null) return NotFound($"Password item with ID {id} not found");
            if (!secrets.TryGetValue("Login.Password", out var password))
                return BadRequest("Password item does not contain a password");
            return Ok(new ApiDtos.RevealPasswordResponseDto { Password = password, ItemId = id, RevealedAt = DateTime.UtcNow });
        }
        catch (VaultGuard.Services.Security.VaultKeyRequiredException)
        {
            return Unauthorized("Invalid master password");
        }
    }

    /// <summary>
    /// Decrypt all secrets of one of the caller's items (login password, TOTP secret, card number/CVV/PIN,
    /// Wi-Fi passwords, note content, passkey id…) using their master password. The server derives the vault
    /// key for this request only and never stores the master password or the plaintext.
    /// POST /api/passworditems/{id}/secrets   { "masterPassword": "…" }
    /// </summary>
    [HttpPost("{id}/secrets")]
    [RestrictedFromChildren]
    public async Task<ActionResult<IReadOnlyDictionary<string, string>>> RevealSecrets(int id, [FromBody] ApiDtos.RevealPasswordRequestDto request)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        if (!await _permissionService.HasPermissionAsync(userId, Permissions.Passwords.ViewSensitive))
            return Forbid();
        try
        {
            var secrets = await _passwordItemService.RevealSecretsAsync(id, request.MasterPassword);
            return secrets == null ? NotFound() : Ok(secrets);
        }
        catch (VaultGuard.Services.Security.VaultKeyRequiredException)
        {
            return Unauthorized("Invalid master password");
        }
    }
}
