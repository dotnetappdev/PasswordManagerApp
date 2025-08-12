using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Services.Interfaces;

namespace PasswordManager.WinUi.ViewModels;

public class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly IVaultSessionService _vaultSessionService;
    private readonly ISecureStorageService _secureStorageService;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private string _authenticationMode = "Local Database";
    private string _usernameLabel = "Username";
    private string _usernamePlaceholder = "Enter your username";

    public LoginViewModel(IServiceProvider serviceProvider)
    {
        _authService = serviceProvider.GetRequiredService<IAuthService>();
        _vaultSessionService = serviceProvider.GetRequiredService<IVaultSessionService>();
        _secureStorageService = serviceProvider.GetRequiredService<ISecureStorageService>();
        
        _ = LoadAuthenticationModeAsync();
    }

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public string AuthenticationMode
    {
        get => _authenticationMode;
        set => SetProperty(ref _authenticationMode, value);
    }

    public string UsernameLabel
    {
        get => _usernameLabel;
        set => SetProperty(ref _usernameLabel, value);
    }

    public string UsernamePlaceholder
    {
        get => _usernamePlaceholder;
        set => SetProperty(ref _usernamePlaceholder, value);
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public bool IsApiMode => AuthenticationMode == "API Server";

    private async Task LoadAuthenticationModeAsync()
    {
        try
        {
            var mode = await _secureStorageService.GetAsync("AuthenticationMode");
            AuthenticationMode = mode ?? "Local Database";
            
            if (IsApiMode)
            {
                UsernameLabel = "Email";
                UsernamePlaceholder = "Enter your email address";
            }
            else
            {
                UsernameLabel = "Username";
                UsernamePlaceholder = "Enter your username";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading auth mode: {ex.Message}");
        }
    }

    public async Task<bool> LoginAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                var fieldName = IsApiMode ? "email and password" : "username and password";
                ErrorMessage = $"Please enter both {fieldName}.";
                return false;
            }

            // Attempt login
            var loginResult = await _authService.LoginAsync(Username, Password);
            
            if (loginResult)
            {
                return true;
            }
            else
            {
                var modeText = IsApiMode ? "API server" : "local database";
                ErrorMessage = $"Invalid credentials. Please check your {(IsApiMode ? "email" : "username")} and password and try again. Authentication mode: {modeText}.";
                return false;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Login failed: {ex.Message}";
            return false;
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasError));
        }
    }

    public async Task<bool> RegisterAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                var fieldName = IsApiMode ? "email and password" : "username and password";
                ErrorMessage = $"Please enter both {fieldName}.";
                return false;
            }

            // Attempt registration
            var registerResult = await _authService.RegisterAsync(Username, Password);
            
            if (registerResult)
            {
                return true;
            }
            else
            {
                var modeText = IsApiMode ? "API server" : "local database";
                ErrorMessage = $"Registration failed on {modeText}. Please try again.";
                return false;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Registration failed: {ex.Message}";
            return false;
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasError));
        }
    }
}