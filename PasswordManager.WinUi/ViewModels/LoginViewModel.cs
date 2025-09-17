using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Models.DTOs.Auth;
using PasswordManager.Services.Interfaces;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
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
    private bool _showLockMessage = false;

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

            // Wait for database initialization to complete to avoid race condition
            await WaitForDatabaseInitializationAsync();

            // Debug: Check if users are properly seeded
            await DebugCheckUsersAsync();

            // Check if there are any existing users
            var users = await _userProfileService.GetAllUsersAsync();
            var activeUsers = users?.Where(u => u?.IsActive == true).ToList() ?? new List<UserDto>();

            if (activeUsers.Count == 0)
            {
                // First time setup - no users exist
                _isFirstTimeSetup = true;
                ShowProfileSelection = false;
            }
            else if (activeUsers.Count == 1)
            {
                // Single user - automatically select them and show password entry
                _isFirstTimeSetup = false;
                var singleUser = activeUsers.First();
                
                // Null-safe check for user properties
                if (singleUser != null)
                {
                    SelectedUser = singleUser;
                    ShowProfileSelection = false;
                    ShowLockMessage = true; // Show lock message for single user
                    
                    // Update UI for selected user with null-safe property access
                    if (!string.IsNullOrEmpty(singleUser.FirstName) && !string.IsNullOrEmpty(singleUser.LastName))
                    {
                        PageTitle = $"Welcome back, {singleUser.FirstName}!";
                    }
                    else if (!string.IsNullOrEmpty(singleUser.FirstName))
                    {
                        PageTitle = $"Welcome back, {singleUser.FirstName}!";
                    }
                    else
                    {
                        PageTitle = "Welcome back!";
                    }
                }
                else
                {
                    // User is null, fallback to first-time setup
                    _isFirstTimeSetup = true;
                    ShowProfileSelection = false;
                }
            }
            else
            {
                // Multiple users - show profile selection by default
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

    /// <summary>
    /// Waits for database initialization to complete to avoid race condition with seeding
    /// </summary>
    private async Task WaitForDatabaseInitializationAsync()
    {
        const int maxWaitTimeMs = 10000; // 10 seconds max wait
        const int pollIntervalMs = 100; // Check every 100ms
        int totalWaitTime = 0;

        try
        {
            // Try to access the user service to ensure database is ready
            while (totalWaitTime < maxWaitTimeMs)
            {
                try
                {
                    // Attempt a simple database operation to check if initialization is complete
                    var testUsers = await _userProfileService.GetAllUsersAsync();
                    // If we get here without exception, database is ready
                    System.Diagnostics.Debug.WriteLine($"Database initialization confirmed after {totalWaitTime}ms");
                    return;
                }
                catch (Exception ex) when (ex.Message.Contains("no such table") || 
                                          ex.Message.Contains("database is locked") ||
                                          ex.Message.Contains("SQLite Error"))
                {
                    // Database still initializing, wait a bit more
                    System.Diagnostics.Debug.WriteLine($"Database still initializing, waiting... ({totalWaitTime}ms elapsed)");
                    await Task.Delay(pollIntervalMs);
                    totalWaitTime += pollIntervalMs;
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"Database initialization wait timeout after {maxWaitTimeMs}ms");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during database initialization wait: {ex.Message}");
            // Continue anyway, let the normal error handling deal with it
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

    public bool ShowLockMessage
    {
        get => _showLockMessage;
        set => SetProperty(ref _showLockMessage, value);
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
        try
        {
            // Add more detailed logging for debugging
            System.Diagnostics.Debug.WriteLine($"LoginWithMasterPasswordAsync - Starting authentication process. SelectedUser: {SelectedUser?.Email ?? "None"}");

            // If we have a selected user, we need to authenticate against that specific user
            if (SelectedUser != null)
            {
                System.Diagnostics.Debug.WriteLine($"Authenticating specific user: {SelectedUser.Email}");
                return await AuthenticateSpecificUserAsync(SelectedUser, MasterPassword);
            }

            // Fallback to original authentication (try all users)
            System.Diagnostics.Debug.WriteLine("Attempting master password authentication against all users");
            var loginResult = await _authService.AuthenticateAsync(MasterPassword);

            if (loginResult)
            {
                System.Diagnostics.Debug.WriteLine("Authentication successful");
                return true;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Authentication failed, checking for hints");
                
                // First check if any users exist in the database
                try
                {
                    var users = await _userProfileService.GetAllUsersAsync();
                    var activeUsers = users?.Where(u => u?.IsActive == true).ToList() ?? new List<UserDto>();
                    
                    if (activeUsers.Count == 0)
                    {
                        ErrorMessage = "No users found in the database. Please check your database setup or create a new account.";
                        System.Diagnostics.Debug.WriteLine("No active users found in database");
                        return false;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Found {activeUsers.Count} active users in database");
                    }
                }
                catch (Exception userEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Error checking users: {userEx.Message}");
                    ErrorMessage = "Database error occurred. Please check your database configuration.";
                    return false;
                }

                // Check if there's a password hint available
                var hint = await _authService.GetMasterPasswordHintAsync();
                if (!string.IsNullOrEmpty(hint))
                {
                    ErrorMessage = $"Incorrect master password. Hint: {hint}";
                }
                else
                {
                    ErrorMessage = "Incorrect master password. Please try again. If this is your first login, try 'CommonMaster123!'";
                }
                
                System.Diagnostics.Debug.WriteLine($"Authentication failed with error: {ErrorMessage}");
                return false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoginWithMasterPasswordAsync error: {ex.Message}");
            ErrorMessage = $"Login error: {ex.Message}";
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
        ShowLockMessage = true; // Show lock message when user is selected

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
        OnPropertyChanged(nameof(ShowLockMessage));
    }

    public void GoBackToProfileSelection()
    {
        SelectedUser = null;
        ShowProfileSelection = true;
        ShowLockMessage = false; // Hide lock message when going back to selection
        PageTitle = "Choose Your Profile";
        MasterPassword = string.Empty;
        ErrorMessage = string.Empty;

        OnPropertyChanged(nameof(ShowProfileSelection));
        OnPropertyChanged(nameof(ShowPasswordEntry));
        OnPropertyChanged(nameof(ShowLockMessage));
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

    /// <summary>
    /// Refreshes the login view state and user profiles
    /// </summary>
    public async Task RefreshAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            // Re-initialize the view model state
            await InitializeAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to refresh: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"RefreshAsync error: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Debug method to check if users are properly seeded with crypto data
    /// </summary>
    private async Task DebugCheckUsersAsync()
    {
        try
        {
            // Direct database check to verify seeded users
            using var scope = ((App)Microsoft.UI.Xaml.Application.Current).Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PasswordManager.DAL.PasswordManagerDbContextApp>();
            
            var dbUsers = await dbContext.Users.ToListAsync();
            System.Diagnostics.Debug.WriteLine($"=== DEBUG: Database User Check ===");
            System.Diagnostics.Debug.WriteLine($"Total users in database: {dbUsers.Count}");
            
            foreach (var user in dbUsers)
            {
                System.Diagnostics.Debug.WriteLine($"User: {user.Email}");
                System.Diagnostics.Debug.WriteLine($"  - IsActive: {user.IsActive}");
                System.Diagnostics.Debug.WriteLine($"  - HasUserSalt: {!string.IsNullOrEmpty(user.UserSalt)}");
                System.Diagnostics.Debug.WriteLine($"  - HasMasterPasswordHash: {!string.IsNullOrEmpty(user.MasterPasswordHash)}");
                System.Diagnostics.Debug.WriteLine($"  - MasterPasswordHint: {user.MasterPasswordHint ?? "None"}");
                System.Diagnostics.Debug.WriteLine($"  - HasMasterKeyIdentifier: {!string.IsNullOrEmpty(user.MasterKeyIdentifier)}");
            }
            System.Diagnostics.Debug.WriteLine($"=== End DEBUG ===");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DEBUG: Error checking users: {ex.Message}");
        }
    }
}