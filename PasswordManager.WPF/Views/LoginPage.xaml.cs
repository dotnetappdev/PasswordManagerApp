using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Services.Interfaces;
using PasswordManager.WPF.ViewModels;
using PasswordManager.Models.DTOs.Auth;
using PasswordManager.Models;
using PasswordManager.Crypto.Interfaces;
using Microsoft.AspNetCore.Identity;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PasswordManager.WPF.Views;

/// <summary>
/// Login page for the Password Manager application with master password authentication
/// </summary>
public sealed partial class LoginPage : Page
{
    private IServiceProvider? _serviceProvider;
    private LoginViewModel? _viewModel;
    private UserProfileSelectionViewModel? _profileSelectionViewModel;

    public LoginPage()
    {
        this.InitializeComponent();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        // Focus the master password field when the page loads
        if (this.FindName("MasterPasswordBox") is PasswordBox masterPasswordBox)
        {
            masterPasswordBox.Focus();
        }
    }

    private async void MasterPasswordBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            await DoPrimaryActionAsync();
        }
    }

    // Custom navigation handler for WPF (replacing WinUI's OnNavigatedTo)
    public void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
    {
        // Note: WPF Page doesn't have base.OnNavigatedTo
        if (e.ExtraData is IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _viewModel = new LoginViewModel(serviceProvider);
            _profileSelectionViewModel = new UserProfileSelectionViewModel(serviceProvider);

            // Set data context for the main view
            this.DataContext = _viewModel;

            // Set data context for user profiles list
            if (this.FindName("UserProfilesList") is ItemsControl userProfilesList)
            {
                userProfilesList.ItemsSource = _profileSelectionViewModel.UserProfiles;
            }


            // Check if already authenticated after a brief delay for initialization
            _ = CheckAuthenticationStatusAsync();
        }
        else
        { }
    }

    private async Task CheckAuthenticationStatusAsync()
    {
        try
        {
            // Give the ViewModel time to initialize and check authentication
            await Task.Delay(100);

            // If already authenticated, navigate to home
            if (_viewModel?.IsAuthenticated == true)
            {
                if (GetMainWindow() is MainWindow mainWindow)
                {
                    mainWindow.NavigateToHome();
                }
            }
        }
        catch (Exception ex)
        { }
    }

    private async Task DoPrimaryActionAsync()
    {
        if (_viewModel == null)
        {
            return;
        }


        // Resolve UI elements once for this handler
        var primaryActionButton = this.FindName("PrimaryActionButton") as Button;
        var authProgressRing = this.FindName("AuthProgressRing") as System.Windows.Controls.ProgressBar;
        var masterPasswordBox = this.FindName("MasterPasswordBox") as PasswordBox;
        var confirmPasswordBox = this.FindName("ConfirmPasswordBox") as PasswordBox;
        var passwordHintBox = this.FindName("PasswordHintBox") as TextBox;

        try
        {
            if (primaryActionButton != null) primaryActionButton.IsEnabled = false;
            if (authProgressRing != null) authProgressRing.IsIndeterminate = true;

            // Update ViewModel with current values
            _viewModel.MasterPassword = masterPasswordBox?.Password ?? string.Empty;
            _viewModel.ConfirmMasterPassword = confirmPasswordBox?.Password ?? string.Empty;
            _viewModel.PasswordHint = passwordHintBox?.Text ?? string.Empty;


            // Attempt authentication (handles both setup and login)
            var success = await _viewModel.AuthenticateAsync();


            if (success)
            {
                // Clear password fields for security
                if (masterPasswordBox != null) masterPasswordBox.Password = string.Empty;
                if (confirmPasswordBox != null) confirmPasswordBox.Password = string.Empty;
                if (passwordHintBox != null) passwordHintBox.Text = string.Empty;

                // Navigate to main dashboard via MainWindow
                if (GetMainWindow() is MainWindow mainWindow)
                {
                    mainWindow.NavigateToHome();
                }
            }
        }
        catch (Exception ex)
        {
            // Error handling is done in ViewModel
        }
        finally
        {
            if (primaryActionButton != null) primaryActionButton.IsEnabled = true;
            if (authProgressRing != null) authProgressRing.IsIndeterminate = false;
        }
    }

    // Event handler remains async void for XAML Click binding
    private async void PrimaryActionButton_Click(object sender, RoutedEventArgs e)
    {
        await DoPrimaryActionAsync();
    }

    private MainWindow? GetMainWindow()
    {
        // Use the MainWindow property exposed in App
        return (App.Current as App)?.MainWindow;
    }

    #region Legacy Methods for Backward Compatibility

    // Keep these methods for any existing references, but redirect to the new flow
    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        await DoPrimaryActionAsync();
    }



    private void ProfileButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is UserDto user && _viewModel != null)
        {
            _viewModel.SelectUserProfile(user);
        }
    }

    private void BackToProfilesButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel?.GoBackToProfileSelection();
    }

    private void CreateProfileButton_Click(object sender, RoutedEventArgs e) => ShowRegistrationPanel();
    private void CreateAccountButton_Click(object sender, RoutedEventArgs e) => ShowRegistrationPanel();

    private void ShowRegistrationPanel()
    {
        ClearRegistrationForm();
        _viewModel?.GoToRegistration();
    }

    // ── Back button inside the registration panel ──────────────────────
    private void RegBackButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel?.GoBackFromRegistration();
    }

    // ── Field change handlers — clear error highlight on edit ──────────
    private void RegField_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox tb)
            ClearFieldError(GetBorderFor(tb));
        HideRegError();
    }

    private void RegField_PasswordChanged(object sender, RoutedEventArgs e) => HideRegError();

    private void RegPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        HideRegError();
        if (RegPasswordBox == null) return;
        var pw = RegPasswordBox.Password;
        if (string.IsNullOrEmpty(pw)) { RegStrengthPanel.Visibility = Visibility.Collapsed; ResetReqs(); return; }
        RegStrengthPanel.Visibility = Visibility.Visible;

        UpdateReq(RegReqLength,  pw.Length >= 8,                        "8+ chars");
        UpdateReq(RegReqUpper,   pw.Any(char.IsUpper),                  "Uppercase");
        UpdateReq(RegReqLower,   pw.Any(char.IsLower),                  "Lowercase");
        UpdateReq(RegReqNumber,  pw.Any(char.IsDigit),                  "Number");
        UpdateReq(RegReqSpecial, pw.Any(c => !char.IsLetterOrDigit(c)), "Symbol");

        // Score 0-4: one point each for length≥8, upper, lower, digit, special (cap at 4)
        var score = Math.Min(new[] {
            pw.Length >= 8, pw.Any(char.IsUpper), pw.Any(char.IsLower),
            pw.Any(char.IsDigit), pw.Any(c => !char.IsLetterOrDigit(c))
        }.Count(x => x), 4);

        var (label, hex) = score switch {
            4 => ("Strong",    "#10B981"),
            3 => ("Good",      "#34D399"),
            2 => ("Fair",      "#F59E0B"),
            1 => ("Weak",      "#F97316"),
            _ => ("Too short", "#EF4444")
        };
        if (RegStrengthText != null) RegStrengthText.Text = $"Strength: {label}";

        // Colour each segment: filled up to score, rest dim
        var segs = new[] { RegSeg1, RegSeg2, RegSeg3, RegSeg4 };
        for (int i = 0; i < segs.Length; i++)
        {
            if (segs[i] == null) continue;
            segs[i].Background = i < score
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex))
                : new SolidColorBrush(Color.FromArgb(0x1A, 0xFF, 0xFF, 0xFF));
        }
    }

    // ── Submit ─────────────────────────────────────────────────────────
    private async void RegSubmitButton_Click(object sender, RoutedEventArgs e)
    {
        await SubmitRegistrationAsync();
    }

    private async Task SubmitRegistrationAsync()
    {
        if (_serviceProvider == null || RegSubmitButton == null) return;

        // Basic validation
        if (!ValidateRegForm()) return;

        RegSubmitButton.IsEnabled = false;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            var crypto      = scope.ServiceProvider.GetRequiredService<IPasswordCryptoService>();

            var email    = RegEmailBox.Text.Trim();
            var pw       = RegPasswordBox.Password;
            var salt     = crypto.GenerateUserSalt();
            var pwHash   = crypto.CreateMasterPasswordHash(pw, salt);
            var keyId    = crypto.CreateMasterKeyIdentifier(pw, salt);

            var newUser = new ApplicationUser
            {
                UserName             = email,
                Email                = email,
                EmailConfirmed       = true,
                FirstName            = RegFirstNameBox.Text.Trim(),
                LastName             = RegLastNameBox.Text.Trim(),
                IsActive             = true,
                CreatedAt            = DateTime.UtcNow,
                LastModified         = DateTime.UtcNow,
                UserSalt             = Convert.ToBase64String(salt),
                MasterPasswordHash   = pwHash,
                MasterKeyIdentifier  = keyId,
                MasterPasswordHint   = RegHintBox?.Text?.Trim(),
                SecurityStamp        = Guid.NewGuid().ToString(),
                ConcurrencyStamp     = Guid.NewGuid().ToString()
            };

            var createResult = await userManager.CreateAsync(newUser, $"TempPass_{DateTime.UtcNow.Ticks}!");
            if (!createResult.Succeeded)
            {
                ShowRegError(string.Join(" ", createResult.Errors.Select(e => e.Description)));
                return;
            }

            await userManager.AddToRoleAsync(newUser, ApplicationRoles.User);

            // Reload profiles and auto-select the new account
            if (_profileSelectionViewModel != null)
                await _profileSelectionViewModel.LoadUserProfilesAsync();

            _viewModel?.GoBackFromRegistration();
            _viewModel?.SelectUserProfile(new UserDto
            {
                Id        = newUser.Id,
                Email     = newUser.Email ?? string.Empty,
                FirstName = newUser.FirstName,
                LastName  = newUser.LastName,
                IsActive  = true
            });
        }
        catch (Exception ex)
        {
            ShowRegError($"Could not create account: {ex.Message}");
        }
        finally
        {
            if (RegSubmitButton != null) RegSubmitButton.IsEnabled = true;
        }
    }

    // ── Validation ─────────────────────────────────────────────────────
    private bool ValidateRegForm()
    {
        var first = RegFirstNameBox?.Text?.Trim() ?? "";
        var last  = RegLastNameBox?.Text?.Trim()  ?? "";
        var email = RegEmailBox?.Text?.Trim()      ?? "";
        var pw    = RegPasswordBox?.Password       ?? "";
        var conf  = RegConfirmBox?.Password        ?? "";

        if (first.Length < 2)             return Fail(RegFirstNameBorder, "First name must be at least 2 characters.");
        if (!Regex.IsMatch(first, @"^[a-zA-Z\s'\-]+$")) return Fail(RegFirstNameBorder, "First name contains invalid characters.");
        if (last.Length < 2)              return Fail(RegLastNameBorder,  "Last name must be at least 2 characters.");
        if (!Regex.IsMatch(last,  @"^[a-zA-Z\s'\-]+$")) return Fail(RegLastNameBorder, "Last name contains invalid characters.");
        if (!Regex.IsMatch(email, @"^[a-zA-Z0-9@.\-_]+@[a-zA-Z0-9.\-_]+\.[a-zA-Z]{2,}$"))
                                          return Fail(RegEmailBorder,     "Please enter a valid email address.");
        if (pw.Length < 8)                return Fail(RegPasswordBorder,  "Password must be at least 8 characters.");
        if (!pw.Any(char.IsUpper))        return Fail(RegPasswordBorder,  "Password must contain an uppercase letter.");
        if (!pw.Any(char.IsLower))        return Fail(RegPasswordBorder,  "Password must contain a lowercase letter.");
        if (!pw.Any(char.IsDigit))        return Fail(RegPasswordBorder,  "Password must contain a number.");
        if (pw != conf)                   return Fail(RegConfirmBorder,   "Passwords do not match.");
        return true;
    }

    private bool Fail(Border? border, string message)
    {
        SetFieldError(border, true);
        ShowRegError(message);
        return false;
    }

    // ── Helpers ────────────────────────────────────────────────────────
    private void ShowRegError(string msg)
    {
        if (RegErrorText  != null) RegErrorText.Text = msg;
        if (RegErrorBorder != null) RegErrorBorder.Visibility = Visibility.Visible;
    }

    private void HideRegError()
    {
        if (RegErrorBorder != null) RegErrorBorder.Visibility = Visibility.Collapsed;
    }

    private static void SetFieldError(Border? b, bool error)
    {
        if (b == null) return;
        b.BorderBrush     = new SolidColorBrush(error ? Color.FromRgb(0xEF, 0x44, 0x44) : Color.FromArgb(0x1A, 0xFF, 0xFF, 0xFF));
        b.BorderThickness = new Thickness(error ? 1.5 : 1);
    }

    private static void ClearFieldError(Border? b) => SetFieldError(b, false);

    private Border? GetBorderFor(TextBox tb) => tb.Name switch
    {
        "RegFirstNameBox" => RegFirstNameBorder,
        "RegLastNameBox"  => RegLastNameBorder,
        "RegEmailBox"     => RegEmailBorder,
        _ => null
    };

    private static void UpdateReq(TextBlock? tb, bool met, string label)
    {
        if (tb == null) return;
        tb.Text       = $"{(met ? "●" : "○")}  {label}";
        tb.Foreground = new SolidColorBrush(met
            ? (Color)ColorConverter.ConvertFromString("#10B981")
            : (Color)ColorConverter.ConvertFromString("#5A6478"));
    }

    private void ResetReqs()
    {
        foreach (var (tb, label) in new[] {
            (RegReqLength, "8+ chars"), (RegReqUpper, "Uppercase"),
            (RegReqLower,  "Lowercase"), (RegReqNumber, "Number"),
            (RegReqSpecial, "Symbol") })
            UpdateReq(tb, false, label);

        var segs = new[] { RegSeg1, RegSeg2, RegSeg3, RegSeg4 };
        foreach (var s in segs)
            if (s != null) s.Background = new SolidColorBrush(Color.FromArgb(0x1A, 0xFF, 0xFF, 0xFF));
    }

    private void ClearRegistrationForm()
    {
        if (RegFirstNameBox  != null) RegFirstNameBox.Text     = "";
        if (RegLastNameBox   != null) RegLastNameBox.Text      = "";
        if (RegEmailBox      != null) RegEmailBox.Text         = "";
        if (RegPasswordBox   != null) RegPasswordBox.Password  = "";
        if (RegConfirmBox    != null) RegConfirmBox.Password   = "";
        if (RegHintBox       != null) RegHintBox.Text          = "";
        HideRegError();
        ResetReqs();
        if (RegStrengthPanel != null) RegStrengthPanel.Visibility = Visibility.Collapsed;
        foreach (var b in new[] { RegFirstNameBorder, RegLastNameBorder, RegEmailBorder, RegPasswordBorder, RegConfirmBorder })
            ClearFieldError(b);
    }


    #endregion
}
