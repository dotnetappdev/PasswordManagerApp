using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PasswordManager.Imports.Interfaces;
using PasswordManager.Models;
using PasswordManager.Services.Interfaces;

namespace PasswordManager.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ImportExportController : ControllerBase
{
    private readonly IImportService _importService;
    private readonly IPasswordItemService _passwordItemService;
    private readonly ILogger<ImportExportController> _logger;

    public ImportExportController(
        IImportService importService,
        IPasswordItemService passwordItemService,
        ILogger<ImportExportController> logger)
    {
        _importService = importService;
        _passwordItemService = passwordItemService;
        _logger = logger;
    }

    /// <summary>
    /// Get list of available import plugins
    /// </summary>
    [HttpGet("plugins")]
    public async Task<IActionResult> GetAvailablePlugins()
    {
        try
        {
            var providers = await _importService.GetAvailableProvidersAsync();
            
            return Ok(new
            {
                success = true,
                plugins = providers.Select(p => new
                {
                    name = p.ProviderName,
                    displayName = p.DisplayName,
                    version = p.Version,
                    supportedExtensions = p.SupportedFileExtensions
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available import plugins");
            return StatusCode(500, new { success = false, error = "Failed to retrieve import plugins" });
        }
    }

    /// <summary>
    /// Import passwords from a file
    /// </summary>
    [HttpPost("import")]
    public async Task<IActionResult> ImportPasswords([FromForm] ImportRequest request)
    {
        try
        {
            if (request.File == null || request.File.Length == 0)
            {
                return BadRequest(new { success = false, error = "No file provided" });
            }

            if (string.IsNullOrEmpty(request.PluginName))
            {
                return BadRequest(new { success = false, error = "Plugin name is required" });
            }

            using var stream = request.File.OpenReadStream();
            
            var result = await _importService.ImportPasswordsAsync(
                request.PluginName,
                stream,
                request.File.FileName);

            if (result.Success)
            {
                return Ok(new
                {
                    success = true,
                    message = result.ErrorMessage,
                    totalProcessed = result.TotalItemsProcessed,
                    successfulImports = result.SuccessfulImports,
                    failedImports = result.FailedImports,
                    warnings = result.Warnings,
                    importedItemsCount = result.ImportedItems.Count,
                    collectionsCreated = result.RequiredCollections.Count,
                    categoriesCreated = result.RequiredCategories.Count
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    error = result.ErrorMessage,
                    warnings = result.Warnings
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing passwords");
            return StatusCode(500, new { success = false, error = "Import failed: " + ex.Message });
        }
    }

    /// <summary>
    /// Get a preview of items that would be imported (first 5 items)
    /// </summary>
    [HttpPost("preview")]
    public async Task<IActionResult> PreviewImport([FromForm] ImportRequest request)
    {
        try
        {
            if (request.File == null || request.File.Length == 0)
            {
                return BadRequest(new { success = false, error = "No file provided" });
            }

            if (string.IsNullOrEmpty(request.PluginName))
            {
                return BadRequest(new { success = false, error = "Plugin name is required" });
            }

            using var stream = request.File.OpenReadStream();
            
            // For now, return a simple success message
            // Full preview implementation would require exposing plugin details through IImportService
            return Ok(new
            {
                success = true,
                message = "Preview not fully implemented - proceed with import",
                items = new List<object>()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing import");
            return StatusCode(500, new { success = false, error = "Preview failed: " + ex.Message });
        }
    }

    /// <summary>
    /// Export passwords to CSV format
    /// </summary>
    [HttpPost("export")]
    public async Task<IActionResult> ExportPasswords([FromBody] ExportRequest request)
    {
        try
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Get all password items for the user
            var items = await _passwordItemService.GetAllAsync();

            // Filter by collection if specified
            if (request.CollectionId.HasValue)
            {
                items = items.Where(i => i.CollectionId == request.CollectionId.Value).ToList();
            }

            // Generate CSV content
            var csv = GenerateCsv(items, request.Format ?? "standard");

            var fileName = $"passwords_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
            
            return File(
                System.Text.Encoding.UTF8.GetBytes(csv),
                "text/csv",
                fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting passwords");
            return StatusCode(500, new { success = false, error = "Export failed: " + ex.Message });
        }
    }

    private string GenerateCsv(IEnumerable<PasswordItem> items, string format)
    {
        var csv = new System.Text.StringBuilder();
        
        // Standard CSV format
        csv.AppendLine("Title,URL,Username,Password,Notes,Type");
        
        foreach (var item in items)
        {
            var title = EscapeCsv(item.Title);
            var url = EscapeCsv(item.LoginItem?.WebsiteUrl ?? item.LoginItem?.Website ?? "");
            var username = EscapeCsv(item.LoginItem?.Username ?? "");
            var password = EscapeCsv(item.LoginItem?.Password ?? "");
            var notes = EscapeCsv(item.LoginItem?.Notes ?? "");
            var type = item.Type.ToString();
            
            csv.AppendLine($"{title},{url},{username},{password},{notes},{type}");
        }
        
        return csv.ToString();
    }

    private string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
            
        // Escape quotes and wrap in quotes if contains comma, quote, or newline
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        
        return value;
    }
}

public class ImportRequest
{
    public IFormFile? File { get; set; }
    public string? PluginName { get; set; }
}

public class ExportRequest
{
    public int? CollectionId { get; set; }
    public string? Format { get; set; }
}
