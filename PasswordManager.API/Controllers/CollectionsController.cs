using Microsoft.AspNetCore.Mvc;
using PasswordManager.Services.Interfaces;
using PasswordManager.Models.DTOs;

namespace PasswordManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CollectionsController : ControllerBase
{
    private readonly ICollectionApiService _collectionService;
    private readonly ILogger<CollectionsController> _logger;

    public CollectionsController(
        ICollectionApiService collectionService,
        ILogger<CollectionsController> logger)
    {
        _collectionService = collectionService;
        _logger = logger;
    }

    /// <summary>
    /// Get all collections
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CollectionDto>>> GetAll()
    {
        try
        {
            var collections = await _collectionService.GetAllAsync();
            return Ok(collections);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all collections");
            return StatusCode(500, "An error occurred while retrieving collections");
        }
    }

    /// <summary>
    /// Get a collection by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<CollectionDto>> GetById(int id)
    {
        try
        {
            var collection = await _collectionService.GetByIdAsync(id);
            if (collection == null)
                return NotFound($"Collection with ID {id} not found");

            return Ok(collection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving collection with ID {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the collection");
        }
    }

    /// <summary>
    /// Get collection hierarchy
    /// </summary>
    [HttpGet("hierarchy")]
    public async Task<ActionResult<IEnumerable<CollectionDto>>> GetHierarchy()
    {
        try
        {
            var collections = await _collectionService.GetHierarchyAsync();
            return Ok(collections);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving collection hierarchy");
            return StatusCode(500, "An error occurred while retrieving collection hierarchy");
        }
    }

    /// <summary>
    /// Create a new collection
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CollectionDto>> Create([FromBody] CreateCollectionDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var collection = await _collectionService.CreateAsync(createDto);
            return CreatedAtAction(nameof(GetById), new { id = collection.Id }, collection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating collection");
            return StatusCode(500, "An error occurred while creating the collection");
        }
    }

    /// <summary>
    /// Update a collection
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<CollectionDto>> Update(int id, [FromBody] UpdateCollectionDto updateDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var collection = await _collectionService.UpdateAsync(id, updateDto);
            if (collection == null)
                return NotFound($"Collection with ID {id} not found");

            return Ok(collection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating collection with ID {Id}", id);
            return StatusCode(500, "An error occurred while updating the collection");
        }
    }

    /// <summary>
    /// Delete a collection
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            var success = await _collectionService.DeleteAsync(id);
            if (!success)
                return NotFound($"Collection with ID {id} not found");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting collection with ID {Id}", id);
            return StatusCode(500, "An error occurred while deleting the collection");
        }
    }
}
