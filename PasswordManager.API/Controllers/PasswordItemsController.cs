using Microsoft.AspNetCore.Mvc;
using PasswordManager.API.Interfaces;
using PasswordManager.Models.DTOs;

namespace PasswordManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PasswordItemsController : ControllerBase
{
    private readonly IPasswordItemApiService _passwordItemService;
    private readonly ILogger<PasswordItemsController> _logger;

    public PasswordItemsController(
        IPasswordItemApiService passwordItemService,
        ILogger<PasswordItemsController> logger)
    {
        _passwordItemService = passwordItemService;
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
}
