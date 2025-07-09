using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PasswordManager.Models.DTOs.Auth;
using PasswordManager.Services.Interfaces;

namespace PasswordManager.API.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class UserProfileController : ControllerBase
{
    private readonly IUserProfileService _userProfileService;
    private readonly ILogger<UserProfileController> _logger;

    public UserProfileController(
        IUserProfileService userProfileService,
        ILogger<UserProfileController> logger)
    {
        _userProfileService = userProfileService;
        _logger = logger;
    }

    /// <summary>
    /// Get all users in the system (admin only)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetAllUsers()
    {
        try
        {
            var users = await _userProfileService.GetAllUsersAsync();
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users");
            return StatusCode(500, "An error occurred while getting users");
        }
    }

    /// <summary>
    /// Get user profile by ID (admin only)
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<UserProfileDetailsDto>> GetUserById(string id)
    {
        try
        {
            var user = await _userProfileService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound("User not found");
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId}", id);
            return StatusCode(500, "An error occurred while getting the user");
        }
    }

    /// <summary>
    /// Create a new user (admin only)
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserProfileDto createUserDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var (result, user, errorMessage) = await _userProfileService.CreateUserAsync(createUserDto);
            
            if (!result.Succeeded || user == null)
            {
                return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });
            }

            var userDto = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                IsActive = user.IsActive
            };

            return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, "An error occurred while creating the user");
        }
    }

    /// <summary>
    /// Update a user's profile (admin only)
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateUserProfile(string id, [FromBody] UpdateUserProfileDto updateUserDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != updateUserDto.Id)
            {
                return BadRequest("ID mismatch");
            }

            var (success, errorMessage) = await _userProfileService.UpdateUserProfileAsync(updateUserDto);
            
            if (!success)
            {
                return BadRequest(errorMessage);
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", id);
            return StatusCode(500, "An error occurred while updating the user");
        }
    }

    /// <summary>
    /// Change a user's master password (admin only)
    /// </summary>
    [HttpPut("{id}/password")]
    public async Task<ActionResult> ChangeUserPassword(string id, [FromBody] ChangePasswordDto changePasswordDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != changePasswordDto.Id)
            {
                return BadRequest("ID mismatch");
            }

            var (success, errorMessage) = await _userProfileService.ChangeMasterPasswordAsync(
                changePasswordDto.Id, 
                changePasswordDto.CurrentPassword, 
                changePasswordDto.NewPassword, 
                changePasswordDto.NewMasterPasswordHint);
            
            if (!success)
            {
                return BadRequest(errorMessage);
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", id);
            return StatusCode(500, "An error occurred while changing the user's password");
        }
    }

    /// <summary>
    /// Deactivate a user account (admin only)
    /// </summary>
    [HttpPut("{id}/deactivate")]
    public async Task<ActionResult> DeactivateUser(string id)
    {
        try
        {
            var success = await _userProfileService.DeactivateUserAsync(id);
            
            if (!success)
            {
                return NotFound("User not found or could not be deactivated");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating user {UserId}", id);
            return StatusCode(500, "An error occurred while deactivating the user");
        }
    }

    /// <summary>
    /// Reactivate a user account (admin only)
    /// </summary>
    [HttpPut("{id}/reactivate")]
    public async Task<ActionResult> ReactivateUser(string id)
    {
        try
        {
            var success = await _userProfileService.ReactivateUserAsync(id);
            
            if (!success)
            {
                return NotFound("User not found or could not be reactivated");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reactivating user {UserId}", id);
            return StatusCode(500, "An error occurred while reactivating the user");
        }
    }

    /// <summary>
    /// Delete a user account permanently (admin only)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteUser(string id)
    {
        try
        {
            var success = await _userProfileService.DeleteUserAsync(id);
            
            if (!success)
            {
                return NotFound("User not found or could not be deleted");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", id);
            return StatusCode(500, "An error occurred while deleting the user");
        }
    }
}
