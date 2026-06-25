using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PasswordManager.Models;
using PasswordManager.Models.Authorization;
using PasswordManager.Services.Interfaces;
using System.Security.Claims;

namespace PasswordManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChildManagementController : ControllerBase
{
    private readonly IPermissionService _permissionService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ChildManagementController> _logger;

    public ChildManagementController(
        IPermissionService permissionService,
        UserManager<ApplicationUser> userManager,
        ILogger<ChildManagementController> logger)
    {
        _permissionService = permissionService;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Get all children for the current parent user
    /// </summary>
    [HttpGet("children")]
    [RequireParentOrAdmin]
    public async Task<ActionResult<IEnumerable<ChildUserDto>>> GetChildren()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var children = await _permissionService.GetChildrenAsync(userId);
            var childDtos = children.Select(child => new ChildUserDto
            {
                Id = child.Id,
                Email = child.Email,
                FirstName = child.FirstName,
                LastName = child.LastName,
                CreatedAt = child.CreatedAt,
                LastLoginAt = child.LastLoginAt,
                IsActive = child.IsActive
            });

            return Ok(childDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving children for user {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            return StatusCode(500, "An error occurred while retrieving children");
        }
    }

    /// <summary>
    /// Get permission configuration for a specific child
    /// </summary>
    [HttpGet("children/{childUserId}/permissions")]
    [RequireParentOrAdmin]
    public async Task<ActionResult<ChildPermissionConfigDto>> GetChildPermissions(string childUserId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Verify the user is a parent of this child or is an admin
            if (!await _permissionService.IsInRoleAsync(userId, ApplicationRoles.Admin) &&
                !await _permissionService.IsParentOfAsync(userId, childUserId))
            {
                return Forbid("You are not the parent of this child");
            }

            var config = await _permissionService.GetChildPermissionConfigAsync(childUserId, userId);
            if (config == null)
            {
                config = _permissionService.GetDefaultChildPermissionConfig();
            }

            var dto = new ChildPermissionConfigDto
            {
                ChildUserId = config.ChildUserId,
                ParentUserId = config.ParentUserId,
                CanViewPasswords = config.CanViewPasswords,
                CanCreatePasswords = config.CanCreatePasswords,
                CanEditPasswords = config.CanEditPasswords,
                CanDeletePasswords = config.CanDeletePasswords,
                CanRevealPasswords = config.CanRevealPasswords,
                CanSharePasswords = config.CanSharePasswords,
                CanExportData = config.CanExportData,
                CanCreateCollections = config.CanCreateCollections,
                CanManageOwnCollections = config.CanManageOwnCollections,
                MaxPasswordItems = config.MaxPasswordItems,
                AccessStartTime = config.AccessStartTime,
                AccessEndTime = config.AccessEndTime,
                AllowedDaysOfWeek = config.AllowedDaysOfWeek,
                RequireParentApproval = config.RequireParentApproval,
                LogActivities = config.LogActivities
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving permissions for child {ChildUserId} by user {UserId}", childUserId, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            return StatusCode(500, "An error occurred while retrieving child permissions");
        }
    }

    /// <summary>
    /// Update permission configuration for a specific child
    /// </summary>
    [HttpPut("children/{childUserId}/permissions")]
    [RequireParentOrAdmin]
    public async Task<ActionResult<ChildPermissionConfigDto>> UpdateChildPermissions(string childUserId, [FromBody] UpdateChildPermissionConfigDto updateDto)
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

            // Verify the user is a parent of this child or is an admin
            if (!await _permissionService.IsInRoleAsync(userId, ApplicationRoles.Admin) &&
                !await _permissionService.IsParentOfAsync(userId, childUserId))
            {
                return Forbid("You are not the parent of this child");
            }

            var config = new ChildPermissionConfig
            {
                ChildUserId = childUserId,
                ParentUserId = userId,
                CanViewPasswords = updateDto.CanViewPasswords,
                CanCreatePasswords = updateDto.CanCreatePasswords,
                CanEditPasswords = updateDto.CanEditPasswords,
                CanDeletePasswords = updateDto.CanDeletePasswords,
                CanRevealPasswords = updateDto.CanRevealPasswords,
                CanSharePasswords = updateDto.CanSharePasswords,
                CanExportData = updateDto.CanExportData,
                CanCreateCollections = updateDto.CanCreateCollections,
                CanManageOwnCollections = updateDto.CanManageOwnCollections,
                MaxPasswordItems = updateDto.MaxPasswordItems,
                AccessStartTime = updateDto.AccessStartTime,
                AccessEndTime = updateDto.AccessEndTime,
                AllowedDaysOfWeek = updateDto.AllowedDaysOfWeek,
                RequireParentApproval = updateDto.RequireParentApproval,
                LogActivities = updateDto.LogActivities
            };

            var updatedConfig = await _permissionService.SetChildPermissionConfigAsync(childUserId, userId, config);

            var dto = new ChildPermissionConfigDto
            {
                ChildUserId = updatedConfig.ChildUserId,
                ParentUserId = updatedConfig.ParentUserId,
                CanViewPasswords = updatedConfig.CanViewPasswords,
                CanCreatePasswords = updatedConfig.CanCreatePasswords,
                CanEditPasswords = updatedConfig.CanEditPasswords,
                CanDeletePasswords = updatedConfig.CanDeletePasswords,
                CanRevealPasswords = updatedConfig.CanRevealPasswords,
                CanSharePasswords = updatedConfig.CanSharePasswords,
                CanExportData = updatedConfig.CanExportData,
                CanCreateCollections = updatedConfig.CanCreateCollections,
                CanManageOwnCollections = updatedConfig.CanManageOwnCollections,
                MaxPasswordItems = updatedConfig.MaxPasswordItems,
                AccessStartTime = updatedConfig.AccessStartTime,
                AccessEndTime = updatedConfig.AccessEndTime,
                AllowedDaysOfWeek = updatedConfig.AllowedDaysOfWeek,
                RequireParentApproval = updatedConfig.RequireParentApproval,
                LogActivities = updatedConfig.LogActivities
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating permissions for child {ChildUserId} by user {UserId}", childUserId, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            return StatusCode(500, "An error occurred while updating child permissions");
        }
    }

    /// <summary>
    /// Create a parent-child relationship
    /// </summary>
    [HttpPost("children/{childUserId}/relationship")]
    [RequireParentOrAdmin]
    public async Task<ActionResult<UserRelationshipDto>> CreateChildRelationship(string childUserId, [FromBody] CreateChildRelationshipDto createDto)
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

            // Verify the child user exists and has Child role
            var childUser = await _userManager.FindByIdAsync(childUserId);
            if (childUser == null)
            {
                return NotFound("Child user not found");
            }

            if (!await _userManager.IsInRoleAsync(childUser, ApplicationRoles.Child))
            {
                return BadRequest("Target user must have Child role");
            }

            // Create the relationship
            var relationship = await _permissionService.CreateParentChildRelationshipAsync(
                userId, childUserId, userId, createDto.Notes);

            var dto = new UserRelationshipDto
            {
                Id = relationship.Id,
                ParentUserId = relationship.ParentUserId,
                ChildUserId = relationship.ChildUserId,
                RelationshipType = relationship.RelationshipType,
                CreatedAt = relationship.CreatedAt,
                IsActive = relationship.IsActive,
                Notes = relationship.Notes
            };

            return CreatedAtAction(nameof(GetChildren), dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating relationship between parent {ParentUserId} and child {ChildUserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, childUserId);
            return StatusCode(500, "An error occurred while creating the parent-child relationship");
        }
    }

    /// <summary>
    /// Remove a parent-child relationship
    /// </summary>
    [HttpDelete("children/{childUserId}/relationship")]
    [RequireParentOrAdmin]
    public async Task<ActionResult> RemoveChildRelationship(string childUserId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Verify the user is a parent of this child or is an admin
            if (!await _permissionService.IsInRoleAsync(userId, ApplicationRoles.Admin) &&
                !await _permissionService.IsParentOfAsync(userId, childUserId))
            {
                return Forbid("You are not the parent of this child");
            }

            var success = await _permissionService.RemoveParentChildRelationshipAsync(userId, childUserId);
            if (!success)
            {
                return NotFound("Parent-child relationship not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing relationship between parent {ParentUserId} and child {ChildUserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, childUserId);
            return StatusCode(500, "An error occurred while removing the parent-child relationship");
        }
    }
}

// DTOs for the API
public class ChildUserDto
{
    public string Id { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; }
}

public class ChildPermissionConfigDto
{
    public string ChildUserId { get; set; } = string.Empty;
    public string ParentUserId { get; set; } = string.Empty;
    public bool CanViewPasswords { get; set; }
    public bool CanCreatePasswords { get; set; }
    public bool CanEditPasswords { get; set; }
    public bool CanDeletePasswords { get; set; }
    public bool CanRevealPasswords { get; set; }
    public bool CanSharePasswords { get; set; }
    public bool CanExportData { get; set; }
    public bool CanCreateCollections { get; set; }
    public bool CanManageOwnCollections { get; set; }
    public int MaxPasswordItems { get; set; }
    public string? AccessStartTime { get; set; }
    public string? AccessEndTime { get; set; }
    public string? AllowedDaysOfWeek { get; set; }
    public bool RequireParentApproval { get; set; }
    public bool LogActivities { get; set; }
}

public class UpdateChildPermissionConfigDto
{
    public bool CanViewPasswords { get; set; } = true;
    public bool CanCreatePasswords { get; set; } = false;
    public bool CanEditPasswords { get; set; } = false;
    public bool CanDeletePasswords { get; set; } = false;
    public bool CanRevealPasswords { get; set; } = false;
    public bool CanSharePasswords { get; set; } = false;
    public bool CanExportData { get; set; } = false;
    public bool CanCreateCollections { get; set; } = false;
    public bool CanManageOwnCollections { get; set; } = true;
    public int MaxPasswordItems { get; set; } = 0;
    public string? AccessStartTime { get; set; }
    public string? AccessEndTime { get; set; }
    public string? AllowedDaysOfWeek { get; set; }
    public bool RequireParentApproval { get; set; } = true;
    public bool LogActivities { get; set; } = true;
}

public class CreateChildRelationshipDto
{
    public string? Notes { get; set; }
}

public class UserRelationshipDto
{
    public int Id { get; set; }
    public string ParentUserId { get; set; } = string.Empty;
    public string ChildUserId { get; set; } = string.Empty;
    public string RelationshipType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
}