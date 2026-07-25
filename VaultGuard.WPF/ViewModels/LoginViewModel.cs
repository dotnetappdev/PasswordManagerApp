using Microsoft.Extensions.DependencyInjection;
using VaultGuard.Models.DTOs.Auth;
using VaultGuard.Services.Interfaces;
using VaultGuard.WPF.Services;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;


namespace VaultGuard.WPF.ViewModels;

public class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly IVaultSessionService _vaultSessionService;
    private readonly ISecureStorageService _secureStorageService;
    private readonly IUserProfileService _userProfileService;
    private readonly ITwoFactorService? _twoFactorService;
    private readonly IMasterPasswordCacheService? _masterPasswordCacheService;
    private readonly IWindowsHelloService? _helloService;
    private readonly IOidcSsoService? _ssoService;
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
    private bool _showRegistrationForm = false;

    // ── 2FA quick-unlock state ───────────────────────────────────────
    private bool _requiresTwoFactor = false;
    private bool _useBackupCode = false;
    private string _twoFactorCode = string.Empty;

    // ── Windows Hello quick-unlock state ─────────────────────────────
    private bool _canUseWindowsHello = false;

    public LoginViewModel(IServiceProvider serviceProvider)
    {
        _authService = serviceProvider.GetRequiredService<IAuthService>();
        _vaultSessionService = serviceProvider.GetRequiredService<IVaultSessionService>();
        _secureStorageService = serviceProvider.GetRequiredService<ISecureStorageService>();
        _userProfileService = serviceProvider.GetRequiredService<IUserProfileService>();
        _twoFactorService = serviceProvider.GetService<ITwoFactorService>();
        _masterPasswordCacheService = serviceProvider.GetService<IMasterPasswordCacheService>();
        _helloService = serviceProvider.GetService<IWindowsHelloService>();
        _ssoService = serviceProvider.GetService<IOidcSsoService>();

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

                    // Single-user setups skip the profile picker, so evaluate 2FA quick-unlock here
                    // too — otherwise a 2FA-enabled single user with a cached master password would
                    // still be shown the master-password box instead of the authenticator field.
                    _ = EvaluateTwoFactorQuickUnlockAsync(singleUser);
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
                    return;
                }
                catch (Exception ex) when (ex.Message.Contains("no such table") || 
                                          ex.Message.Contains("database is locked") ||
                                          ex.Message.Contains("SQLite Error"))
                {
                    // Database still initializing, wait a bit more
                    await Task.Delay(pollIntervalMs);
                    totalWaitTime += pollIntervalMs;
                }
            }
            
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Warning("Suppressed exception", ex); }
    }

    private void UpdateUIForSetupMode()
    {

        if (_isFirstTimeSetup)
        {
            PageTitle = "Set up Vault Guard";
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

    public new string ErrorMessage
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

    public new bool HasError => !string.IsNullOrEmpty(ErrorMessage);

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

    /// <summary>Every configured OIDC provider from appsettings.json's Sso:Providers section - drives the profile picker's list of "Continue with &lt;provider&gt;" buttons. Empty when none are configured.</summary>
    public IReadOnlyList<VaultGuard.Models.Configuration.SsoProviderConfig> SsoProviders =>
        _ssoService?.ConfiguredProviders ?? Array.Empty<VaultGuard.Models.Configuration.SsoProviderConfig>();

    public bool ShowPasswordEntry => !ShowProfileSelection;

    public bool ShowLockMessage
    {
        get => _showLockMessage;
        set => SetProperty(ref _showLockMessage, value);
    }

    public bool ShowRegistrationForm
    {
        get => _showRegistrationForm;
        set => SetProperty(ref _showRegistrationForm, value);
    }

    /// <summary>
    /// True when the selected profile has 2FA enabled AND a cached master password is
    /// available on this device — in that case the UI shows the TOTP/recovery-code field
    /// instead of the master-password textbox.
    /// </summary>
    public bool RequiresTwoFactor
    {
        get => _requiresTwoFactor;
        set
        {
            if (SetProperty(ref _requiresTwoFactor, value))
            {
                OnPropertyChanged(nameof(ShowMasterPasswordEntry));
            }
        }
    }

    /// <summary>True while the "Use a recovery code instead" toggle is active.</summary>
    public bool UseBackupCode
    {
        get => _useBackupCode;
        set
        {
            if (SetProperty(ref _useBackupCode, value))
            {
                OnPropertyChanged(nameof(TwoFactorFieldLabel));
                OnPropertyChanged(nameof(TwoFactorFieldPlaceholder));
            }
        }
    }

    public string TwoFactorCode
    {
        get => _twoFactorCode;
        set => SetProperty(ref _twoFactorCode, value);
    }

    public string TwoFactorFieldLabel => UseBackupCode ? "Recovery code" : "Authenticator code";

    public string TwoFactorFieldPlaceholder => UseBackupCode ? "XXXX XXXX" : "6-digit code";

    /// <summary>The classic master-password textbox is shown unless we're doing a 2FA-only quick unlock.</summary>
    public bool ShowMasterPasswordEntry => !RequiresTwoFactor;

    /// <summary>
    /// True when the selected profile has Windows Hello linked (a real TPM-backed key registered via
    /// the Passkeys page) AND a cached master password is available on this device — in that case the
    /// UI offers an "Unlock with Windows Hello" button. Independent of <see cref="RequiresTwoFactor"/>:
    /// a profile can offer either, both, or neither.
    /// </summary>
    public bool CanUseWindowsHello
    {
        get => _canUseWindowsHello;
        set => SetProperty(ref _canUseWindowsHello, value);
    }

    public void GoToRegistration()
    {
        ShowRegistrationForm = true;
        OnPropertyChanged(nameof(ShowRegistrationForm));
    }

    public void GoBackFromRegistration()
    {
        ShowRegistrationForm = false;
        OnPropertyChanged(nameof(ShowRegistrationForm));
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
        try
        {
            // Add more detailed logging for debugging

            // If we have a selected user, we need to authenticate against that specific user
            if (SelectedUser != null)
            {
                // Enforce 2FA as a genuine second factor. If the profile has 2FA enabled, the
                // master password ALONE must NOT unlock the vault — previously it did, silently
                // skipping the code. Verify the password, cache it (so the existing TOTP path can
                // finish the unlock), then switch the UI to the authenticator-code step.
                if (_twoFactorService != null && !RequiresTwoFactor)
                {
                    var status = await _twoFactorService.GetTwoFactorStatusAsync(SelectedUser.Id);
                    if (status.IsEnabled)
                    {
                        var passwordOk = await _userProfileService.VerifyMasterPasswordAsync(SelectedUser.Id, MasterPassword);
                        if (!passwordOk)
                        {
                            var userDetails = await _userProfileService.GetUserByIdAsync(SelectedUser.Id);
                            ErrorMessage = userDetails is UserProfileDetailsDto d && !string.IsNullOrEmpty(d.MasterPasswordHint)
                                ? $"Incorrect master password. Hint: {d.MasterPasswordHint}"
                                : "Incorrect master password. Please try again.";
                            return false;
                        }

                        // Cache the verified password so AuthenticateWithTwoFactorAsync can complete
                        // the unlock once the code is verified — the user won't retype it.
                        if (_masterPasswordCacheService != null)
                        {
                            await _masterPasswordCacheService.CacheMasterPasswordAsync(SelectedUser.Id, MasterPassword);
                        }

                        UseBackupCode = false;
                        TwoFactorCode = string.Empty;
                        ErrorMessage = string.Empty;
                        RequiresTwoFactor = true; // Flip the UI to the authenticator-code step.
                        return false; // Not signed in yet — awaiting the 2FA code.
                    }
                }

                return await AuthenticateSpecificUserAsync(SelectedUser, MasterPassword);
            }

            // Fallback to original authentication (try all users)
            var loginResult = await _authService.AuthenticateAsync(MasterPassword);

            if (loginResult)
            {
                // Cache the master password (DPAPI) for the authenticated user so that a 2FA-enabled
                // profile can quick-unlock next time with just the authenticator code — no retyping.
                // Without this the fallback path never armed the cache and 2FA users kept being asked
                // for their master password every sign-in.
                if (_masterPasswordCacheService != null)
                {
                    var uid = _authService.CurrentUser?.Id ?? await _authService.GetCurrentUserIdAsync();
                    if (!string.IsNullOrEmpty(uid))
                    {
                        await _masterPasswordCacheService.CacheMasterPasswordAsync(uid, MasterPassword);
                    }
                }

                return true;
            }
            else
            {
                
                // First check if any users exist in the database
                try
                {
                    var users = await _userProfileService.GetAllUsersAsync();
                    var activeUsers = users?.Where(u => u?.IsActive == true).ToList() ?? new List<UserDto>();
                    
                    if (activeUsers.Count == 0)
                    {
                        ErrorMessage = "No users found in the database. Please check your database setup or create a new account.";
                        return false;
                    }
                    else
                    {
                    }
                }
                catch (Exception userEx)
                {
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
                
                return false;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Login error: {ex.Message}";
            return false;
        }
    }

    /// <summary>
    /// Completes a "sign in from your phone" flow: a phone scanned the QR this device showed and sent
    /// back the master password (end-to-end encrypted, already decrypted by the caller). Run the normal
    /// local sign-in with it so this device unlocks its own vault — works in local/SQLite mode.
    /// </summary>
    public async Task<bool> CompleteQrPhoneSignInAsync(string email, string password)
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
            {
                ErrorMessage = "Sign-in from phone failed (missing details).";
                return false;
            }

            var success = await _authService.LoginAsync(email, password);
            if (success)
            {
                if (_masterPasswordCacheService != null)
                {
                    var uid = _authService.CurrentUser?.Id ?? await _authService.GetCurrentUserIdAsync();
                    if (!string.IsNullOrEmpty(uid))
                        await _masterPasswordCacheService.CacheMasterPasswordAsync(uid, password);
                }
                IsAuthenticated = true;
                return true;
            }

            ErrorMessage = "Sign-in from phone failed. Please try again.";
            return false;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Sign-in failed: {ex.Message}";
            return false;
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasError));
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
        RequiresTwoFactor = false;
        UseBackupCode = false;
        TwoFactorCode = string.Empty;

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

        // Decide whether this profile can do a 2FA-only quick unlock: 2FA must be enabled for
        // the user AND we must already have a cached master password for them on this device.
        _ = EvaluateTwoFactorQuickUnlockAsync(user);
        _ = EvaluateWindowsHelloQuickUnlockAsync(user);
    }

    // Set when an SSO sign-in matched a profile by email only (no persisted AspNetUserLogins link
    // yet) - carries just enough to create that link once, and only once, the user has separately
    // proven they hold this profile's master password (see AuthenticateSpecificUserAsync). Cleared
    // as soon as it's consumed, or if the user backs out to the profile picker.
    private (string ProviderId, string? ProviderDisplayName, string Subject, string UserId)? _pendingSsoLink;

    /// <summary>
    /// Opens the system browser for the given provider's sign-in, then selects the matching local
    /// profile exactly as clicking its tile would (<see cref="SelectUserProfile"/>). Prefers a
    /// persisted AspNetUserLogins link (keyed on the IdP's stable "sub" claim) over a same-email
    /// match, since email can drift out of sync between the IdP and the local profile. This never
    /// authenticates the user: it only jumps to the master-password step for whichever profile
    /// matches, since only the master password can derive the vault key. Returns true if a matching
    /// profile was found and selected.
    /// </summary>
    public async Task<bool> SignInWithSsoAsync(string providerId)
    {
        if (_ssoService == null || !_ssoService.ConfiguredProviders.Any(p => p.Id == providerId))
        {
            ErrorMessage = "That sign-in provider is not configured.";
            return false;
        }

        try
        {
            IsLoading = true;
            var result = await _ssoService.SignInAsync(providerId);
            if (!result.Success || string.IsNullOrWhiteSpace(result.Email))
            {
                ErrorMessage = result.ErrorMessage ?? "Sign-in failed.";
                return false;
            }

            var providerDisplayName = _ssoService.ConfiguredProviders.FirstOrDefault(p => p.Id == providerId)?.DisplayName;

            var linkedMatch = !string.IsNullOrWhiteSpace(result.Subject)
                ? await _userProfileService.FindByExternalLoginAsync(providerId, result.Subject)
                : null;

            if (linkedMatch != null)
            {
                _pendingSsoLink = null;
                SelectUserProfile(linkedMatch);
                return true;
            }

            var users = await _userProfileService.GetAllUsersAsync();
            var match = users?.FirstOrDefault(u =>
                u?.IsActive == true && string.Equals(u.Email, result.Email, StringComparison.OrdinalIgnoreCase));

            if (match == null)
            {
                ErrorMessage = $"No local profile matches the account {result.Email}.";
                return false;
            }

            // No persisted link yet - remember to create one once the master password below proves
            // this really is that profile's owner.
            _pendingSsoLink = !string.IsNullOrWhiteSpace(result.Subject)
                ? (providerId, providerDisplayName, result.Subject, match.Id)
                : null;

            SelectUserProfile(match);
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Sign-in failed: {ex.Message}";
            return false;
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasError));
        }
    }

    /// <summary>
    /// Checks whether the selected profile qualifies for 2FA-only quick unlock (2FA enabled +
    /// cached master password present). If so, swaps the UI to the TOTP/recovery-code field
    /// instead of the master-password textbox. Users with 2FA disabled, or with 2FA enabled
    /// but no cached password yet, keep the regular master-password flow unchanged.
    /// </summary>
    private async Task EvaluateTwoFactorQuickUnlockAsync(UserDto user)
    {
        try
        {
            if (_twoFactorService == null || _masterPasswordCacheService == null)
            {
                RequiresTwoFactor = false;
                return;
            }

            var status = await _twoFactorService.GetTwoFactorStatusAsync(user.Id);
            if (!status.IsEnabled)
            {
                RequiresTwoFactor = false;
                return;
            }

            var hasCachedPassword = await _masterPasswordCacheService.HasCachedMasterPasswordAsync(user.Id);
            RequiresTwoFactor = hasCachedPassword;
        }
        catch
        {
            // If the 2FA status check fails for any reason, fall back to the safe path:
            // the regular master-password flow.
            RequiresTwoFactor = false;
        }
    }

    /// <summary>
    /// Checks whether the selected profile qualifies for Windows Hello quick unlock: a Hello key is
    /// actually registered on this PC (real TPM check, not a database flag) AND a cached master
    /// password is present. Purely local — this does not consult or claim the server-side FIDO2
    /// "PasskeysEnabled" account flag used by the Web/mobile passkey system.
    /// </summary>
    private async Task EvaluateWindowsHelloQuickUnlockAsync(UserDto user)
    {
        try
        {
            if (_helloService == null || _masterPasswordCacheService == null)
            {
                CanUseWindowsHello = false;
                return;
            }

            if (!await _helloService.IsAvailableAsync() ||
                !await _helloService.KeyExistsAsync(WindowsHelloService.DefaultKeyName))
            {
                CanUseWindowsHello = false;
                return;
            }

            CanUseWindowsHello = await _masterPasswordCacheService.HasCachedMasterPasswordAsync(user.Id);
        }
        catch
        {
            CanUseWindowsHello = false;
        }
    }

    /// <summary>
    /// Unlocks with Windows Hello: requires a genuine TPM-backed signature (real biometric/PIN
    /// prompt) via <see cref="IWindowsHelloService.VerifyAsync"/> before releasing the DPAPI-cached
    /// master password. Hello proves presence only — the actual vault key still comes from the
    /// cached master password, exactly as with the 2FA quick-unlock path.
    /// </summary>
    public async Task<bool> AuthenticateWithWindowsHelloAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            if (SelectedUser == null)
            {
                ErrorMessage = "No profile selected.";
                return false;
            }

            if (_helloService == null || _masterPasswordCacheService == null)
            {
                ErrorMessage = "Windows Hello unlock is unavailable right now.";
                return false;
            }

            var cachedPassword = await _masterPasswordCacheService.GetCachedMasterPasswordAsync(SelectedUser.Id);
            if (string.IsNullOrEmpty(cachedPassword))
            {
                ErrorMessage = "Your saved sign-in expired on this device. Please enter your master password.";
                CanUseWindowsHello = false;
                return false;
            }

            var helloResult = await _helloService.VerifyAsync("Unlock your VaultGuard vault");
            if (helloResult != HelloResult.Success)
            {
                ErrorMessage = helloResult == HelloResult.Cancelled
                    ? "Windows Hello verification was cancelled."
                    : "Windows Hello verification failed. Please enter your master password.";
                return false;
            }

            return await AuthenticateSpecificUserAsync(SelectedUser, cachedPassword);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Windows Hello unlock failed: {ex.Message}";
            return false;
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasError));
        }
    }

    public void ToggleBackupCodeMode()
    {
        UseBackupCode = !UseBackupCode;
        TwoFactorCode = string.Empty;
        ErrorMessage = string.Empty;
        OnPropertyChanged(nameof(HasError));
    }

    /// <summary>
    /// Falls back to the regular master-password textbox for this session, e.g. when the user
    /// can't access their authenticator or recovery codes right now.
    /// </summary>
    public void SwitchToMasterPasswordEntry()
    {
        RequiresTwoFactor = false;
        TwoFactorCode = string.Empty;
        ErrorMessage = string.Empty;
        OnPropertyChanged(nameof(HasError));
    }

    public void GoBackToProfileSelection()
    {
        _pendingSsoLink = null;
        SelectedUser = null;
        ShowProfileSelection = true;
        ShowLockMessage = false; // Hide lock message when going back to selection
        PageTitle = "Choose Your Profile";
        MasterPassword = string.Empty;
        ErrorMessage = string.Empty;
        RequiresTwoFactor = false;
        UseBackupCode = false;
        TwoFactorCode = string.Empty;
        CanUseWindowsHello = false;

        OnPropertyChanged(nameof(ShowProfileSelection));
        OnPropertyChanged(nameof(ShowPasswordEntry));
        OnPropertyChanged(nameof(ShowLockMessage));
    }

    /// <summary>
    /// Verifies the entered TOTP or recovery code for the selected profile and, on success,
    /// retrieves the DPAPI-cached master password and feeds it into the existing
    /// AuthenticateSpecificUserAsync path unchanged — the user never retypes their password.
    /// </summary>
    public async Task<bool> AuthenticateWithTwoFactorAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            if (SelectedUser == null)
            {
                ErrorMessage = "No profile selected.";
                return false;
            }

            if (_twoFactorService == null || _masterPasswordCacheService == null)
            {
                ErrorMessage = "Two-factor authentication is unavailable right now.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(TwoFactorCode))
            {
                ErrorMessage = UseBackupCode
                    ? "Enter your recovery code."
                    : "Enter your 6-digit authenticator code.";
                return false;
            }

            // Recovery codes are displayed/typed as "XXXX XXXX" — strip the separator before verifying.
            var code = UseBackupCode
                ? TwoFactorCode.Replace(" ", string.Empty).Trim()
                : TwoFactorCode.Trim();

            var verified = await _twoFactorService.VerifyTwoFactorCodeAsync(SelectedUser.Id, code, UseBackupCode);
            if (!verified)
            {
                ErrorMessage = UseBackupCode
                    ? "That recovery code didn't match. Try again."
                    : "That code didn't match. Try again.";
                return false;
            }

            var cachedPassword = await _masterPasswordCacheService.GetCachedMasterPasswordAsync(SelectedUser.Id);
            if (string.IsNullOrEmpty(cachedPassword))
            {
                // Cache must have been cleared/forgotten between selecting the profile and
                // submitting the code — fall back to asking for the master password directly.
                RequiresTwoFactor = false;
                ErrorMessage = "Your saved sign-in expired on this device. Please enter your master password.";
                return false;
            }

            return await AuthenticateSpecificUserAsync(SelectedUser, cachedPassword);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Verification failed: {ex.Message}";
            return false;
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasError));
        }
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
                // Cache the master password (DPAPI-encrypted, scoped to this Windows user) so that
                // future unlocks for a 2FA-enabled profile can skip retyping it. Safe to call
                // unconditionally; users with 2FA disabled simply never read this cache back.
                if (_masterPasswordCacheService != null)
                {
                    await _masterPasswordCacheService.CacheMasterPasswordAsync(user.Id, masterPassword);
                }

                // The master password just verified is proof enough to persist the SSO link that
                // SignInWithSsoAsync deferred - see _pendingSsoLink's doc comment for why this can't
                // happen any earlier.
                if (_pendingSsoLink is { } pending && pending.UserId == user.Id)
                {
                    await _userProfileService.LinkExternalLoginAsync(user.Id, pending.ProviderId, pending.Subject, pending.ProviderDisplayName);
                    _pendingSsoLink = null;
                }

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
            ErrorMessage = "Authentication failed. Please try again.";
            return false;
        }
    }

    /// <summary>
    /// Wipes the DPAPI-cached master password for the given user, forcing master-password
    /// re-entry next time even if 2FA is enabled. Surfaced via a "Forget this device" action.
    /// </summary>
    public static async Task ForgetDeviceAsync(IServiceProvider serviceProvider, string userId)
    {
        var cacheService = serviceProvider.GetService<IMasterPasswordCacheService>();
        if (cacheService != null)
        {
            await cacheService.ForgetDeviceAsync(userId);
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
            using var scope = ((App)System.Windows.Application.Current).Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<VaultGuard.DAL.VaultGuardDbContext>();
            
            var dbUsers = await dbContext.Users.ToListAsync();
            
            foreach (var user in dbUsers)
            {
            }
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Warning("Suppressed exception", ex); }
    }
}
