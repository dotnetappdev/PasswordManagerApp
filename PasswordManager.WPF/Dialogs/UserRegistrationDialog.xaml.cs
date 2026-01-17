using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Services.Interfaces;
using PasswordManager.Crypto.Interfaces;
using PasswordManager.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace PasswordManager.WPF.Dialogs;

/// <summary>
/// Dialog for creating new user accounts with role-based restrictions
/// </summary>
public sealed partial class UserRegistrationDialog : ModernWpf.Controls.ContentDialog, INotifyPropertyChanged
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
        _role_manager_null_check: ;
        _roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        _cryptoService = serviceProvider.GetRequiredService<IPasswordCryptoService>();
        _currentUser = currentUser;

        // Defensive startup: wrap permission and UI initialization so the dialog won't crash
        try
        {
            // Determine if current user can create admin accounts
            DeterminePermissions();

            // Set up initial UI state
            UpdateRoleDescription();
        }
        catch (Exception ex)
        {
            // Disable primary action so user cannot proceed when dialog is in an invalid state
            try { this.IsPrimaryButtonEnabled = false; } catch { }
        }

        // Wire up events (guard against null subscription)
        try
        {
            this.PrimaryButtonClick += UserRegistrationDialog_PrimaryButtonClick;
        }
        catch (Exception ex)
        {
        }
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
            // Default to safe permissions
            CanCreateAdminAccount = false;
            ShowRoleSelection = true;
        }
    }

    private void AdminToggleSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        try
        {
            if (AdminToggleSwitch == null)
            {
                return;
            }

            var isAdmin = AdminToggleSwitch.IsChecked;
            ShowRoleSelection = !isAdmin;

            if (isAdmin)
            {
                if (RoleDescriptionTextBlock != null)
                    RoleDescriptionTextBlock.Text = "Administrators have full access to all features and can manage other users.";
            }
            else
            {
                UpdateRoleDescription();
            }
        }
        catch (Exception ex)
        {
        }
    }

    private void RoleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (RoleComboBox == null) return;
            UpdateRoleDescription();
        }
        catch (Exception ex)
        {
        }
    }

    private void UpdateRoleDescription()
    {
        try
        {
            if (RoleComboBox == null || RoleDescriptionTextBlock == null) return;

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
            else
            {
                // Fallback
                RoleDescriptionTextBlock.Text = "Standard users can manage their own passwords and access all features.";
            }
        }
        catch (Exception ex)
        {
        }
    }

    private async void UserRegistrationDialog_PrimaryButtonClick(ModernWpf.Controls.ContentDialog sender, ModernWpf.Controls.ContentDialogButtonClickEventArgs args)
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
            if (CanCreateAdminAccount && AdminToggleSwitch.IsChecked)
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

            // Additional security validation: Ensure non-admin users cannot create admin accounts
            if (selectedRole == ApplicationRoles.Admin && _currentUser != null)
            {
                var currentUserRoles = await _userManager.GetRolesAsync(_currentUser);
                var isCurrentUserAdmin = currentUserRoles.Contains(ApplicationRoles.Admin);

                if (!isCurrentUserAdmin)
                {
                    ShowErrorMessage("Only administrators can create admin accounts. Access denied.");
                    return false;
                }
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

            return true;
        }
        catch (Exception ex)
        {
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

        if (FirstNameTextBox.Text.Length < 2)
        {
            SetFieldError(FirstNameBorder, true);
            ShowErrorMessage("First name must be at least 2 characters long.");
            FirstNameTextBox.Focus(FocusState.Programmatic);
            return false;
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(FirstNameTextBox.Text, @"^[a-zA-Z\s'\-]+$"))
        {
            SetFieldError(FirstNameBorder, true);
            ShowErrorMessage("First name can only contain letters, spaces, hyphens, and apostrophes.");
            FirstNameTextBox.Focus(FocusState.Programmatic);
            return false;
        }

        // Last Name validation
        if (string.IsNullOrWhiteSpace(LastNameTextBox.Text))
        {
            SetFieldError(LastNameBorder, true);
            ShowErrorMessage("Please enter a last name.");
            LastNameTextBox.Focus(FocusState.Programmatic);
            return false;
        }

        if (LastNameTextBox.Text.Length < 2)
        {
            SetFieldError(LastNameBorder, true);
            ShowErrorMessage("Last name must be at least 2 characters long.");
            LastNameTextBox.Focus(FocusState.Programmatic);
            return false;
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(LastNameTextBox.Text, @"^[a-zA-Z\s'\-]+$"))
        {
            SetFieldError(LastNameBorder, true);
            ShowErrorMessage("Last name can only contain letters, spaces, hyphens, and apostrophes.");
            LastNameTextBox.Focus(FocusState.Programmatic);
            return false;
        }

        // Email validation
        if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
        {
            SetFieldError(EmailBorder, true);
            ShowErrorMessage("Please enter an email address.");
            EmailTextBox.Focus(FocusState.Programmatic);
            return false;
        }

        if (EmailTextBox.Text.Length < 3)
        {
            SetFieldError(EmailBorder, true);
            ShowErrorMessage("Email address must be at least 3 characters long.");
            EmailTextBox.Focus(FocusState.Programmatic);
            return false;
        }

        // Email format validation with legal characters check
        if (!System.Text.RegularExpressions.Regex.IsMatch(EmailTextBox.Text, @"^[a-zA-Z0-9@.\-_]+@[a-zA-Z0-9.\-_]+\.[a-zA-Z]{2,}$"))
        {
            SetFieldError(EmailBorder, true);
            ShowErrorMessage("Please enter a valid email address with only legal characters (letters, numbers, @, ., -, _).");
            EmailTextBox.Focus(FocusState.Programmatic);
            return false;
        }

        // Master password validation
        if (string.IsNullOrWhiteSpace(MasterPasswordBox.Password))
        {
            SetFieldError(MasterPasswordBorder, true);
            ShowErrorMessage("Please enter a master password.");
            MasterPasswordBox.Focus(FocusState.Programmatic);
            return false;
        }

        // Password strength validation
        var password = MasterPasswordBox.Password;
        if (password.Length < 8)
        {
            SetFieldError(MasterPasswordBorder, true);
            ShowErrorMessage("Master password must be at least 8 characters long.");
            MasterPasswordBox.Focus(FocusState.Programmatic);
            return false;
        }

        if (!password.Any(char.IsUpper))
        {
            SetFieldError(MasterPasswordBorder, true);
            ShowErrorMessage("Master password must contain at least one uppercase letter.");
            MasterPasswordBox.Focus(FocusState.Programmatic);
            return false;
        }

        if (!password.Any(char.IsLower))
        {
            SetFieldError(MasterPasswordBorder, true);
            ShowErrorMessage("Master password must contain at least one lowercase letter.");
            MasterPasswordBox.Focus(FocusState.Programmatic);
            return false;
        }

        if (!password.Any(char.IsDigit))
        {
            SetFieldError(MasterPasswordBorder, true);
            ShowErrorMessage("Master password must contain at least one number.");
            MasterPasswordBox.Focus(FocusState.Programmatic);
            return false;
        }

        // Confirm password validation
        if (MasterPasswordBox.Password != ConfirmPasswordBox.Password)
        {
            SetFieldError(MasterPasswordBorder, true);
            SetFieldError(ConfirmPasswordBorder, true);
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

        // Also show in InfoBar for modern toast-like notification
        ValidationInfoBar.Message = message;
        ValidationInfoBar.Severity = InfoBarSeverity.Error;
        ValidationInfoBar.IsOpen = true;
    }

    private void HideErrorMessage()
    {
        ErrorMessageBorder.Visibility = Visibility.Collapsed;
        ValidationInfoBar.IsOpen = false;
    }

    private void SetFieldError(Border border, bool hasError)
    {
        if (hasError)
        {
            border.BorderBrush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Colors.Red);
            border.BorderThickness = new Thickness(2);
        }
        else
        {
            border.BorderBrush = new System.Windows.Media.SolidColorBrush(
                Color.FromArgb(255, 74, 74, 74)); // #4A4A4A
            border.BorderThickness = new Thickness(1);
        }
    }

    private void FirstNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        SetFieldError(FirstNameBorder, false);
        ValidationInfoBar.IsOpen = false;
    }

    private void LastNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        SetFieldError(LastNameBorder, false);
        ValidationInfoBar.IsOpen = false;
    }

    private void EmailTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        SetFieldError(EmailBorder, false);
        ValidationInfoBar.IsOpen = false;
    }

    private void MasterPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        SetFieldError(MasterPasswordBorder, false);
        ValidationInfoBar.IsOpen = false;

        // Update password strength indicator
        var password = MasterPasswordBox.Password;
        if (string.IsNullOrEmpty(password))
        {
            PasswordStrengthPanel.Visibility = Visibility.Collapsed;
            return;
        }

        PasswordStrengthPanel.Visibility = Visibility.Visible;

        var score = CalculatePasswordStrength(password);
        PasswordStrengthBar.Value = score * 20; // Convert 0-5 to 0-100

        var (strength, color) = score switch
        {
            5 => ("Very Strong", System.Windows.Media.Colors.Green),
            4 => ("Strong", System.Windows.Media.Colors.LightGreen),
            3 => ("Medium", System.Windows.Media.Colors.Orange),
            2 => ("Weak", System.Windows.Media.Colors.OrangeRed),
            _ => ("Very Weak", System.Windows.Media.Colors.Red)
        };

        PasswordStrengthText.Text = $"Password strength: {strength}";
        PasswordStrengthBar.Foreground = new System.Windows.Media.SolidColorBrush(color);
    }

    private int CalculatePasswordStrength(string password)
    {
        var score = 0;
        if (password.Length >= 8) score++;
        if (password.Length >= 12) score++;
        if (password.Any(char.IsUpper)) score++;
        if (password.Any(char.IsLower)) score++;
        if (password.Any(char.IsDigit)) score++;
        if (password.Any(ch => !char.IsLetterOrDigit(ch))) score++;
        return Math.Min(score, 5);
    }

    private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        SetFieldError(ConfirmPasswordBorder, false);
        ValidationInfoBar.IsOpen = false;
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
