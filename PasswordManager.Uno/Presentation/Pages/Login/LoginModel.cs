using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PasswordManager.Uno.Services.Sync;
using PasswordManager.Uno.Services.Biometric;
using System.Net.Http.Json;
using System.Text;

namespace PasswordManager.Mobile.Presentation.Pages.Login;

public partial class LoginModel : ObservableObject
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SyncService _syncService;
    private readonly IBiometricAuthService _biometricService;
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

    [ObservableProperty]
    private bool showBiometricOption;

    [ObservableProperty]
    private bool enableBiometricLogin;

    [ObservableProperty]
    private string biometricType = string.Empty;

    public LoginModel(
        IHttpClientFactory httpClientFactory,
        SyncService syncService,
        IBiometricAuthService biometricService,
        ILogger<LoginModel> logger)
    {
        _httpClientFactory = httpClientFactory;
        _syncService = syncService;
        _biometricService = biometricService;
        _logger = logger;

        // Initialize biometric availability
        _ = InitializeBiometricAsync();
    }

    private async Task InitializeBiometricAsync()
    {
        try
        {
            var available = await _biometricService.IsBiometricAvailableAsync();
            ShowBiometricOption = available;

            if (available)
            {
                BiometricType = await _biometricService.GetBiometricTypeAsync();
                
                // Check if user has biometric login enabled and try auto-login
                if (_biometricService.IsBiometricLoginEnabled())
                {
                    await TryBiometricLoginAsync();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing biometric");
        }
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

                        // Enable biometric login if user opted in
                        if (EnableBiometricLogin && ShowBiometricOption)
                        {
                            await EnableBiometricAsync();
                        }
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
    private async Task TryBiometricLoginAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var credentials = await _biometricService.GetStoredCredentialsAsync();
            if (credentials == null)
            {
                _logger.LogInformation("No stored credentials or authentication failed");
                return;
            }

            // Set the email and password from stored credentials
            Email = credentials.Value.email;
            Password = credentials.Value.encryptedCredentials;

            // Perform login
            await LoginAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during biometric login");
            ErrorMessage = "Biometric login failed. Please sign in manually.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task EnableBiometricAsync()
    {
        try
        {
            // Store encrypted password securely
            await _biometricService.EnableBiometricLoginAsync(Email, Password);
            _logger.LogInformation("Biometric login enabled successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling biometric login");
            // Don't show error to user as login was successful
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
