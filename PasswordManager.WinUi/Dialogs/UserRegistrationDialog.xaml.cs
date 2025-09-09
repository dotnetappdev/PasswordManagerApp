using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Services.Interfaces;
using PasswordManager.Crypto.Interfaces;
using PasswordManager.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace PasswordManager.WinUi.Dialogs;

/// <summary>
/// Dialog for creating new user accounts with role-based restrictions
/// </summary>
public sealed partial class UserRegistrationDialog : ContentDialog, INotifyPropertyChanged
{
    private readonly IServiceProvider _serviceProvider;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IPasswordCryptoService _cryptoService;
    private readonly ApplicationUser? _currentUser;
    private bool _canCreateAdminAccount = true;
    private bool _showRoleSelection = true;

    public event PropertyChangedEventHandler? PropertyChanged;

    public UserRegistrationDialog(IServiceProvider serviceProvider, ApplicationUser? currentUser = null)
    {
        this.InitializeComponent();
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        _roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        _cryptoService = serviceProvider.GetRequiredService<IPasswordCryptoService>();
        _currentUser = currentUser;

        // Determine if current user can create admin accounts
        DeterminePermissions();

        // Set up initial UI state
        UpdateRoleDescription();
        
        // Wire up events
        this.PrimaryButtonClick += UserRegistrationDialog_PrimaryButtonClick;
    }

    public bool CanCreateAdminAccount
    {
        get => _canCreateAdminAccount;
        private set
        {
            if (_canCreateAdminAccount != value)
            {
                _canCreateAdminAccount = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanCreateAdminAccount)));
            }
        }
    }

    public bool ShowRoleSelection
    {
        get => _showRoleSelection;
        private set
        {
            if (_showRoleSelection != value)
            {
                _showRoleSelection = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowRoleSelection)));
            }
        }
    }

    public UserRegistrationResult? Result { get; private set; }

    private void DeterminePermissions()
    {
        try
        {
            // If no current user, assume first-time setup (can create admin)
            if (_currentUser == null)
            {
                CanCreateAdminAccount = true;
                ShowRoleSelection = true;
                return;
            }

            // Check if current user is admin
            var currentUserRoles = _userManager.GetRolesAsync(_currentUser).Result;
            var isCurrentUserAdmin = currentUserRoles.Contains(ApplicationRoles.Admin);

            // Only admins can create admin accounts
            CanCreateAdminAccount = isCurrentUserAdmin;

            // Child users cannot create new users at all
            var isCurrentUserChild = currentUserRoles.Contains(ApplicationRoles.Child);
            if (isCurrentUserChild)
            {
                ShowErrorMessage("Child users are not allowed to create new accounts.");
                this.IsPrimaryButtonEnabled = false;
                return;
            }

            ShowRoleSelection = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error determining permissions: {ex.Message}");
            // Default to safe permissions
            CanCreateAdminAccount = false;
            ShowRoleSelection = true;
        }
    }

    private void AdminToggleSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        var isAdmin = AdminToggleSwitch.IsOn;
        ShowRoleSelection = !isAdmin;
        
        if (isAdmin)
        {
            RoleDescriptionTextBlock.Text = "Administrators have full access to all features and can manage other users.";
        }
        else
        {
            UpdateRoleDescription();
        }
    }

    private void RoleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateRoleDescription();
    }

    private void UpdateRoleDescription()
    {
        if (RoleComboBox.SelectedItem is ComboBoxItem selectedItem)
        {
            var roleTag = selectedItem.Tag?.ToString();
            RoleDescriptionTextBlock.Text = roleTag switch
            {
                ApplicationRoles.Parent => "Parent users can manage child accounts and have full access to their own passwords.",
                ApplicationRoles.User => "Standard users can manage their own passwords and access all features.",
                ApplicationRoles.Child => "Child users have restricted access and are managed by parent users.",
                _ => "Standard users can manage their own passwords and access all features."
            };
        }
    }

    private async void UserRegistrationDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Get a deferral to allow async operations
        var deferral = args.GetDeferral();
        
        try
        {
            args.Cancel = true; // Cancel the default close behavior
            
            if (await CreateUserAsync())
            {
                // Success - close the dialog
                this.Hide();
            }
            // If creation failed, keep dialog open to show error
        }
        finally
        {
            deferral.Complete();
        }
    }

    private async Task<bool> CreateUserAsync()
    {
        try
        {
            // Hide any previous error
            HideErrorMessage();

            // Validate input
            if (!ValidateInput())
            {
                return false;
            }

            // Determine the role
            string selectedRole;
            if (CanCreateAdminAccount && AdminToggleSwitch.IsOn)
            {
                selectedRole = ApplicationRoles.Admin;
            }
            else if (RoleComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                selectedRole = selectedItem.Tag?.ToString() ?? ApplicationRoles.User;
            }
            else
            {
                selectedRole = ApplicationRoles.User;
            }

            // Generate cryptographic components
            var userSalt = _cryptoService.GenerateUserSalt();
            var masterPassword = MasterPasswordBox.Password;
            var masterPasswordHash = _cryptoService.CreateMasterPasswordHash(masterPassword, userSalt);
            var masterKeyIdentifier = _cryptoService.CreateMasterKeyIdentifier(masterPassword, userSalt);

            // Create the user
            var newUser = new ApplicationUser
            {
                UserName = EmailTextBox.Text.Trim(),
                Email = EmailTextBox.Text.Trim(),
                EmailConfirmed = true,
                FirstName = FirstNameTextBox.Text.Trim(),
                LastName = LastNameTextBox.Text.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                UserSalt = Convert.ToBase64String(userSalt),
                MasterPasswordHash = masterPasswordHash,
                MasterKeyIdentifier = masterKeyIdentifier,
                MasterPasswordHint = PasswordHintTextBox.Text?.Trim(),
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString()
            };

            // Create user with a temporary password (they'll use master password to login)
            var tempPassword = $"TempPass_{DateTime.UtcNow.Ticks}!";
            var createResult = await _userManager.CreateAsync(newUser, tempPassword);

            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                ShowErrorMessage($"Failed to create user: {errors}");
                return false;
            }

            // Add user to role
            var roleResult = await _userManager.AddToRoleAsync(newUser, selectedRole);
            if (!roleResult.Succeeded)
            {
                var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                ShowErrorMessage($"User created but failed to assign role: {errors}");
                // Continue anyway, role can be assigned later
            }

            // Set result
            Result = new UserRegistrationResult
            {
                User = newUser,
                Role = selectedRole,
                MasterPassword = masterPassword
            };

            System.Diagnostics.Debug.WriteLine($"Successfully created user: {newUser.Email} with role: {selectedRole}");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating user: {ex.Message}");
            ShowErrorMessage($"An error occurred while creating the user: {ex.Message}");
            return false;
        }
    }

    private bool ValidateInput()
    {
        // First Name validation
        if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text))
        {
            ShowErrorMessage("Please enter a first name.");
            FirstNameTextBox.Focus(FocusState.Programmatic);
            return false;
        }

        // Last Name validation
        if (string.IsNullOrWhiteSpace(LastNameTextBox.Text))
        {
            ShowErrorMessage("Please enter a last name.");
            LastNameTextBox.Focus(FocusState.Programmatic);
            return false;
        }

        // Email validation
        if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
        {
            ShowErrorMessage("Please enter an email address.");
            EmailTextBox.Focus(FocusState.Programmatic);
            return false;
        }

        // Basic email format validation
        if (!EmailTextBox.Text.Contains("@") || !EmailTextBox.Text.Contains("."))
        {
            ShowErrorMessage("Please enter a valid email address.");
            EmailTextBox.Focus(FocusState.Programmatic);
            return false;
        }

        // Master password validation
        if (string.IsNullOrWhiteSpace(MasterPasswordBox.Password))
        {
            ShowErrorMessage("Please enter a master password.");
            MasterPasswordBox.Focus(FocusState.Programmatic);
            return false;
        }

        // Password strength validation
        var password = MasterPasswordBox.Password;
        if (password.Length < 8)
        {
            ShowErrorMessage("Master password must be at least 8 characters long.");
            MasterPasswordBox.Focus(FocusState.Programmatic);
            return false;
        }

        if (!password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit))
        {
            ShowErrorMessage("Master password must contain at least one uppercase letter, one lowercase letter, and one number.");
            MasterPasswordBox.Focus(FocusState.Programmatic);
            return false;
        }

        // Confirm password validation
        if (MasterPasswordBox.Password != ConfirmPasswordBox.Password)
        {
            ShowErrorMessage("Passwords do not match. Please try again.");
            ConfirmPasswordBox.Focus(FocusState.Programmatic);
            return false;
        }

        return true;
    }

    private void ShowErrorMessage(string message)
    {
        ErrorMessageTextBlock.Text = message;
        ErrorMessageBorder.Visibility = Visibility.Visible;
    }

    private void HideErrorMessage()
    {
        ErrorMessageBorder.Visibility = Visibility.Collapsed;
    }
}

/// <summary>
/// Result of user registration operation
/// </summary>
public class UserRegistrationResult
{
    public ApplicationUser User { get; set; } = null!;
    public string Role { get; set; } = string.Empty;
    public string MasterPassword { get; set; } = string.Empty;
}