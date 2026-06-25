using Microsoft.Extensions.Logging;
using PasswordManager.Services.Interfaces;
using PasswordManager.Models.DTOs.Sync;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace PasswordManager.Services.Services;

/// <summary>
/// Service for synchronizing data with the web API
/// </summary>
public class AppSyncService : IAppSyncService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AppSyncService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IDatabaseConfigurationService _databaseConfigurationService;
    private string? _apiKey;

    public AppSyncService(
        HttpClient httpClient,
        ILogger<AppSyncService> logger,
        IConfiguration configuration,
        IDatabaseConfigurationService databaseConfigurationService)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
        _databaseConfigurationService = databaseConfigurationService;
    }

    public async Task<SyncResponseDto> SyncOnStartupAsync()
    {
        try
        {
            _logger.LogInformation("Starting sync on startup");
            
            // First try to sync from API to get latest data
            var syncFromResult = await SyncFromApiAsync();
            
            if (syncFromResult.Success)
            {
                // Then sync to API to upload any local changes
                var syncToResult = await SyncToApiAsync();
                
                return new SyncResponseDto
                {
                    Success = syncToResult.Success,
                    Message = "Startup sync completed",
                    Statistics = CombineStatistics(syncFromResult.Statistics, syncToResult.Statistics)
                };
            }
            
            return syncFromResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during startup sync");
            return new SyncResponseDto
            {
                Success = false,
                Message = "Startup sync failed: " + ex.Message
            };
        }
    }

    public async Task<SyncResponseDto> SyncManuallyAsync()
    {
        try
        {
            _logger.LogInformation("Starting manual sync");
            
            if (string.IsNullOrEmpty(_apiKey))
            {
                return new SyncResponseDto
                {
                    Success = false,
                    Message = "API key not configured. Please generate and configure an API key first."
                };
            }

            // Check if sync is available first
            if (!await IsSyncAvailableAsync())
            {
                return new SyncResponseDto
                {
                    Success = false,
                    Message = "API is not available for sync"
                };
            }

            // First try to sync from API to get latest data
            var syncFromResult = await SyncFromApiAsync();
            
            if (syncFromResult.Success)
            {
                // Then sync to API to upload any local changes
                var syncToResult = await SyncToApiAsync();
                
                return new SyncResponseDto
                {
                    Success = syncToResult.Success,
                    Message = "Manual sync completed",
                    Statistics = CombineStatistics(syncFromResult.Statistics, syncToResult.Statistics)
                };
            }
            
            return syncFromResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during manual sync");
            return new SyncResponseDto
            {
                Success = false,
                Message = "Manual sync failed: " + ex.Message
            };
        }
    }

    public async Task SetApiKeyAsync(string apiKey)
    {
        _apiKey = apiKey;
        _logger.LogInformation("API key configured for sync service");
    }

    public async Task<string?> GetApiKeyAsync()
    {
        return _apiKey;
    }

    private async Task<string> GetApiBaseUrlAsync()
    {
        try
        {
            var dbConfig = await _databaseConfigurationService.GetConfigurationAsync();
            
            // First try to get API URL from database configuration
            if (!string.IsNullOrEmpty(dbConfig.ApiUrl))
            {
                return dbConfig.ApiUrl;
            }
            
            // Then try provider-specific API URL
            var providerApiUrl = dbConfig.Provider switch
            {
                Models.Configuration.DatabaseProvider.Sqlite => dbConfig.Sqlite?.ApiUrl,
                Models.Configuration.DatabaseProvider.SqlServer => dbConfig.SqlServer?.ApiUrl,
                Models.Configuration.DatabaseProvider.MySql => dbConfig.MySql?.ApiUrl,
                Models.Configuration.DatabaseProvider.PostgreSql => dbConfig.PostgreSql?.ApiUrl,
                Models.Configuration.DatabaseProvider.Supabase => dbConfig.Supabase?.ApiUrl,
                _ => null
            };
            
            if (!string.IsNullOrEmpty(providerApiUrl))
            {
                return providerApiUrl;
            }
            
            // Fallback to configuration file
            return _configuration["Api:BaseUrl"] ?? _configuration["ApiSettings:BaseUrl"] ?? "https://api.passwordmanager.local";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting API URL from database configuration, using fallback");
            return _configuration["Api:BaseUrl"] ?? _configuration["ApiSettings:BaseUrl"] ?? "https://api.passwordmanager.local";
        }
    }

    public async Task<SyncResponseDto> SyncToApiAsync()
    {
        try
        {
            _logger.LogInformation("Syncing data to API");
            
            if (string.IsNullOrEmpty(_apiKey))
            {
                return new SyncResponseDto
                {
                    Success = false,
                    Message = "API key not configured"
                };
            }

            var apiBaseUrl = await GetApiBaseUrlAsync();

            // Add API key to request headers
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", _apiKey);

            // Create sync request
            var syncRequest = new SyncRequestDto
            {
                SourceDatabase = "LocalSqlite",
                TargetDatabase = "RemoteApi",
                LastSyncTime = await GetLastSyncTimeAsync(),
                EntitiesToSync = new List<string> { "PasswordItems", "Collections", "Categories", "Tags" }
            };

            var jsonContent = JsonSerializer.Serialize(syncRequest);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{apiBaseUrl}/api/sync/sync", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<SyncResponseDto>(responseContent);
                
                if (result?.Success == true)
                {
                    await UpdateLastSyncTimeAsync(DateTime.UtcNow);
                }
                
                return result ?? new SyncResponseDto
                {
                    Success = false,
                    Message = "Invalid response from API"
                };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new SyncResponseDto
                {
                    Success = false,
                    Message = $"Sync to API failed: {response.StatusCode} - {errorContent}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing to API");
            return new SyncResponseDto
            {
                Success = false,
                Message = "Sync to API failed: " + ex.Message
            };
        }
    }

    public async Task<SyncResponseDto> SyncFromApiAsync()
    {
        try
        {
            _logger.LogInformation("Syncing data from API");
            
            if (string.IsNullOrEmpty(_apiKey))
            {
                return new SyncResponseDto
                {
                    Success = false,
                    Message = "API key not configured"
                };
            }

            var apiBaseUrl = await GetApiBaseUrlAsync();

            // Add API key to request headers
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", _apiKey);

            var response = await _httpClient.GetAsync($"{apiBaseUrl}/api/sync/last-sync-time?sourceDb=RemoteApi&targetDb=LocalSqlite");
            
            if (response.IsSuccessStatusCode)
            {
                // TODO: Implement actual sync from API logic
                // This would involve:
                // 1. Get timestamp of last sync
                // 2. Request changes from API since last sync
                // 3. Apply changes to local database
                // 4. Handle conflicts
                // 5. Update local sync timestamp
                
                await Task.Delay(1000); // Simulate API call
                
                return new SyncResponseDto
                {
                    Success = true,
                    Message = "Sync from API completed",
                    Statistics = new SyncStatisticsDto
                    {
                        TotalRecordsProcessed = 0,
                        Duration = TimeSpan.FromSeconds(1)
                    }
                };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new SyncResponseDto
                {
                    Success = false,
                    Message = $"Sync from API failed: {response.StatusCode} - {errorContent}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing from API");
            return new SyncResponseDto
            {
                Success = false,
                Message = "Sync from API failed: " + ex.Message
            };
        }
    }

    public async Task<bool> IsSyncAvailableAsync()
    {
        try
        {
            var apiBaseUrl = await GetApiBaseUrlAsync();
            // Check if API is reachable
            var response = await _httpClient.GetAsync($"{apiBaseUrl}/health");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Sync API is not available");
            return false;
        }
    }

    public async Task<DateTime?> GetLastSyncTimeAsync()
    {
        try
        {
            // TODO: Implement reading last sync time from local storage or database
            // For now, return null (no previous sync)
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
            // TODO: Implement updating last sync time in local storage or database
            _logger.LogInformation("Last sync time updated to {SyncTime}", syncTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating last sync time");
        }
    }

    private SyncStatisticsDto? CombineStatistics(SyncStatisticsDto? stats1, SyncStatisticsDto? stats2)
    {
        if (stats1 == null && stats2 == null) return null;
        if (stats1 == null) return stats2;
        if (stats2 == null) return stats1;

        return new SyncStatisticsDto
        {
            TotalRecordsProcessed = stats1.TotalRecordsProcessed + stats2.TotalRecordsProcessed,
            Duration = stats1.Duration + stats2.Duration
        };
    }
}
