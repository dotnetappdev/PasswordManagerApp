using Microsoft.AspNetCore.Mvc;
using PasswordManager.API.Interfaces;
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
