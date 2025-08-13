using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Services.Interfaces;
using System.Linq;

namespace PasswordManager.WinUi.ViewModels;

public class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly IVaultSessionService _vaultSessionService;
    private readonly ISecureStorageService _secureStorageService;
    private string _masterPassword = string.Empty;
    private string _confirmMasterPassword = string.Empty;
    private string _passwordHint = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isFirstTimeSetup = false;
    private string _pageTitle = "Sign In";
    private string _primaryButtonText = "Unlock";
    private string _passwordLabel = "Master Password";
    private string _passwordPlaceholder = "Enter your master password";
    private bool _isAuthenticated = false;

    public LoginViewModel(IServiceProvider serviceProvider)
    {
        _authService = serviceProvider.GetRequiredService<IAuthService>();
        _vaultSessionService = serviceProvider.GetRequiredService<IVaultSessionService>();
        _secureStorageService = serviceProvider.GetRequiredService<ISecureStorageService>();
        
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            // First check if user is already authenticated
            var isAlreadyAuthenticated = await _authService.IsAuthenticatedAsync();
            if (isAlreadyAuthenticated)
            {
                // User is already authenticated, we'll let the UI handle this
                // The navigation will be handled by the LoginPage code-behind
                _isAuthenticated = true;
                return;
            }

            _isFirstTimeSetup = await _authService.IsFirstTimeSetupAsync();
            UpdateUIForSetupMode();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during initialization: {ex.Message}");
            // Default to first-time setup on error
            _isFirstTimeSetup = true;
            UpdateUIForSetupMode();
        }
    }

    private void UpdateUIForSetupMode()
    {
        if (_isFirstTimeSetup)
        {
            PageTitle = "Set up Password Manager";
            PrimaryButtonText = "Create Master Password";
            PasswordLabel = "Create Master Password";
            PasswordPlaceholder = "Choose a strong master password";
        }
        else
        {
            PageTitle = "Welcome back";
            PrimaryButtonText = "Unlock";
            PasswordLabel = "Master Password";
            PasswordPlaceholder = "Enter your master password";
        }
        
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(PrimaryButtonText));
        OnPropertyChanged(nameof(PasswordLabel));
        OnPropertyChanged(nameof(PasswordPlaceholder));
        OnPropertyChanged(nameof(IsFirstTimeSetup));
        OnPropertyChanged(nameof(ShowConfirmPassword));
        OnPropertyChanged(nameof(ShowPasswordHint));
    }

    public string MasterPassword
    {
        get => _masterPassword;
        set => SetProperty(ref _masterPassword, value);
    }

    public string ConfirmMasterPassword
    {
        get => _confirmMasterPassword;
        set => SetProperty(ref _confirmMasterPassword, value);
    }

    public string PasswordHint
    {
        get => _passwordHint;
        set => SetProperty(ref _passwordHint, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsFirstTimeSetup
    {
        get => _isFirstTimeSetup;
        set => SetProperty(ref _isFirstTimeSetup, value);
    }

    public string PageTitle
    {
        get => _pageTitle;
        set => SetProperty(ref _pageTitle, value);
    }

    public string PrimaryButtonText
    {
        get => _primaryButtonText;
        set => SetProperty(ref _primaryButtonText, value);
    }

    public string PasswordLabel
    {
        get => _passwordLabel;
        set => SetProperty(ref _passwordLabel, value);
    }

    public string PasswordPlaceholder
    {
        get => _passwordPlaceholder;
        set => SetProperty(ref _passwordPlaceholder, value);
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public bool ShowConfirmPassword => IsFirstTimeSetup;

    public bool ShowPasswordHint => IsFirstTimeSetup;

    public bool IsAuthenticated
    {
        get => _isAuthenticated;
        set => SetProperty(ref _isAuthenticated, value);
    }

    // Legacy properties for backward compatibility (not used in new flow)
    public string Username { get; set; } = string.Empty;
    public string UsernameLabel { get; set; } = "Username";
    public string UsernamePlaceholder { get; set; } = "Enter username";
    public string AuthenticationMode { get; set; } = "Local Database";
    public bool IsApiMode => false;

    public async Task<bool> AuthenticateAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(MasterPassword))
            {
                ErrorMessage = "Please enter your master password.";
                return false;
            }

            if (IsFirstTimeSetup)
            {
                return await SetupMasterPasswordAsync();
            }
            else
            {
                return await LoginWithMasterPasswordAsync();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Authentication failed: {ex.Message}";
            return false;
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasError));
        }
    }

    private async Task<bool> SetupMasterPasswordAsync()
    {
        // Validate password confirmation
        if (MasterPassword != ConfirmMasterPassword)
        {
            ErrorMessage = "Passwords do not match. Please try again.";
            return false;
        }

        // Validate password strength
        if (MasterPassword.Length < 8)
        {
            ErrorMessage = "Master password must be at least 8 characters long.";
            return false;
        }

        // Additional password strength checks
        if (!HasUpperCase(MasterPassword) || !HasLowerCase(MasterPassword) || !HasDigit(MasterPassword))
        {
            ErrorMessage = "Master password must contain at least one uppercase letter, one lowercase letter, and one number.";
            return false;
        }

        // Setup master password
        var setupResult = await _authService.SetupMasterPasswordAsync(MasterPassword, PasswordHint);
        
        if (setupResult)
        {
            // Auto-authenticate after setup
            return await LoginWithMasterPasswordAsync();
        }
        else
        {
            ErrorMessage = "Failed to set up master password. Please try again.";
            return false;
        }
    }

    private static bool HasUpperCase(string password) => password.Any(char.IsUpper);
    private static bool HasLowerCase(string password) => password.Any(char.IsLower);
    private static bool HasDigit(string password) => password.Any(char.IsDigit);

    private async Task<bool> LoginWithMasterPasswordAsync()
    {
        var loginResult = await _authService.AuthenticateAsync(MasterPassword);
        
        if (loginResult)
        {
            return true;
        }
        else
        {
            // Check if there's a password hint available
            var hint = await _authService.GetMasterPasswordHintAsync();
            if (!string.IsNullOrEmpty(hint))
            {
                ErrorMessage = $"Incorrect master password. Hint: {hint}";
            }
            else
            {
                ErrorMessage = "Incorrect master password. Please try again.";
            }
            return false;
        }
    }

    // Legacy methods for backward compatibility
    public async Task<bool> LoginAsync()
    {
        return await AuthenticateAsync();
    }

    public async Task<bool> RegisterAsync()
    {
        return await AuthenticateAsync();
    }
}