using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PasswordManager.App.Services.Interfaces;
using PasswordManager.Models.DTOs.Sync;

namespace PasswordManager.App.Services;

public class AppSyncService : IAppSyncService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AppSyncService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _apiBaseUrl;
    private const string LAST_SYNC_KEY = "LastSyncTime";

    public AppSyncService(
        HttpClient httpClient,
        ILogger<AppSyncService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
        _apiBaseUrl = _configuration.GetValue<string>("Api:BaseUrl") ?? "https://localhost:7001";
        
        _httpClient.BaseAddress = new Uri(_apiBaseUrl);
        _httpClient.Timeout = TimeSpan.FromMinutes(5); // Allow time for large syncs
    }

    public async Task<SyncResponseDto> SyncOnStartupAsync()
    {
        try
        {
            _logger.LogInformation("Starting sync on app startup");

            // Check if API is available
            if (!await IsSyncAvailableAsync())
            {
                _logger.LogWarning("API is not available for startup sync");
                return new SyncResponseDto
                {
                    Success = false,
                    Message = "API is not available",
                    SyncTime = DateTime.UtcNow
                };
            }

            // Get the last sync time
            var lastSyncTime = await GetLastSyncTimeAsync();

            // Sync to API (push local changes)
            var syncRequest = new SyncRequestDto
            {
                SourceDatabase = "Sqlite",
                TargetDatabase = "SqlServer",
                LastSyncTime = lastSyncTime,
                EntitiesToSync = new List<string> { "Collections", "Categories", "Tags", "PasswordItems" },
                ConflictResolution = SyncConflictResolution.LastWriteWins
            };

            var response = await _httpClient.PostAsJsonAsync("/api/sync", syncRequest);
            
            if (response.IsSuccessStatusCode)
            {
                var syncResult = await response.Content.ReadFromJsonAsync<SyncResponseDto>();
                
                if (syncResult != null && syncResult.Success)
                {
                    await UpdateLastSyncTimeAsync(syncResult.SyncTime);
                    _logger.LogInformation("Startup sync completed successfully");
                }
                
                return syncResult ?? new SyncResponseDto { Success = false, Message = "Invalid response" };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Startup sync failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
                
                return new SyncResponseDto
                {
                    Success = false,
                    Message = $"HTTP {response.StatusCode}: {errorContent}",
                    SyncTime = DateTime.UtcNow
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during startup sync");
            return new SyncResponseDto
            {
                Success = false,
                Message = $"Startup sync error: {ex.Message}",
                SyncTime = DateTime.UtcNow
            };
        }
    }

    public async Task<SyncResponseDto> SyncToApiAsync()
    {
        try
        {
            _logger.LogInformation("Starting manual sync to API");

            var lastSyncTime = await GetLastSyncTimeAsync();

            var syncRequest = new SyncRequestDto
            {
                SourceDatabase = "Sqlite",
                TargetDatabase = "SqlServer",
                LastSyncTime = lastSyncTime,
                EntitiesToSync = new List<string> { "Collections", "Categories", "Tags", "PasswordItems" },
                ConflictResolution = SyncConflictResolution.LastWriteWins
            };

            var response = await _httpClient.PostAsJsonAsync("/api/sync", syncRequest);
            
            if (response.IsSuccessStatusCode)
            {
                var syncResult = await response.Content.ReadFromJsonAsync<SyncResponseDto>();
                
                if (syncResult != null && syncResult.Success)
                {
                    await UpdateLastSyncTimeAsync(syncResult.SyncTime);
                    _logger.LogInformation("Manual sync to API completed successfully");
                }
                
                return syncResult ?? new SyncResponseDto { Success = false, Message = "Invalid response" };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Manual sync to API failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
                
                return new SyncResponseDto
                {
                    Success = false,
                    Message = $"HTTP {response.StatusCode}: {errorContent}",
                    SyncTime = DateTime.UtcNow
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during manual sync to API");
            return new SyncResponseDto
            {
                Success = false,
                Message = $"Sync error: {ex.Message}",
                SyncTime = DateTime.UtcNow
            };
        }
    }

    public async Task<SyncResponseDto> SyncFromApiAsync()
    {
        try
        {
            _logger.LogInformation("Starting sync from API");

            var lastSyncTime = await GetLastSyncTimeAsync();

            var syncRequest = new SyncRequestDto
            {
                SourceDatabase = "SqlServer",
                TargetDatabase = "Sqlite",
                LastSyncTime = lastSyncTime,
                EntitiesToSync = new List<string> { "Collections", "Categories", "Tags", "PasswordItems" },
                ConflictResolution = SyncConflictResolution.LastWriteWins
            };

            var response = await _httpClient.PostAsJsonAsync("/api/sync", syncRequest);
            
            if (response.IsSuccessStatusCode)
            {
                var syncResult = await response.Content.ReadFromJsonAsync<SyncResponseDto>();
                
                if (syncResult != null && syncResult.Success)
                {
                    await UpdateLastSyncTimeAsync(syncResult.SyncTime);
                    _logger.LogInformation("Sync from API completed successfully");
                }
                
                return syncResult ?? new SyncResponseDto { Success = false, Message = "Invalid response" };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Sync from API failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
                
                return new SyncResponseDto
                {
                    Success = false,
                    Message = $"HTTP {response.StatusCode}: {errorContent}",
                    SyncTime = DateTime.UtcNow
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during sync from API");
            return new SyncResponseDto
            {
                Success = false,
                Message = $"Sync error: {ex.Message}",
                SyncTime = DateTime.UtcNow
            };
        }
    }

    public async Task<bool> IsSyncAvailableAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/health");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "API health check failed");
            return false;
        }
    }

    public async Task<DateTime?> GetLastSyncTimeAsync()
    {
        try
        {
            await Task.CompletedTask;
            
            // Store in app preferences/settings
            var lastSyncString = Preferences.Get(LAST_SYNC_KEY, string.Empty);
            
            if (string.IsNullOrEmpty(lastSyncString))
                return null;

            if (DateTime.TryParse(lastSyncString, out var lastSync))
                return lastSync;

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting last sync time");
            return null;
        }
    }

    public async Task UpdateLastSyncTimeAsync(DateTime syncTime)
    {
        try
        {
            await Task.CompletedTask;
            Preferences.Set(LAST_SYNC_KEY, syncTime.ToString("O")); // ISO 8601 format
            _logger.LogDebug("Updated last sync time to {SyncTime}", syncTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating last sync time");
        }
    }
}
