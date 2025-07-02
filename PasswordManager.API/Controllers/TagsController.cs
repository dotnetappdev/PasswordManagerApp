using Microsoft.AspNetCore.Mvc;
using PasswordManager.API.Interfaces;
using PasswordManager.Models.DTOs;

namespace PasswordManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TagsController : ControllerBase
{
    private readonly ITagApiService _tagService;
    private readonly ILogger<TagsController> _logger;

    public TagsController(
        ITagApiService tagService,
        ILogger<TagsController> logger)
    {
        _tagService = tagService;
        _logger = logger;
    }

    /// <summary>
    /// Get all tags
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TagDto>>> GetAll()
    {
        try
        {
            var tags = await _tagService.GetAllAsync();
            return Ok(tags);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all tags");
            return StatusCode(500, "An error occurred while retrieving tags");
        }
    }

    /// <summary>
    /// Get a tag by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<TagDto>> GetById(int id)
    {
        try
        {
            var tag = await _tagService.GetByIdAsync(id);
            if (tag == null)
                return NotFound($"Tag with ID {id} not found");

            return Ok(tag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tag with ID {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the tag");
        }
    }

    /// <summary>
    /// Create a new tag
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TagDto>> Create([FromBody] CreateTagDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var tag = await _tagService.CreateAsync(createDto);
            return CreatedAtAction(nameof(GetById), new { id = tag.Id }, tag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tag");
            return StatusCode(500, "An error occurred while creating the tag");
        }
    }

    /// <summary>
    /// Update a tag
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<TagDto>> Update(int id, [FromBody] UpdateTagDto updateDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var tag = await _tagService.UpdateAsync(id, updateDto);
            if (tag == null)
                return NotFound($"Tag with ID {id} not found");

            return Ok(tag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tag with ID {Id}", id);
            return StatusCode(500, "An error occurred while updating the tag");
        }
    }

    /// <summary>
    /// Delete a tag
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            var success = await _tagService.DeleteAsync(id);
            if (!success)
                return NotFound($"Tag with ID {id} not found");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tag with ID {Id}", id);
            return StatusCode(500, "An error occurred while deleting the tag");
        }
    }
}
