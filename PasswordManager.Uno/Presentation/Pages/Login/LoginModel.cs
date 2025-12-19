using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PasswordManager.Uno.Services.Sync;
using System.Net.Http.Json;

namespace PasswordManager.Mobile.Presentation.Pages.Login;

public partial class LoginModel : ObservableObject
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SyncService _syncService;
    private readonly ILogger<LoginModel> _logger;

    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private bool isLoggedIn;

    public LoginModel(
        IHttpClientFactory httpClientFactory,
        SyncService syncService,
        ILogger<LoginModel> logger)
    {
        _httpClientFactory = httpClientFactory;
        _syncService = syncService;
        _logger = logger;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Please enter email and password";
            return;
        }

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var client = _httpClientFactory.CreateClient("PasswordManagerApi");
            var loginRequest = new { Email, Password };
            
            var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                
                if (result?.Token != null)
                {
                    // Store token and set up sync service
                    _syncService.SetAuthToken(result.Token);
                    
                    // Initial sync from server
                    var syncResult = await _syncService.SyncFromServerAsync();
                    
                    if (syncResult.Success)
                    {
                        IsLoggedIn = true;
                        _logger.LogInformation("Login successful, synced {ItemsAdded} items and {CategoriesAdded} categories", 
                            syncResult.ItemsAdded, syncResult.CategoriesAdded);
                    }
                    else
                    {
                        ErrorMessage = $"Login successful but sync failed: {syncResult.ErrorMessage}";
                    }
                }
            }
            else
            {
                ErrorMessage = "Login failed. Please check your credentials.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            ErrorMessage = $"Login error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ClearError()
    {
        ErrorMessage = null;
    }
}

public class LoginResponse
{
    public string? Token { get; set; }
    public string? UserId { get; set; }
    public string? Email { get; set; }
}
