using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VaultGuard.Services.Interfaces;
using VaultGuard.WPF.ViewModels;
using VaultGuard.Models.DTOs.Auth;
using VaultGuard.Models;
using VaultGuard.Crypto.Interfaces;
using Microsoft.AspNetCore.Identity;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VaultGuard.WPF.Views;

/// <summary>
/// Login page for the Vault Guard application with master password authentication
/// </summary>
public sealed partial class LoginPage : Page
{
    private IServiceProvider? _serviceProvider;
    private LoginViewModel? _viewModel;
    private UserProfileSelectionViewModel? _profileSelectionViewModel;
    private bool _showMasterPassword;
    private bool _showRegPassword;
    private bool _showRegConfirm;

    private static readonly string _prefFile =
        System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VaultGuard", "show_pw_pref.txt");

    public LoginPage()
    {
        this.InitializeComponent();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        // Restore persistent show-password preference
        try
        {
            if (System.IO.File.Exists(_prefFile) &&
                System.IO.File.ReadAllText(_prefFile).Trim() == "1")
            {
                _showMasterPassword = true;
                if (KeepVisibleCheckBox != null) KeepVisibleCheckBox.IsChecked = true;
                TogglePasswordVisibility(MasterPasswordBox, MasterPasswordVisibleBox, MasterRevealIcon, true);
            }
        }
        catch { /* preference read failure is non-fatal */ }

        FocusActivePasswordField();
    }

    private void FocusActivePasswordField()
    {
        if (_showMasterPassword)
            MasterPasswordVisibleBox?.Focus();
        else
            MasterPasswordBox?.Focus();
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

    // Recovery action: (re)create the built-in default accounts on demand — handy when a fresh /
    // cleared database has no accounts to sign in with.
    private async void SeedAccountsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_serviceProvider == null) return;

        var button = sender as Button;
        var originalContent = button?.Content;
        try
        {
            if (button != null)
            {
                button.IsEnabled = false;
                button.Content = "Creating accounts…";
            }

            using var scope = _serviceProvider.CreateScope();

            // The Identity tables (AspNetRoles / AspNetUsers …) live in the App DbContext. On a freshly
            // created or partially-migrated database they may not exist yet, which is what crashes the
            // seeder ("no such table: AspNetRoles"). Make sure the schema is in place first.
            await EnsureIdentitySchemaAsync(scope.ServiceProvider);

            var seeder = scope.ServiceProvider.GetService<VaultGuard.DAL.Seed.IdentityDataSeeder>();
            if (seeder == null)
            {
                await ShowLoginMessageAsync("Unavailable", "The account seeder could not be loaded.");
                return;
            }

            await seeder.SeedAsync();

            // The seeder creates users via the App context/UserManager; in this project's dual-context
            // setup the master-key crypto fields don't always land where login reads them. Ensure the
            // four default accounts exist and are unlockable with the common master key.
            await EnsureDefaultAccountsLoginableAsync(scope.ServiceProvider);

            // Refresh the on-screen profile list so the new accounts appear immediately.
            try
            {
                _profileSelectionViewModel = new UserProfileSelectionViewModel(_serviceProvider);
                if (this.FindName("UserProfilesList") is ItemsControl userProfilesList)
                    userProfilesList.ItemsSource = _profileSelectionViewModel.UserProfiles;
            }
            catch { /* refresh is best-effort */ }

            await ShowAccountsReadyDialogAsync(
                "admin@passwordmanager.local",
                "CommonMaster123!");
        }
        catch (Exception ex)
        {
            await ShowLoginMessageAsync("Couldn't create accounts", ex.Message);
        }
        finally
        {
            if (button != null)
            {
                button.IsEnabled = true;
                button.Content = originalContent ?? "Create default accounts";
            }
        }
    }

    // Ensures the ASP.NET Identity tables (AspNetRoles / AspNetUsers / …) exist before seeding.
    // They live in the App DbContext and may be missing on a fresh or partially-migrated database.
    private static async Task EnsureIdentitySchemaAsync(IServiceProvider scopedProvider)
    {
        try
        {
            var appCtx = scopedProvider.GetService<VaultGuard.DAL.VaultGuardDbContextApp>();
            if (appCtx == null) return;

            if (await TableExistsAsync(appCtx, "AspNetRoles")) return;

            // 1) Try the normal migration path first.
            try { await appCtx.Database.MigrateAsync(); } catch { }
            if (await TableExistsAsync(appCtx, "AspNetRoles")) return;

            // 2) Migration didn't create the Identity tables (this project's dual-context setup leaves
            //    the App context's migrations unapplied when the main context created the file). Fall
            //    back to running EF's own CREATE script statement-by-statement and ignore any object
            //    that already exists - that creates just the missing Identity tables with correct columns.
            var script = appCtx.Database.GenerateCreateScript();
            foreach (var statement in SplitSqlStatements(script))
            {
                try { await appCtx.Database.ExecuteSqlRawAsync(statement); }
                catch { /* table/index already exists - ignore */ }
            }
        }
        catch { /* best-effort; the seeder surfaces a clear error if schema is still missing */ }
    }

    private static System.Collections.Generic.IEnumerable<string> SplitSqlStatements(string script)
    {
        foreach (var raw in script.Split(';'))
        {
            var statement = raw.Trim();
            if (statement.Length > 0)
                yield return statement;
        }
    }

    private static async Task<bool> TableExistsAsync(Microsoft.EntityFrameworkCore.DbContext ctx, string table)
    {
        try
        {
            var conn = ctx.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name=@n";
            var p = cmd.CreateParameter();
            p.ParameterName = "@n";
            p.Value = table;
            cmd.Parameters.Add(p);
            return await cmd.ExecuteScalarAsync() != null;
        }
        catch
        {
            return false;
        }
    }

    // Guarantees the four default accounts exist and can be unlocked with the common master key, by
    // writing their master-key crypto directly through the context the login flow reads (mirrors the
    // working SetupMasterPasswordAsync path). Safe to run repeatedly.
    private static async Task EnsureDefaultAccountsLoginableAsync(IServiceProvider scopedProvider)
    {
        const string masterKey = "CommonMaster123!";
        var defaults = new (string Email, string First, string Last)[]
        {
            ("admin@passwordmanager.local",  "Administrator", "User"),
            ("parent@passwordmanager.local", "Parent",        "User"),
            ("user@passwordmanager.local",   "Regular",       "User"),
            ("child@passwordmanager.local",  "Child",         "User"),
        };

        try
        {
            var ctx = scopedProvider.GetService<VaultGuard.DAL.VaultGuardDbContext>();
            var crypto = scopedProvider.GetService<VaultGuard.Crypto.Interfaces.IPasswordCryptoService>();
            if (ctx == null || crypto == null) return;

            foreach (var (email, first, last) in defaults)
            {
                var normalized = email.ToUpperInvariant();
                var user = await ctx.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalized || u.Email == email);

                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserName = email,
                        NormalizedUserName = normalized,
                        Email = email,
                        NormalizedEmail = normalized,
                        EmailConfirmed = true,
                        FirstName = first,
                        LastName = last,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        SecurityStamp = Guid.NewGuid().ToString(),
                        ConcurrencyStamp = Guid.NewGuid().ToString()
                    };
                    ctx.Users.Add(user);
                }

                // Always (re)set these well-known default accounts to the common master key so the
                // button reliably makes them unlockable with CommonMaster123!.
                var salt = crypto.GenerateUserSalt();
                user.UserSalt = Convert.ToBase64String(salt);
                user.MasterPasswordHash = crypto.CreateMasterPasswordHash(masterKey, salt);
                user.MasterKeyIdentifier = crypto.CreateMasterKeyIdentifier(masterKey, salt);
                user.IsActive = true;
            }

            await ctx.SaveChangesAsync();
        }
        catch { /* best-effort; if it still fails the login error will make it clear */ }
    }

    private static async Task ShowLoginMessageAsync(string title, string message)
    {
        var dialog = new ModernWpf.Controls.ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK"
        };
        await dialog.ShowAsync();
    }

    // Styled "default accounts created" confirmation — replaces the plain text ContentDialog with
    // a card-style layout that highlights the sign-in email and master key as copyable chips.
    private static async Task ShowAccountsReadyDialogAsync(string email, string masterKey)
    {
        var accent = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));

        var iconBadge = new Border
        {
            Width = 56,
            Height = 56,
            CornerRadius = new CornerRadius(28),
            Background = new SolidColorBrush(Color.FromArgb(0x26, 0x10, 0xB9, 0x81)),
            Margin = new Thickness(0, 0, 0, 16),
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = new TextBlock
            {
                Text = "",
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 26,
                Foreground = accent,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };

        var subtitle = new TextBlock
        {
            Text = "The built-in accounts were created. Sign in with:",
            Foreground = new SolidColorBrush(Color.FromRgb(0xA1, 0xA8, 0xB8)),
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 16)
        };

        var credentialsCard = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(0x14, 0xFF, 0xFF, 0xFF)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(0x1F, 0xFF, 0xFF, 0xFF)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16, 12, 16, 12),
            Child = new StackPanel
            {
                Children =
                {
                    BuildCredentialRow("Email", email),
                    new Border { Height = 10 },
                    BuildCredentialRow("Master key", masterKey)
                }
            }
        };

        var content = new StackPanel
        {
            Margin = new Thickness(4, 8, 4, 0),
            Children = { iconBadge, subtitle, credentialsCard }
        };

        var dialog = new ModernWpf.Controls.ContentDialog
        {
            Title = "Default accounts ready",
            Content = content,
            CloseButtonText = "OK"
        };
        await dialog.ShowAsync();
    }

    private static StackPanel BuildCredentialRow(string label, string value)
    {
        return new StackPanel
        {
            Children =
            {
                new TextBlock
                {
                    Text = label,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x80, 0x88, 0x99)),
                    Margin = new Thickness(0, 0, 0, 2)
                },
                new TextBlock
                {
                    Text = value,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.White
                }
            }
        };
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

        // 2FA-only quick unlock: the master-password textbox isn't shown at all, so skip
        // straight to code verification instead of reading password fields.
        if (_viewModel.RequiresTwoFactor)
        {
            await DoTwoFactorActionAsync();
            return;
        }

        // Resolve UI elements once for this handler
        var primaryActionButton = this.FindName("PrimaryActionButton") as Button;
        var authProgressRing = this.FindName("AuthProgressRing") as ModernWpf.Controls.ProgressRing;
        var masterPasswordBox = this.FindName("MasterPasswordBox") as PasswordBox;
        var confirmPasswordBox = this.FindName("ConfirmPasswordBox") as PasswordBox;
        var passwordHintBox = this.FindName("PasswordHintBox") as TextBox;

        try
        {
            if (primaryActionButton != null) primaryActionButton.IsEnabled = false;
            if (authProgressRing != null) authProgressRing.IsActive = true;

            // Read from whichever field is active (hidden PasswordBox or visible TextBox)
            _viewModel.MasterPassword = (_showMasterPassword
                ? (MasterPasswordVisibleBox?.Text ?? string.Empty)
                : (masterPasswordBox?.Password ?? string.Empty)).Trim();
            _viewModel.ConfirmMasterPassword = (confirmPasswordBox?.Password ?? string.Empty).Trim();
            _viewModel.PasswordHint = passwordHintBox?.Text ?? string.Empty;


            // Attempt authentication (handles both setup and login)
            var success = await _viewModel.AuthenticateAsync();


            if (success)
            {
                // Clear password fields for security before navigating away
                if (masterPasswordBox != null) masterPasswordBox.Password = string.Empty;
                if (confirmPasswordBox != null) confirmPasswordBox.Password = string.Empty;
                if (passwordHintBox != null) passwordHintBox.Text = string.Empty;

                if (GetMainWindow() is MainWindow mainWindow)
                    mainWindow.NavigateToHome();
            }
            else
            {
                // Auth failed — return focus to whichever field is active.
                FocusActivePasswordField();
            }
        }
        catch (Exception ex)
        {
            FocusActivePasswordField();
        }
        finally
        {
            if (primaryActionButton != null) primaryActionButton.IsEnabled = true;
            if (authProgressRing != null) authProgressRing.IsActive = false;
        }
    }

    // ── 2FA-only quick unlock ────────────────────────────────────────────
    private async Task DoTwoFactorActionAsync()
    {
        if (_viewModel == null) return;

        var primaryActionButton = this.FindName("PrimaryActionButton") as Button;
        var authProgressRing = this.FindName("AuthProgressRing") as ModernWpf.Controls.ProgressRing;

        try
        {
            if (primaryActionButton != null) primaryActionButton.IsEnabled = false;
            if (authProgressRing != null) authProgressRing.IsActive = true;

            var success = await _viewModel.AuthenticateWithTwoFactorAsync();

            if (success)
            {
                if (TwoFactorCodeBox != null) TwoFactorCodeBox.Text = string.Empty;

                if (GetMainWindow() is MainWindow mainWindow)
                    mainWindow.NavigateToHome();
            }
            else
            {
                TwoFactorCodeBox?.Focus();
            }
        }
        catch (Exception)
        {
            TwoFactorCodeBox?.Focus();
        }
        finally
        {
            if (primaryActionButton != null) primaryActionButton.IsEnabled = true;
            if (authProgressRing != null) authProgressRing.IsActive = false;
        }
    }

    private async void TwoFactorCodeBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            await DoTwoFactorActionAsync();
        }
    }

    private void ToggleBackupCodeButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel?.ToggleBackupCodeMode();
        TwoFactorCodeBox?.Focus();
    }

    private void UseMasterPasswordButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel?.SwitchToMasterPasswordEntry();
        FocusActivePasswordField();
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

    private void MasterPasswordRevealBtn_Click(object sender, RoutedEventArgs e)
    {
        _showMasterPassword = !_showMasterPassword;
        TogglePasswordVisibility(MasterPasswordBox, MasterPasswordVisibleBox, MasterRevealIcon, _showMasterPassword);
        FocusActivePasswordField();
    }

    // Sync plain-text box → PasswordBox so auth reads the correct value
    private void MasterPasswordVisibleBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (MasterPasswordBox != null)
            MasterPasswordBox.Password = MasterPasswordVisibleBox.Text;
    }

    // Allow Enter key in the visible TextBox to trigger unlock
    private async void MasterPasswordVisibleBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
            await DoPrimaryActionAsync();
    }

    // Persist the "always show" preference to disk
    private void KeepVisibleCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        bool keep = KeepVisibleCheckBox.IsChecked == true;
        try
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_prefFile)!);
            System.IO.File.WriteAllText(_prefFile, keep ? "1" : "0");
        }
        catch { }

        if (keep && !_showMasterPassword)
        {
            _showMasterPassword = true;
            TogglePasswordVisibility(MasterPasswordBox, MasterPasswordVisibleBox, MasterRevealIcon, true);
            FocusActivePasswordField();
        }
        else if (!keep && _showMasterPassword)
        {
            _showMasterPassword = false;
            TogglePasswordVisibility(MasterPasswordBox, MasterPasswordVisibleBox, MasterRevealIcon, false);
            FocusActivePasswordField();
        }
    }

    private void RegPasswordRevealBtn_Click(object sender, RoutedEventArgs e)
    {
        _showRegPassword = !_showRegPassword;
        TogglePasswordVisibility(RegPasswordBox, RegPasswordVisibleBox, RegPasswordRevealIcon, _showRegPassword);
    }

    private void RegConfirmRevealBtn_Click(object sender, RoutedEventArgs e)
    {
        _showRegConfirm = !_showRegConfirm;
        TogglePasswordVisibility(RegConfirmBox, RegConfirmVisibleBox, RegConfirmRevealIcon, _showRegConfirm);
    }

    private static void TogglePasswordVisibility(PasswordBox pb, TextBox tb, TextBlock icon, bool show)
    {
        if (show)
        {
            tb.Text = pb.Password;
            pb.Visibility = Visibility.Collapsed;
            tb.Visibility = Visibility.Visible;
            icon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7C3AED"));
        }
        else
        {
            tb.Visibility = Visibility.Collapsed;
            pb.Visibility = Visibility.Visible;
            tb.Text = string.Empty;
            icon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5A6478"));
        }
    }

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
            // Ensure migrations have run before attempting to create a user.
            // This guards against the race condition where the user registers
            // before the background startup has finished initialising the DB,
            // and also fixes existing databases that are missing columns.
            using (var migScope = _serviceProvider.CreateScope())
            {
                var dbCtxApp = migScope.ServiceProvider.GetRequiredService<VaultGuard.DAL.VaultGuardDbContextApp>();
                await dbCtxApp.Database.MigrateAsync();
            }

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
        tb.Text       = $"{(met ? "✓" : "–")}  {label}";
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

        // Reset reveal state
        if (_showRegPassword && RegPasswordBox != null && RegPasswordVisibleBox != null && RegPasswordRevealIcon != null)
            TogglePasswordVisibility(RegPasswordBox, RegPasswordVisibleBox, RegPasswordRevealIcon, false);
        if (_showRegConfirm && RegConfirmBox != null && RegConfirmVisibleBox != null && RegConfirmRevealIcon != null)
            TogglePasswordVisibility(RegConfirmBox, RegConfirmVisibleBox, RegConfirmRevealIcon, false);
        _showRegPassword = false;
        _showRegConfirm = false;

        HideRegError();
        ResetReqs();
        if (RegStrengthPanel != null) RegStrengthPanel.Visibility = Visibility.Collapsed;
        foreach (var b in new[] { RegFirstNameBorder, RegLastNameBorder, RegEmailBorder, RegPasswordBorder, RegConfirmBorder })
            ClearFieldError(b);
    }


    #endregion
}
