using Microsoft.AspNetCore.Mvc;
using PasswordManager.Services.Interfaces;
using PasswordManager.Models.DTOs;

namespace PasswordManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryApiService _categoryService;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(
        ICategoryApiService categoryService,
        ILogger<CategoriesController> logger)
    {
        _categoryService = categoryService;
        _logger = logger;
    }

    /// <summary>
    /// Get all categories
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetAll()
    {
        try
        {
            var categories = await _categoryService.GetAllAsync();
            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all categories");
            return StatusCode(500, "An error occurred while retrieving categories");
        }
    }

    /// <summary>
    /// Get a category by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<CategoryDto>> GetById(int id)
    {
        try
        {
            var category = await _categoryService.GetByIdAsync(id);
            if (category == null)
                return NotFound($"Category with ID {id} not found");

            return Ok(category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving category with ID {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the category");
        }
    }

    /// <summary>
    /// Get categories by collection ID
    /// </summary>
    [HttpGet("collection/{collectionId}")]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetByCollectionId(int collectionId)
    {
        try
        {
            var categories = await _categoryService.GetByCollectionIdAsync(collectionId);
            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving categories for collection {CollectionId}", collectionId);
            return StatusCode(500, "An error occurred while retrieving categories");
        }
    }

    /// <summary>
    /// Create a new category
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create([FromBody] CreateCategoryDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var category = await _categoryService.CreateAsync(createDto);
            return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category");
            return StatusCode(500, "An error occurred while creating the category");
        }
    }

    /// <summary>
    /// Update a category
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<CategoryDto>> Update(int id, [FromBody] UpdateCategoryDto updateDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var category = await _categoryService.UpdateAsync(id, updateDto);
            if (category == null)
                return NotFound($"Category with ID {id} not found");

            return Ok(category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category with ID {Id}", id);
            return StatusCode(500, "An error occurred while updating the category");
        }
    }

    /// <summary>
    /// Delete a category
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            var success = await _categoryService.DeleteAsync(id);
            if (!success)
                return NotFound($"Category with ID {id} not found");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category with ID {Id}", id);
            return StatusCode(500, "An error occurred while deleting the category");
        }
    }
}
