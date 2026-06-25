using Microsoft.AspNetCore.Mvc;
using VaultGuard.API.Interfaces;
using VaultGuard.Models.DTOs;

namespace VaultGuard.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VaultsController : ControllerBase
{
    private readonly IVaultApiService _vaultService;
    private readonly ILogger<VaultsController> _logger;

    public VaultsController(
        IVaultApiService vaultService,
        ILogger<VaultsController> logger)
    {
        _vaultService = vaultService;
        _logger = logger;
    }

    /// <summary>
    /// Get all vaults
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<VaultDto>>> GetAll()
    {
        try
        {
            var vaults = await _vaultService.GetAllAsync();
            return Ok(vaults);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all vaults");
            return StatusCode(500, "An error occurred while retrieving vaults");
        }
    }

    /// <summary>
    /// Get a vault by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<VaultDto>> GetById(int id)
    {
        try
        {
            var vault = await _vaultService.GetByIdAsync(id);
            if (vault == null)
                return NotFound($"Vault with ID {id} not found");

            return Ok(vault);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving vault with ID {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the vault");
        }
    }

    /// <summary>
    /// Get the default vault
    /// </summary>
    [HttpGet("default")]
    public async Task<ActionResult<VaultDto>> GetDefault()
    {
        try
        {
            var vault = await _vaultService.GetDefaultVaultAsync();
            if (vault == null)
                return NotFound("Default vault not found");

            return Ok(vault);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving default vault");
            return StatusCode(500, "An error occurred while retrieving the default vault");
        }
    }

    /// <summary>
    /// Create a new vault
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<VaultDto>> Create([FromBody] CreateVaultDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var vault = await _vaultService.CreateAsync(createDto);
            return CreatedAtAction(nameof(GetById), new { id = vault.Id }, vault);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating vault");
            return StatusCode(500, "An error occurred while creating the vault");
        }
    }

    /// <summary>
    /// Update a vault
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<VaultDto>> Update(int id, [FromBody] UpdateVaultDto updateDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var vault = await _vaultService.UpdateAsync(id, updateDto);
            if (vault == null)
                return NotFound($"Vault with ID {id} not found");

            return Ok(vault);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating vault with ID {Id}", id);
            return StatusCode(500, "An error occurred while updating the vault");
        }
    }

    /// <summary>
    /// Delete a vault
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            var success = await _vaultService.DeleteAsync(id);
            if (!success)
                return NotFound($"Vault with ID {id} not found");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting vault with ID {Id}", id);
            return StatusCode(500, "An error occurred while deleting the vault");
        }
    }
}
