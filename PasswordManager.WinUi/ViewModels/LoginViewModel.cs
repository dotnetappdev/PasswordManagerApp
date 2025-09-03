using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Models.DTOs.Auth;
using PasswordManager.Services.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PasswordManager.WinUi.ViewModels;

public class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly IVaultSessionService _vaultSessionService;
    private readonly ISecureStorageService _secureStorageService;
    private readonly IUserProfileService _userProfileService;
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
    private bool _isButtonEnabled = true;
    private UserDto? _selectedUser;
    private bool _showProfileSelection = true;

    public LoginViewModel(IServiceProvider serviceProvider)
    {
        _authService = serviceProvider.GetRequiredService<IAuthService>();
        _vaultSessionService = serviceProvider.GetRequiredService<IVaultSessionService>();
        _secureStorageService = serviceProvider.GetRequiredService<ISecureStorageService>();
        _userProfileService = serviceProvider.GetRequiredService<IUserProfileService>();
        
        // Initialize with default state and then asynchronously update
        UpdateUIForSetupMode(); // Set initial UI state
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            
            // First check if user is already authenticated
            var isAlreadyAuthenticated = await _authService.IsAuthenticatedAsync();
            if (isAlreadyAuthenticated)
            {
                // User is already authenticated, we'll let the UI handle this
                // The navigation will be handled by the LoginPage code-behind
                _isAuthenticated = true;
                OnPropertyChanged(nameof(IsAuthenticated));
                return;
            }

            // Check if there are any existing users
            var users = await _userProfileService.GetAllUsersAsync();
            var activeUsers = users.Where(u => u.IsActive).ToList();

            if (activeUsers.Count == 0)
            {
                // First time setup - no users exist
                _isFirstTimeSetup = true;
                ShowProfileSelection = false;
            }
            else
            {
                // Users exist - show profile selection
                _isFirstTimeSetup = false;
                ShowProfileSelection = true;
            }

            UpdateUIForSetupMode();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during initialization: {ex.Message}");
            // Default to first-time setup on error
            _isFirstTimeSetup = true;
            ShowProfileSelection = false;
            UpdateUIForSetupMode();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdateUIForSetupMode()
    {
        System.Diagnostics.Debug.WriteLine($"UpdateUIForSetupMode called - IsFirstTimeSetup: {_isFirstTimeSetup}");
        
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
        OnPropertyChanged(nameof(ShowProfileSelection));
        OnPropertyChanged(nameof(ShowPasswordEntry));
        
        System.Diagnostics.Debug.WriteLine($"UpdateUIForSetupMode completed - PageTitle: {PageTitle}, PrimaryButtonText: {PrimaryButtonText}");
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

    public bool IsButtonEnabled
    {
        get => _isButtonEnabled && !IsLoading;
        set => SetProperty(ref _isButtonEnabled, value);
    }

    public UserDto? SelectedUser
    {
        get => _selectedUser;
        set => SetProperty(ref _selectedUser, value);
    }

    public bool ShowProfileSelection
    {
        get => _showProfileSelection;
        set => SetProperty(ref _showProfileSelection, value);
    }

    public bool ShowPasswordEntry => !ShowProfileSelection;

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

            System.Diagnostics.Debug.WriteLine($"AuthenticateAsync called - IsFirstTimeSetup: {IsFirstTimeSetup}, MasterPassword length: {MasterPassword?.Length}");

            if (string.IsNullOrWhiteSpace(MasterPassword))
            {
                ErrorMessage = "Please enter your master password.";
                System.Diagnostics.Debug.WriteLine("AuthenticateAsync failed - empty password");
                return false;
            }

            if (IsFirstTimeSetup)
            {
                System.Diagnostics.Debug.WriteLine("Calling SetupMasterPasswordAsync");
                return await SetupMasterPasswordAsync();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Calling LoginWithMasterPasswordAsync");
                return await LoginWithMasterPasswordAsync();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Authentication failed: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"AuthenticateAsync error: {ex}");
            return false;
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasError));
            System.Diagnostics.Debug.WriteLine($"AuthenticateAsync completed - IsLoading: {IsLoading}, ErrorMessage: {ErrorMessage}");
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
        // If we have a selected user, we need to authenticate against that specific user
        if (SelectedUser != null)
        {
            return await AuthenticateSpecificUserAsync(SelectedUser, MasterPassword);
        }
        
        // Fallback to original authentication
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

    public void SelectUserProfile(UserDto user)
    {
        SelectedUser = user;
        ShowProfileSelection = false;
        
        // Update UI for selected user
        if (!string.IsNullOrEmpty(user.FirstName) && !string.IsNullOrEmpty(user.LastName))
        {
            PageTitle = $"Welcome back, {user.FirstName}!";
        }
        else if (!string.IsNullOrEmpty(user.FirstName))
        {
            PageTitle = $"Welcome back, {user.FirstName}!";
        }
        else
        {
            PageTitle = "Welcome back!";
        }
        
        OnPropertyChanged(nameof(ShowProfileSelection));
        OnPropertyChanged(nameof(ShowPasswordEntry));
    }

    public void GoBackToProfileSelection()
    {
        SelectedUser = null;
        ShowProfileSelection = true;
        PageTitle = "Sign In";
        MasterPassword = string.Empty;
        ErrorMessage = string.Empty;
        
        OnPropertyChanged(nameof(ShowProfileSelection));
        OnPropertyChanged(nameof(ShowPasswordEntry));
    }

    private async Task<bool> AuthenticateSpecificUserAsync(UserDto user, string masterPassword)
    {
        try
        {
            // For now, use the general auth service
            // In a full implementation, we'd need to modify the auth service to support specific user authentication
            var loginResult = await _authService.AuthenticateAsync(masterPassword);
            
            if (loginResult)
            {
                return true;
            }
            else
            {
                // Try to get password hint for this specific user
                var userDetails = await _userProfileService.GetUserByIdAsync(user.Id);
                if (userDetails is UserProfileDetailsDto details && !string.IsNullOrEmpty(details.MasterPasswordHint))
                {
                    ErrorMessage = $"Incorrect master password. Hint: {details.MasterPasswordHint}";
                }
                else
                {
                    ErrorMessage = "Incorrect master password. Please try again.";
                }
                return false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error authenticating user {user.Email}: {ex.Message}");
            ErrorMessage = "Authentication failed. Please try again.";
            return false;
        }
    }
}