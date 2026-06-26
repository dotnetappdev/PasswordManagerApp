using ModernWpf.Controls;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VaultGuard.Models;
using VaultGuard.Services.Interfaces;
using VaultGuard.WPF.Models;
using VaultGuard.WPF.Services;

namespace VaultGuard.WPF;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly IServiceProvider _serviceProvider;
    private bool _isAuthenticated = false;
    private string? _currentUserId = null;
    private int? _selectedVaultId = null; // currently highlighted vault row in the sidebar
    private Style? _navItemStyle; // cache for dynamic nav items

    // Plain, chrome-free Button template — no Background/MouseOver visuals at all. The default WPF
    // Button template paints a solid light-blue "hot track" rectangle on hover, which otherwise
    // blots out the vault name text underneath the sidebar's transparent click overlays.
    private static readonly ControlTemplate TransparentOverlayButtonTemplate = CreateTransparentOverlayButtonTemplate();

    private static ControlTemplate CreateTransparentOverlayButtonTemplate()
    {
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.BackgroundProperty, System.Windows.Media.Brushes.Transparent);
        var template = new ControlTemplate(typeof(Button)) { VisualTree = border };
        return template;
    }
    
    // Protected default categories that cannot be deleted
    private static readonly string[] ProtectedCategoryTags = new[] 
    { 
        "LoginCategory", 
        "CreditCardCategory", 
        "SecureNotesCategory", 
        "WiFiCategory",
        "PasskeysCategory"
    };

    public MainWindow(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        this.InitializeComponent();
        this.Title = "Vault Guard";

        // Windows 11 desktop polish: default + minimum size, centred launch, the Win11 UI font,
        // and the Mica backdrop / immersive dark title bar / rounded corners.
        this.Width = 1200;
        this.Height = 800;
        this.MinWidth = 960;
        this.MinHeight = 640;
        this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        this.FontFamily = new System.Windows.Media.FontFamily("Segoe UI Variable Text, Segoe UI");
        Helpers.Win11Chrome.Apply(this, dark: true);

        // Cache style early (after resources loaded by InitializeComponent)
        _navItemStyle = TryGetNavItemStyle();

        // Initialize navigation - start with Login if not authenticated, otherwise Home
        InitializeNavigation();
        // Load dynamic categories for navigation
        _ = RefreshCategoriesAsync();
        // Load the user's vaults into the sidebar
        _ = RefreshVaultsNavAsync();
        // Load sidebar tags as chips
        _ = RefreshTagsAsync();
    }

    private void InitializeNavigation()
    {
        SetAuthenticationState(false);
        NavigateLoginFrame();
    }

    // Creates a fresh LoginPage instance and navigates LoginFrame to it.
    // Must use an instance — Frame.Navigate(Type, ...) renders the Type as text, not a page.
    private void NavigateLoginFrame()
    {
        var page = new Views.LoginPage();
        LoginFrame.Navigate(page, _serviceProvider);
    }

    private void SetAuthenticationState(bool isAuthenticated)
    {
        if (isAuthenticated)
        {
            // Hide login frame and show main navigation
            LoginFrame.Visibility = Visibility.Collapsed;
            MainNavigationView.Visibility = Visibility.Visible;
            MainMenuBar.Visibility = Visibility.Visible;
        }
        else
        {
            // Show login frame and hide main navigation
            LoginFrame.Visibility = Visibility.Visible;
            MainNavigationView.Visibility = Visibility.Collapsed;
            MainMenuBar.Visibility = Visibility.Collapsed;
        }
    }

    // ─── Old-school menu bar (File / Help) ──────────────────────────────────────

    private void MenuSettings_Click(object sender, RoutedEventArgs e) => NavigateToPage("Settings");
    private void MenuAbout_Click(object sender, RoutedEventArgs e) => NavigateToPage("Settings");
    private void MenuExit_Click(object sender, RoutedEventArgs e) => Close();

    private void MenuDocumentation_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://github.com/dotnetappdev/vaultguard/wiki",
                UseShellExecute = true
            });
        }
        catch (System.Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Suppressed exception", logEx); }
    }

    private async void MenuCheckUpdates_Click(object sender, RoutedEventArgs e)
    {
        var svc = _serviceProvider?.GetService<Services.UpdateService>();
        if (svc == null) return;
        try
        {
            var info = await svc.CheckManifestAsync(ReadUpdateManifestUrl());
            if (info == null)
            {
                await ShowInfoMessage("Check for Updates", $"You're up to date (v{svc.GetCurrentVersion()}).");
                return;
            }

            var features = info.Features is { Count: > 0 }
                ? "\n\nWhat's new:\n" + string.Join("\n", info.Features.Select(f => "  •  " + f))
                : string.Empty;
            var appName = string.IsNullOrWhiteSpace(info.AppName) ? "Vault Guard" : info.AppName;

            var dlg = new ModernWpf.Controls.ContentDialog
            {
                Title = "Update available",
                Content = MakeDialogMessage(
                    $"{appName} v{info.Version} is available (you have v{svc.GetCurrentVersion()})."
                    + (info.RequiresRestart ? " A restart is required to apply it." : string.Empty)
                    + features),
                PrimaryButtonText = "Open Settings",
                CloseButtonText = "Later",
                DefaultButton = ModernWpf.Controls.ContentDialogButton.Primary
            };
            ApplyDialogStyle(dlg);
            if (await Helpers.DialogManager.ShowAsync(dlg) == ModernWpf.Controls.ContentDialogResult.Primary)
                NavigateToPage("Settings");
        }
        catch (Exception ex)
        {
            await ShowErrorMessage("Check for Updates", $"Could not check for updates: {ex.Message}");
        }
    }

    // Reads the configured update-manifest URL from the same local settings file the Settings page
    // writes, so the menu's quick check honours the user's configured update source.
    private static string? ReadUpdateManifestUrl()
    {
        try
        {
            var path = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "VaultGuard", "settings.json");
            if (System.IO.File.Exists(path))
            {
                var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(
                    System.IO.File.ReadAllText(path));
                if (dict != null && dict.TryGetValue("UpdateManifestUrl", out var url) && !string.IsNullOrWhiteSpace(url))
                    return url;
            }
        }
        catch (System.Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Suppressed exception", logEx); }
        return Services.UpdateService.DefaultManifestUrl;
    }

    private void MainNavigationView_SelectionChanged(ModernWpf.Controls.NavigationView sender, ModernWpf.Controls.NavigationViewSelectionChangedEventArgs args)
    {
        // Only allow navigation if authenticated
        if (!_isAuthenticated)
        {
            // Prevent selection if not authenticated
            sender.SelectedItem = null;
            return;
        }

        if (args.SelectedItem is ModernWpf.Controls.NavigationViewItem selectedItem)
        {
            string tag = selectedItem.Tag?.ToString() ?? "";

            // "About" opens a dialog rather than navigating to a page.
            if (tag == "About")
            {
                sender.SelectedItem = null;
                _ = ShowAboutDialogAsync();
                return;
            }

            // Only navigate if the item has a tag (leaf items, not parent categories)
            if (!string.IsNullOrEmpty(tag))
            {
                try
                {
                    NavigateToPage(tag);
                }
                catch (Exception ex)
                {
                    // Reset selection on error
                    sender.SelectedItem = null;
                }
            }
        }
    }

    /// <summary>
    /// Application-wide keyboard shortcuts. Global actions (Settings, Lock) work anywhere; the
    /// item actions (copy, open &amp; fill, edit, delete) are dispatched to the items page when shown.
    /// Mirrors the reference list on the Settings → Shortcuts tab.
    /// </summary>
    // App-wide zoom (Ctrl + / Ctrl - / Ctrl 0). Scales the whole window content.
    private readonly System.Windows.Media.ScaleTransform _appScale = new(1.0, 1.0);
    private double _zoom = 1.0;

    private void ApplyZoom(double delta)
    {
        _zoom = System.Math.Round(System.Math.Clamp(_zoom + delta, 0.6, 2.5), 2);
        SetZoom();
    }

    private void ResetZoom()
    {
        _zoom = 1.0;
        SetZoom();
    }

    private void SetZoom()
    {
        _appScale.ScaleX = _zoom;
        _appScale.ScaleY = _zoom;
        if (this.Content is System.Windows.FrameworkElement root && root.LayoutTransform != _appScale)
            root.LayoutTransform = _appScale;
        try { Services.ToastService.Instance.Info($"Zoom {(int)(_zoom * 100)}%"); } catch (System.Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Suppressed exception", logEx); }
    }

    private void MainWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (!_isAuthenticated) return;

        var mods  = System.Windows.Input.Keyboard.Modifiers;
        bool ctrl  = (mods & System.Windows.Input.ModifierKeys.Control) != 0;
        bool shift = (mods & System.Windows.Input.ModifierKeys.Shift) != 0;

        // ── Global ──
        if (ctrl && e.Key == System.Windows.Input.Key.OemComma) { NavigateToPage("Settings"); e.Handled = true; return; }
        if (ctrl && e.Key == System.Windows.Input.Key.L)        { HandleLogout();              e.Handled = true; return; }

        // ── App zoom (Ctrl +, Ctrl -, Ctrl 0 to reset) ──
        if (ctrl && (e.Key == System.Windows.Input.Key.OemPlus || e.Key == System.Windows.Input.Key.Add))
        { ApplyZoom(+0.1); e.Handled = true; return; }
        if (ctrl && (e.Key == System.Windows.Input.Key.OemMinus || e.Key == System.Windows.Input.Key.Subtract))
        { ApplyZoom(-0.1); e.Handled = true; return; }
        if (ctrl && (e.Key == System.Windows.Input.Key.D0 || e.Key == System.Windows.Input.Key.NumPad0))
        { ResetZoom(); e.Handled = true; return; }

        var itemsPage = ContentFrame?.Content as Views.PasswordItemsPage;

        if (ctrl && e.Key == System.Windows.Input.Key.N)
        {
            if (itemsPage != null) itemsPage.TriggerAddNew();
            else NavigateToPage("AllItems");
            e.Handled = true;
            return;
        }

        if (ctrl && e.Key == System.Windows.Input.Key.F)
        {
            if (itemsPage != null) { itemsPage.FocusSearch(); e.Handled = true; }
            else NavigateToPage("AllItems");
            return;
        }

        if (itemsPage == null) return;

        // Don't hijack Ctrl+C / Delete while the user is typing in an input control.
        var focused = System.Windows.Input.Keyboard.FocusedElement;
        bool editing = focused is System.Windows.Controls.Primitives.TextBoxBase
                       || focused is System.Windows.Controls.PasswordBox;

        if (ctrl && shift && e.Key == System.Windows.Input.Key.C && !editing) { itemsPage.CopyUsernameShortcut(); e.Handled = true; }
        else if (ctrl && e.Key == System.Windows.Input.Key.C && !editing)     { itemsPage.CopyPasswordShortcut(); e.Handled = true; }
        else if (ctrl && e.Key == System.Windows.Input.Key.O)                 { itemsPage.OpenAndFillShortcut();  e.Handled = true; }
        else if (ctrl && e.Key == System.Windows.Input.Key.E)                 { itemsPage.EditSelectedShortcut(); e.Handled = true; }
        else if (e.Key == System.Windows.Input.Key.Delete && !editing)        { itemsPage.DeleteSelectedShortcut(); e.Handled = true; }
    }

    private async Task ShowAboutDialogAsync()
    {
        try
        {
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

            var content = new StackPanel { Margin = new Thickness(4) };

            var logo = new Border
            {
                Width = 64,
                Height = 64,
                CornerRadius = new CornerRadius(16),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 12),
                Background = TryFindResource("ModernPrimaryGradientBrush") as Brush
                             ?? new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB))
            };
            logo.Child = new TextBlock
            {
                Text = "",
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 32,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            content.Children.Add(logo);

            content.Children.Add(new TextBlock
            {
                Text = "VaultGuard Vault Guard",
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = TryFindResource("ModernTextPrimaryBrush") as Brush ?? Brushes.White
            });
            content.Children.Add(new TextBlock
            {
                Text = $"Version {version} · Windows Desktop (.NET 10 · WPF)",
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 0),
                Foreground = TryFindResource("ModernTextSecondaryBrush") as Brush ?? Brushes.Gray
            });
            content.Children.Add(new TextBlock
            {
                Text = "The best open-source password manager for Windows and Web.\nEnd-to-end encrypted with AES-256-GCM.",
                FontSize = 12,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 12, 0, 0),
                Foreground = TryFindResource("ModernTextSecondaryBrush") as Brush ?? Brushes.Gray
            });
            content.Children.Add(new TextBlock
            {
                Text = $"© {DateTime.Now.Year} VaultGuard",
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 12, 0, 0),
                Foreground = TryFindResource("ModernTextTertiaryBrush") as Brush ?? Brushes.Gray
            });

            var dialog = new ModernWpf.Controls.ContentDialog
            {
                Title = "About",
                Content = content,
                PrimaryButtonText = "Documentation",
                CloseButtonText = "Close",
                DefaultButton = ModernWpf.Controls.ContentDialogButton.Close
            };

            if (Application.Current.Resources.Contains("Modern1PasswordDialogStyle"))
                dialog.Style = Application.Current.Resources["Modern1PasswordDialogStyle"] as Style;

            if (await Helpers.DialogManager.ShowAsync(dialog) == ModernWpf.Controls.ContentDialogResult.Primary)
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "https://github.com/dotnetappdev/passwordmanager",
                        UseShellExecute = true
                    });
                }
                catch (System.Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Suppressed exception", logEx); }
            }
        }
        catch (System.Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Suppressed exception", logEx); }
    }

    public void NavigateToPage(string pageTag)
    {
        if (!_isAuthenticated && pageTag != "Login") return;

        // Selecting a real nav item deselects any highlighted vault in the sidebar.
        if (_selectedVaultId != null)
        {
            _selectedVaultId = null;
            HighlightSelectedVault();
        }

        try
        {
            // Create a fresh page INSTANCE — Frame.Navigate(Type, ...) renders the Type
            // as text, not as a page, so we must instantiate explicitly.
            System.Windows.Controls.Page page = pageTag switch
            {
                "AllItems" or "Favorites" or "LoginCategory" or "CreditCardCategory" or
                "SecureNotesCategory" or "IdentityCategory" or "WiFiCategory" or
                "PasskeysCategory" or "Archive" or "RecentlyDeleted" or "Passwords"
                    => new Views.PasswordItemsPage(),
                "Profile"           => new Views.ProfilePage(),
                "Categories"        => new Views.CategoriesPage(),
                "Passkeys"          => new Views.PasskeysPage(),
                "ManageItems"       => new Views.ManageItemsPage(),
                "SecurityDashboard" => new Views.DashboardPage(),
                "Security"          => new Views.SecurityPage(),
                "Import"            => new Views.ImportPage(),
                "Settings"          => new Views.SettingsPage(),
                "Home"              => new Views.DashboardPage(),
                "Login"             => new Views.LoginPage(),
                _                   => pageTag.EndsWith("Category", StringComparison.OrdinalIgnoreCase)
                                           ? new Views.PasswordItemsPage()
                                           : (System.Windows.Controls.Page)new Views.DashboardPage()
            };

            object navigationParameter = CreateNavigationParameter(pageTag);

            if (ContentFrame != null)
                ContentFrame.Navigate(page, navigationParameter);
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Warning("Suppressed exception", ex); }
    }

    private object CreateNavigationParameter(string pageTag)
    {
        // For password item pages, create filter data
        if (IsPasswordItemsPage(pageTag))
        {
            var filterData = new NavigationFilterData(_serviceProvider);

            switch (pageTag)
            {
                case "Favorites":
                    filterData.ShowFavorites = true;
                    filterData.FilterName = "Favorites";
                    break;
                case "LoginCategory":
                    filterData.FilterType = ItemType.Login;
                    filterData.FilterName = "Logins";
                    break;
                case "CreditCardCategory":
                    filterData.FilterType = ItemType.CreditCard;
                    filterData.FilterName = "Credit Cards";
                    break;
                case "SecureNotesCategory":
                    filterData.FilterType = ItemType.SecureNote;
                    filterData.FilterName = "Secure Notes";
                    break;
                case "WiFiCategory":
                    filterData.FilterType = ItemType.WiFi;
                    filterData.FilterName = "WiFi";
                    break;
                case "PasskeysCategory":
                    filterData.FilterType = ItemType.Passkey;
                    filterData.FilterName = "Passkeys";
                    break;
                case "Archive":
                    filterData.ShowArchived = true;
                    filterData.FilterName = "Archive";
                    break;
                case "RecentlyDeleted":
                    filterData.ShowDeleted = true;
                    filterData.FilterName = "Recently Deleted";
                    break;
                default:
                    // Handle dynamic categories - filter by category name
                    if (pageTag.EndsWith("Category"))
                    {
                        var categoryName = pageTag.Substring(0, pageTag.Length - 8); // Remove "Category" suffix
                        filterData.FilterCategoryName = categoryName;
                        filterData.FilterName = categoryName;
                    }
                    else
                    {
                        filterData.FilterName = "All Items";
                    }
                    break;
            }

            return filterData;
        }

        // For other pages, just pass the service provider
        return _serviceProvider;
    }

    private bool IsPasswordItemsPage(string pageTag)
    {
        return pageTag switch
        {
            "AllItems" or "Favorites" or "LoginCategory" or "CreditCardCategory" or
            "SecureNotesCategory" or "IdentityCategory" or "WiFiCategory" or "PasskeysCategory" or
            "Archive" or "RecentlyDeleted" or "Passwords" => true,
            // Also treat any tag ending with "Category" as a password items page (for dynamic categories)
            _ => pageTag.EndsWith("Category", StringComparison.OrdinalIgnoreCase)
        };
    }



    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        // Handle search text changes and provide suggestions + auto-filter
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            var query = sender.Text?.ToLower() ?? "";

            if (!string.IsNullOrWhiteSpace(query) && query.Length >= 2)
            {
                // Provide basic search suggestions
                var suggestions = new List<string>();

                // Add common search categories as suggestions
                var commonSearches = new[] { "logins", "passwords", "credit cards", "secure notes", "wifi", "favorites" };
                foreach (var category in commonSearches)
                {
                    if (category.Contains(query))
                    {
                        suggestions.Add(category);
                    }
                }

                // Add "search for" prefix for better UX
                if (suggestions.Count == 0)
                {
                    suggestions.Add($"Search for '{query}'");
                }

                sender.ItemsSource = suggestions;

                // Auto-filter: Navigate to passwords page and apply search immediately
                try
                {
                    NavigateToPage("AllItems");

                    // Apply auto-filter with slight delay to ensure page is loaded
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        PassSearchQueryToPage(query);
                    }));
                }
                catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Warning("Suppressed exception", ex); }
            }
            else
            {
                sender.ItemsSource = null;

                // Clear filters when search is empty
                if (string.IsNullOrWhiteSpace(query))
                {
                    try
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            PassSearchQueryToPage("");
                        }));
                    }
                    catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Warning("Suppressed exception", ex); }
                }
            }
        }
    }

    private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        // Handle search query submission with better feedback
        string searchQuery = args.QueryText?.Trim() ?? "";

        if (!string.IsNullOrEmpty(searchQuery))
        {
            try
            {
                // Navigate to passwords page with search query
                NavigateToPage("AllItems");

                // Use a more reliable way to pass search query
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    PassSearchQueryToPage(searchQuery);
                }));

                // Provide visual feedback
                sender.PlaceholderText = $"Searching for '{searchQuery}'...";

                // Reset placeholder after a delay
                Dispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.Background, new Action(() =>
                {
                    Task.Delay(3000).ContinueWith(_ =>
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            if (sender.PlaceholderText.StartsWith("Searching"))
                            {
                                sender.PlaceholderText = "Search passwords...";
                            }
                        }));
                    });
                }));
            }
            catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Warning("Suppressed exception", ex); }
        }
    }

    private void PassSearchQueryToPage(string searchQuery)
    {
        // Pass search query to the passwords page if it's currently loaded
        if (ContentFrame.Content is Views.PasswordItemsPage passwordsPage)
        {
            // Try to find the search textbox and set the search text
            var searchTextBox = FindChildControl<TextBox>(passwordsPage, "SearchTextBox");
            if (searchTextBox != null)
            {
                // Setting Text triggers TextChanged automatically; no need to manually raise the event
                searchTextBox.Text = searchQuery;
            }
        }
    }

    // Helper method to find child controls
    private T? FindChildControl<T>(DependencyObject parent, string controlName) where T : FrameworkElement
    {
        if (parent == null) return null;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            if (child is T element && element.Name == controlName)
                return element;

            var result = FindChildControl<T>(child, controlName);
            if (result != null)
                return result;
        }

        return null;
    }

    // Called by both LoginFrame and ContentFrame after every navigation.
    // WPF does not automatically call a page's OnNavigatedTo method — we must do it here.
    private void Frame_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
    {
        switch (e.Content)
        {
            case Views.LoginPage p:          p.OnNavigatedTo(e);      break;
            case Views.PasswordItemsPage p:  p.OnNavigatedTo(e);      break;
            case Views.DashboardPage p:      p.OnNavigatedTo(e);      break;
            case Views.ProfilePage p:        p.OnNavigatedTo(e);      break;
            case Views.CategoriesPage p:     p.OnNavigatedTo(e);      break;
            case Views.ImportPage p:         p.OnNavigatedTo(e);      break;
            case Views.ManageItemsPage p:    p.OnNavigatedTo(e);      break;
            case Views.SettingsPage p:       p.OnNavigatedTo(e);      break;
            case Views.VaultsPage p:         p.OnNavigatedTo(e);      break;
            case Views.PasskeysPage p:       p.OnNavigatedTo(e);      break;
            case Views.SecurityPage p:       p.OnNavigatedTo(e);      break;
        }
    }

    // Called by App after the host and startup service have fully initialised the database.
    // Re-navigates to LoginPage so profiles and auth checks run against a live DB.
    public void OnDatabaseInitialized()
    {
        NavigateLoginFrame();
        _ = RefreshCategoriesAsync();
        _ = RefreshVaultsNavAsync();
        _ = RefreshTagsAsync();
    }

    // Public method to allow programmatic navigation (e.g., after login)
    public void NavigateToHome()
    {
        _isAuthenticated = true;

        // Capture current user from auth service and update sidebar
        var authService = _serviceProvider.GetService<IAuthService>();
        if (authService?.CurrentUser != null)
        {
            _currentUserId = authService.CurrentUser.Id;
            var user = authService.CurrentUser;
            var displayName = $"{user.FirstName} {user.LastName}".Trim();
            if (string.IsNullOrEmpty(displayName)) displayName = user.UserName ?? user.Email ?? "User";

            if (this.FindName("UserNameText") is System.Windows.Controls.TextBlock nameText)
                nameText.Text = displayName;
            if (this.FindName("UserAvatarText") is System.Windows.Controls.TextBlock avatarText)
                avatarText.Text = displayName.Length > 0 ? displayName[0].ToString().ToUpper() : "?";
        }

        SetAuthenticationState(true);
        MainNavigationView.SelectedItem = AllItemsNavItem;
        NavigateToPage("AllItems");
        _ = RefreshVaultsNavAsync();
    }

    // Public method to handle logout
    public void HandleLogout()
    {
        _isAuthenticated = false;
        _currentUserId = null;

        // Show login frame and hide main navigation
        SetAuthenticationState(false);

        // Clear navigation selection
        MainNavigationView.SelectedItem = null;

        // Navigate back to login
        NavigateLoginFrame();
    }

    // Missing event handlers for XAML bindings

    private void ProfileButton_Click(object sender, RoutedEventArgs e)
    {
        // Show the dropdown (View Profile / Sign Out) instead of navigating directly.
        if (sender is Button button && button.ContextMenu != null)
        {
            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.IsOpen = true;
        }
    }

    private void ViewProfileMenuItem_Click(object sender, RoutedEventArgs e)
    {
        NavigateToPage("Profile");
    }

    private async void ProfileSettings_Click(object sender, RoutedEventArgs e)
    {
        // This is now handled by the ProfilePage - keeping for compatibility
        NavigateToPage("Profile");
    }

    private async Task<string> GetPasswordItemCount()
    {
        try
        {
            var passwordService = _serviceProvider.GetService<IPasswordItemService>();
            if (passwordService != null)
            {
                var items = await passwordService.GetAllAsync();
                return items.Count().ToString();
            }
        }
        catch (System.Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Suppressed exception", logEx); }
        return "N/A";
    }

    private async void Logout_Click(object sender, RoutedEventArgs e)
    {
        // Show confirmation dialog
        var dialog = new ModernWpf.Controls.ContentDialog
        {
            Title = "Sign Out",
            Content = "Are you sure you want to sign out?",
            PrimaryButtonText = "Sign Out",
            CloseButtonText = "Cancel",
            DefaultButton = ModernWpf.Controls.ContentDialogButton.Close
        };
        ApplyDialogStyle(dialog);

        var result = await Helpers.DialogManager.ShowAsync(dialog);
        if (result == ModernWpf.Controls.ContentDialogResult.Primary)
        {
            // Handle logout
            HandleLogout();
        }
    }

    private async void NavigationItem_RightTapped(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // Handle right-click context menu functionality for navigation items
        if (sender is ModernWpf.Controls.NavigationViewItem navItem)
        {
            var tag = navItem.Tag?.ToString();

            // For now, just select the item if it's not already selected
            if (MainNavigationView.SelectedItem != navItem)
            {
                MainNavigationView.SelectedItem = navItem;
                if (!string.IsNullOrEmpty(tag))
                {
                    NavigateToPage(tag);
                }
            }
        }
    }

    private async void AddTagNavButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Open the TagDialog for creating a new tag
            var tagDialog = new Dialogs.TagDialog(_serviceProvider);

            var result = await Helpers.DialogManager.ShowAsync(tagDialog);
            if (result == ModernWpf.Controls.ContentDialogResult.Primary && tagDialog.Result is not null)
            {
                await ShowInfoMessage("Tag Created", $"Tag '{tagDialog.Result.Name}' has been created successfully.");
                // Refresh categories panel as tags may influence UI
                await RefreshCategoriesAsync();
            }
        }
        catch (Exception ex)
        {
            await ShowErrorMessage("Error", $"Failed to create tag: {ex.Message}");
        }
    }

    // Delete tag navigation handler removed (UI buttons removed). Kept method removed per request.

    private async void AddVaultNavButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var vaultService = _serviceProvider.GetService<IVaultService>();
            var authService = _serviceProvider.GetService<IAuthService>();
            if (vaultService == null) return;

            var (name, description, color, icon) = await Helpers.VaultDialogHelper.ShowAsync("Create New Vault");
            if (name == null) return;

            var userId = authService?.CurrentUser?.Id ?? string.Empty;
            await vaultService.CreateAsync(new Vault
            {
                Name = name,
                Description = description,
                UserId = userId,
                Icon = string.IsNullOrWhiteSpace(icon) ? "🔐" : icon,
                Color = string.IsNullOrWhiteSpace(color) ? "#2563EB" : color
            });

            await RefreshVaultsNavAsync();
            ToastService.Instance.Success($"Vault \"{name}\" created");
        }
        catch (Exception ex)
        {
            await ShowErrorMessage("Error", $"Failed to create vault: {ex.Message}");
        }
    }

    // Populate the VaultsNavPanel with the user's actual vaults; clicking one filters
    // the shared All Items / Passwords screen down to that vault.
    public async Task RefreshVaultsNavAsync()
    {
        try
        {
            // Resolve directly from the root provider — IAuthService is scoped, and a
            // fresh CreateScope() here would get its own AuthService instance with
            // CurrentUser == null, making GetAllAsync() return an empty list even
            // though vaults exist in the database for the actual logged-in user.
            var vaultService = _serviceProvider.GetService<IVaultService>();
            if (vaultService == null) return;

            var vaults = await vaultService.GetAllAsync();

            Dispatcher.Invoke(() =>
            {
                var vaultsPanel = VaultsNavPanel;
                if (vaultsPanel == null) return;

                var previousIds = vaultsPanel.Children.OfType<FrameworkElement>()
                    .Select(c => c.Tag as int?)
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .ToHashSet();

                vaultsPanel.Children.Clear();

                var secondaryBrush = (System.Windows.Media.Brush?)Application.Current.Resources["ModernTextSecondaryBrush"]
                                     ?? System.Windows.Media.Brushes.White;
                var tertiaryBrush = (System.Windows.Media.Brush?)Application.Current.Resources["ModernTextTertiaryBrush"]
                                    ?? System.Windows.Media.Brushes.Gray;

                foreach (var vault in vaults)
                {
                    var rowGrid = new Grid { Margin = new Thickness(0, 2, 0, 2), Height = 38, Tag = vault.Id };
                    rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    // Selection chrome, drawn behind the row content. A rounded fill (inset from the
                    // pane edges so it never looks clipped) plus a short left accent bar — toggled by
                    // HighlightSelectedVault so the selected vault reads like a real menu selection
                    // rather than a flat rectangle bleeding to the edges.
                    var selectionFill = new Border
                    {
                        CornerRadius = new CornerRadius(6),
                        Margin = new Thickness(4, 0, 4, 0),
                        Background = System.Windows.Media.Brushes.Transparent,
                        Tag = "sel",
                        IsHitTestVisible = false,
                        SnapsToDevicePixels = true
                    };
                    Grid.SetColumnSpan(selectionFill, 3);
                    rowGrid.Children.Add(selectionFill);

                    var accentBar = new Border
                    {
                        Width = 3,
                        Height = 18,
                        CornerRadius = new CornerRadius(2),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(1, 0, 0, 0),
                        Background = System.Windows.Media.Brushes.Transparent,
                        Tag = "accent",
                        IsHitTestVisible = false
                    };
                    Grid.SetColumnSpan(accentBar, 3);
                    rowGrid.Children.Add(accentBar);

                    // Vault entry — a real NavigationViewItem so its icon/text land on exactly the
                    // same indentation as the "Vaults" header and the NavigationViewItems above it
                    // (hand-tuned Grid margins kept drifting out of alignment). Clicking it navigates
                    // to the filtered All Items / Passwords screen.
                    var (vaultGlyph, vaultGlyphFont) = Helpers.VaultIconHelper.Resolve(vault.Icon);
                    var iconGlyph = new FontIcon { FontSize = 16, Glyph = vaultGlyph, FontFamily = vaultGlyphFont };
                    // IsHitTestVisible=False: this NavigationViewItem is never added to a real
                    // NavigationView's MenuItems, just dropped in for its icon/content template so
                    // it lines up with the items above — letting it handle its own mouse/selection
                    // logic here could misbehave. A transparent Button on top (below) does the click.
                    var navItem = new NavigationViewItem
                    {
                        Icon = iconGlyph,
                        Focusable = false,
                        IsHitTestVisible = false,
                        Background = System.Windows.Media.Brushes.Transparent,
                        // Unhosted (not a real MainNavigationView child), so it doesn't pick up
                        // NavigationView's own compact item spacing — zero these out or the
                        // library's default template padding makes rows look far apart.
                        Margin = new Thickness(0),
                        Padding = new Thickness(0),
                        MinHeight = 0,
                        VerticalAlignment = VerticalAlignment.Stretch,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Content = new TextBlock
                        {
                            Text = vault.Name,
                            FontSize = 14,
                            FontWeight = FontWeights.SemiBold,
                            Foreground = secondaryBrush,
                            TextTrimming = TextTrimming.CharacterEllipsis,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    };
                    Grid.SetColumn(navItem, 0);
                    rowGrid.Children.Add(navItem);

                    // Transparent click overlay on top of navItem — handles navigation without
                    // relying on NavigationViewItem's own (unhosted) selection/click plumbing.
                    // Must use a flat, chrome-free template: the default WPF Button template paints
                    // a solid light-blue "hot track" highlight on hover that otherwise blots out the
                    // vault name underneath.
                    var navClickOverlay = new Button
                    {
                        Background = System.Windows.Media.Brushes.Transparent,
                        BorderThickness = new Thickness(0),
                        Padding = new Thickness(0),
                        Template = TransparentOverlayButtonTemplate
                    };
                    navClickOverlay.Click += (s, e) =>
                    {
                        // Behave like a proper menu: mark this vault selected (highlight it, clear the
                        // NavigationView selection) and open its filtered item list.
                        _selectedVaultId = vault.Id;
                        MainNavigationView.SelectedItem = null;
                        HighlightSelectedVault();

                        var filterData = new Models.NavigationFilterData(_serviceProvider)
                        {
                            FilterName = vault.Name,
                            FilterVaultId = vault.Id,
                            FilterVaultName = vault.Name
                        };
                        ContentFrame.Navigate(new Views.PasswordItemsPage(), filterData);
                    };
                    Grid.SetColumn(navClickOverlay, 0);
                    rowGrid.Children.Add(navClickOverlay);

                    // Rename (pencil)
                    var renameBtn = new Button
                    {
                        Style = Application.Current.Resources["ModernIconButtonStyle"] as Style,
                        Padding = new Thickness(4),
                        MinWidth = 0, MinHeight = 0,
                        Width = 24, Height = 24,
                        ToolTip = "Rename vault"
                    };
                    renameBtn.Content = new TextBlock
                    {
                        Text = "", FontFamily = new System.Windows.Media.FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
                        FontSize = 12, Foreground = tertiaryBrush
                    };
                    renameBtn.Click += async (s, e) => await RenameVaultAsync(vault);
                    Grid.SetColumn(renameBtn, 1);
                    rowGrid.Children.Add(renameBtn);

                    // Delete (trash) — disabled while the vault still has items
                    var deleteBtn = new Button
                    {
                        Style = Application.Current.Resources["ModernIconButtonStyle"] as Style,
                        Padding = new Thickness(4),
                        MinWidth = 0, MinHeight = 0,
                        Width = 24, Height = 24,
                        IsEnabled = vault.ItemCount == 0,
                        ToolTip = vault.ItemCount == 0
                            ? "Delete vault"
                            : $"Move or delete the {vault.ItemCount} item(s) in this vault first"
                    };
                    deleteBtn.Content = new TextBlock
                    {
                        Text = "", FontFamily = new System.Windows.Media.FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
                        FontSize = 12, Foreground = tertiaryBrush
                    };
                    deleteBtn.Click += async (s, e) =>
                    {
                        try
                        {
                            var confirmDialog = new ModernWpf.Controls.ContentDialog
                            {
                                Title = "Delete Vault",
                                Content = MakeDialogMessage($"Delete \"{vault.Name}\"? This cannot be undone."),
                                PrimaryButtonText = "Delete",
                                CloseButtonText = "Cancel",
                                DefaultButton = ModernWpf.Controls.ContentDialogButton.Close
                            };
                            ApplyDialogStyle(confirmDialog);
                            if (await Helpers.DialogManager.ShowAsync(confirmDialog) != ModernWpf.Controls.ContentDialogResult.Primary)
                                return;

                            var delVaultService = _serviceProvider.GetService<IVaultService>();
                            if (delVaultService == null) return;
                            await delVaultService.DeleteAsync(vault.Id);
                            ToastService.Instance.Success($"Vault \"{vault.Name}\" deleted");
                            await RefreshVaultsNavAsync();
                        }
                        catch (Exception ex)
                        {
                            await ShowErrorMessage("Error", $"Failed to delete vault: {ex.Message}");
                        }
                    };
                    Grid.SetColumn(deleteBtn, 2);
                    rowGrid.Children.Add(deleteBtn);

                    vaultsPanel.Children.Add(rowGrid);

                    if (!previousIds.Contains(vault.Id))
                        AnimateNewVaultRow(rowGrid);
                }

                // Re-apply the selection highlight after a rebuild so it survives refreshes.
                HighlightSelectedVault();
            });
        }
        catch (System.Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Suppressed exception", logEx); }
    }

    // Highlights the selected vault row in the sidebar (like a selected NavigationViewItem) and
    // clears the others, so the vault list reads and behaves like a proper menu.
    private void HighlightSelectedVault()
    {
        if (VaultsNavPanel == null) return;

        var accent = System.Windows.Media.Color.FromRgb(0x25, 0x63, 0xEB);
        // Dark-mode selection fill — a solid dark-gray card (matches NavigationViewItemBackgroundSelected)
        // rather than a translucent accent tint, so the row reads clearly against the dark sidebar
        // and never washes out the label text.
        var selectedFill = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(0x2B, 0x2B, 0x2B));
        selectedFill.Freeze();
        var accentBrush = new System.Windows.Media.SolidColorBrush(accent);
        accentBrush.Freeze();
        var transparent = System.Windows.Media.Brushes.Transparent;
        var brightWhite = System.Windows.Media.Brushes.White;

        var secondaryBrush = (System.Windows.Media.Brush?)Application.Current.Resources["ModernTextSecondaryBrush"]
                             ?? System.Windows.Media.Brushes.Gray;

        foreach (var child in VaultsNavPanel.Children.OfType<Grid>())
        {
            bool isSelected = child.Tag is int id && _selectedVaultId.HasValue && id == _selectedVaultId.Value;

            // The Grid itself stays transparent — the chrome is painted by the inset borders so it
            // gets rounded corners and never bleeds to (or looks cut off at) the pane edges.
            child.Background = transparent;

            foreach (var border in child.Children.OfType<Border>())
            {
                switch (border.Tag as string)
                {
                    case "sel": border.Background = isSelected ? selectedFill : transparent; break;
                    case "accent": border.Background = isSelected ? accentBrush : transparent; break;
                }
            }

            // Brighten the selected vault's label to pure white so it clearly shines through the
            // dark selection card; unselected rows stay at the dimmer secondary color.
            var nav = child.Children.OfType<NavigationViewItem>().FirstOrDefault();
            if (nav?.Content is TextBlock tb)
                tb.Foreground = isSelected ? brightWhite : secondaryBrush;
        }
    }

    // Flashy "just added" entrance: scale/fade in plus a quick accent-color highlight flash
    // on the row background so a newly created vault is unmistakable in the sidebar.
    private static void AnimateNewVaultRow(Grid rowGrid)
    {
        rowGrid.RenderTransformOrigin = new Point(0.5, 0.5);
        var scaleTransform = new System.Windows.Media.ScaleTransform(0.85, 0.85);
        rowGrid.RenderTransform = scaleTransform;
        rowGrid.Opacity = 0;

        var highlight = new Border
        {
            CornerRadius = new CornerRadius(6),
            Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromArgb(90, 37, 99, 235)), // #2563EB flash
            IsHitTestVisible = false
        };
        Grid.SetColumnSpan(highlight, 3);
        rowGrid.Children.Insert(0, highlight);

        var fadeIn = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(280))
        {
            EasingFunction = new System.Windows.Media.Animation.QuadraticEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
        };
        var scaleX = new System.Windows.Media.Animation.DoubleAnimation(0.85, 1.0, TimeSpan.FromMilliseconds(280))
        {
            EasingFunction = new System.Windows.Media.Animation.BackEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut, Amplitude = 0.6 }
        };
        var scaleY = new System.Windows.Media.Animation.DoubleAnimation(0.85, 1.0, TimeSpan.FromMilliseconds(280))
        {
            EasingFunction = new System.Windows.Media.Animation.BackEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut, Amplitude = 0.6 }
        };
        var highlightFade = new System.Windows.Media.Animation.DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(900))
        {
            BeginTime = TimeSpan.FromMilliseconds(150),
            EasingFunction = new System.Windows.Media.Animation.QuadraticEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseIn }
        };

        rowGrid.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        scaleTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, scaleX);
        scaleTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, scaleY);
        highlight.BeginAnimation(UIElement.OpacityProperty, highlightFade);
    }

    private async Task RenameVaultAsync(Vault vault)
    {
        try
        {
            var (name, description, color, icon) = await Helpers.VaultDialogHelper.ShowAsync(
                "Edit Vault", vault.Name, vault.Description, vault.Color, vault.Icon);
            if (name == null) return;

            using var scope = _serviceProvider.CreateScope();
            var vaultService = scope.ServiceProvider.GetService<IVaultService>();
            if (vaultService == null) return;

            vault.Name = name;
            vault.Description = description;
            vault.Color = string.IsNullOrWhiteSpace(color) ? vault.Color : color;
            vault.Icon = string.IsNullOrWhiteSpace(icon) ? vault.Icon : icon;

            await vaultService.UpdateAsync(vault);
            await RefreshVaultsNavAsync();
            ToastService.Instance.Success("Vault updated");
        }
        catch (Exception ex)
        {
            await ShowErrorMessage("Error", $"Failed to rename vault: {ex.Message}");
        }
    }

    private void NewItemButton_Click(object sender, RoutedEventArgs e)
    {
        // If an items page is already showing (e.g. a vault was selected), add the new item there so
        // it inherits the current vault — the page's Add dialog pre-selects that vault in its combo.
        // Otherwise fall back to the All Items page.
        if (ContentFrame?.Content is Views.PasswordItemsPage existingPage)
        {
            existingPage.TriggerAddNew();
            return;
        }

        NavigateToPage("AllItems");
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (ContentFrame.Content is Views.PasswordItemsPage page)
            {
                var addBtn = FindChildControl<Button>(page, "AddButton");
                addBtn?.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
        }));
    }

    // Populate the DynamicCategoriesPanel with categories from the service
    public async Task RefreshCategoriesAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var categoryService = scope.ServiceProvider.GetRequiredService<ICategoryInterface>();

            // Get all favorite categories
            var categories = await categoryService.GetAllAsync();
            var favoriteCategories = categories.Where(c => c.IsFavorite).ToList();

            // Update UI on dispatcher thread
            Dispatcher.Invoke(() =>
            {
                var categoriesPanel = this.FindName("FavoriteCategoriesPanel") as StackPanel;
                if (categoriesPanel == null) return;
                
                categoriesPanel.Children.Clear();
                
                foreach (var category in favoriteCategories)
                {
                    var navItem = new ModernWpf.Controls.NavigationViewItem
                    {
                        Content = category.Name,
                        Tag = $"{category.Name}Category",
                        Style = _navItemStyle
                    };
                    
                    // Set icon if available (using ModernWpf.Controls icon)
                    if (!string.IsNullOrWhiteSpace(category.Icon))
                    {
                        // ModernWPF NavigationViewItem uses IconElement property
                        // For WPF, we'll need to create a proper icon using ModernWpf types
                        // Skipping icon for now as it requires proper WPF icon implementation
                    }
                    
                    // Note: Context menus in WPF use System.Windows.Controls.ContextMenu
                    // not MenuFlyout from WinUI
                    // Context menu implementation would need to be rewritten for WPF
                    
                    categoriesPanel.Children.Add(navItem);
                }
            });
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Warning("Suppressed exception", ex); }
    }

    // Populate the SidebarTagsPanel with tag chips
    public async Task RefreshTagsAsync()
    {
        try
        {
            var tagService = _serviceProvider.GetService<ITagService>();
            if (tagService == null) return;

            var tags = (await tagService.GetAllAsync()).ToList();

            Dispatcher.Invoke(() =>
            {
                var tagsPanel = this.FindName("SidebarTagsPanel") as StackPanel;
                if (tagsPanel == null) return;

                tagsPanel.Children.Clear();

                foreach (var tag in tags)
                {
                    var border = new Border
                    {
                        CornerRadius = new CornerRadius(12),
                        Padding = new Thickness(8, 4, 8, 4),
                        Margin = new Thickness(8, 4, 8, 0),
                        Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 48, 50, 52)),
                    };

                    // Try to use tag color if available
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(tag.Color))
                        {
                            var color = System.Windows.Media.Colors.Transparent;
                            // Expecting hex like #rrggbb
                            if (tag.Color.StartsWith("#"))
                            {
                                var hex = tag.Color.TrimStart('#');
                                if (hex.Length == 6)
                                {
                                    var r = Convert.ToByte(hex.Substring(0, 2), 16);
                                    var g = Convert.ToByte(hex.Substring(2, 2), 16);
                                    var b = Convert.ToByte(hex.Substring(4, 2), 16);
                                    color = System.Windows.Media.Color.FromArgb(255, r, g, b);
                                }
                            }

                            if (color != System.Windows.Media.Colors.Transparent)
                                border.Background = new SolidColorBrush(color);
                        }
                    }
                    catch (System.Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Suppressed exception", logEx); }

                    var txt = new TextBlock
                    {
                        Text = tag.Name,
                        Foreground = new SolidColorBrush(System.Windows.Media.Colors.White),
                        FontSize = 13,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    border.Child = txt;

                    // Make chip clickable to filter by tag
                    var btn = new Button
                    {
                        Content = border,
                        Background = new SolidColorBrush(System.Windows.Media.Colors.Transparent),
                        BorderThickness = new Thickness(0),
                        Padding = new Thickness(0),
                        HorizontalAlignment = HorizontalAlignment.Left
                    };

                    btn.Click += (s, e) =>
                    {
                        try
                        {
                            // Navigate to PasswordItemsPage with a NavigationFilterData that includes the tag filter
                            var filterData = new Models.NavigationFilterData(_serviceProvider)
                            {
                                FilterName = tag.Name,
                                TagName = tag.Name
                            };
                            ContentFrame.Navigate(new Views.PasswordItemsPage(), filterData);
                        }
                        catch (System.Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Suppressed exception", logEx); }
                    };

                    tagsPanel.Children.Add(btn);
                }
            });
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Warning("Suppressed exception", ex); }
    }

    // Delete category navigation handler removed (UI buttons removed). Kept method removed per request.

    private async void AddVaultButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var collectionService = _serviceProvider.GetService<ICollectionService>();
            if (collectionService == null)
            {
                await ShowErrorMessage("Error", "Collection service is not available.");
                return;
            }

            // Create a simple collection creation dialog
            var dialog = new ModernWpf.Controls.ContentDialog
            {
                Title = "Create New Vault",
                PrimaryButtonText = "Create",
                CloseButtonText = "Cancel",
                DefaultButton = ModernWpf.Controls.ContentDialogButton.Primary};

            var nameTextBox = new TextBox
            {
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var descriptionTextBox = new TextBox
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 12, 0, 0)
            };

            var content = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = "Create a new vault to organize your password items." },
                    nameTextBox,
                    descriptionTextBox
                }
            };

            dialog.Content = content;

            var result = await Helpers.DialogManager.ShowAsync(dialog);
            if (result == ModernWpf.Controls.ContentDialogResult.Primary)
            {
                var name = nameTextBox.Text?.Trim();
                if (string.IsNullOrEmpty(name))
                {
                    await ShowErrorMessage("Error", "Vault name is required.");
                    return;
                }

                var newCollection = new Collection
                {
                    Name = name,
                    Description = descriptionTextBox.Text?.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };

                // Set user ID from current authenticated user
                var authService = _serviceProvider.GetService<IAuthService>();
                if (authService?.CurrentUser != null)
                {
                    newCollection.UserId = authService.CurrentUser.Id;
                }

                await collectionService.CreateAsync(newCollection);
                await ShowInfoMessage("Vault Created", $"Vault '{newCollection.Name}' has been created successfully.");

            }
        }
        catch (Exception ex)
        {
            await ShowErrorMessage("Error", $"Failed to create vault: {ex.Message}");
        }
    }

    // Delete vault navigation handler removed (UI buttons removed). Kept method removed per request.

    private async void EditItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Get the tag from the menu item to identify which navigation item to edit
            var menuItem = sender as System.Windows.Controls.MenuItem;
            var tag = menuItem?.Tag?.ToString();

            if (string.IsNullOrEmpty(tag))
            {
                await ShowErrorMessage("Error", "Unable to identify the item to edit.");
                return;
            }

            // Handle editing based on the item type
            switch (tag)
            {
                case "AllItems":
                case "Favorites":
                    await ShowInfoMessage("Edit Item", $"Editing '{tag}' - This would open item management interface.");
                    NavigateToPage("ManageItems");
                    break;

                case "LoginCategory":
                case "CreditCardCategory":
                case "SecureNotesCategory":
                case "PasskeysCategory":
                    await ShowInfoMessage("Edit Category", $"This would open the category editor for '{tag}'.");
                    // Could open category dialog in edit mode here
                    break;

                default:
                    await ShowInfoMessage("Edit Item", $"Editing functionality for '{tag}' would be implemented here.");
                    break;
            }

        }
        catch (Exception ex)
        {
            await ShowErrorMessage("Error", $"Failed to edit item: {ex.Message}");
        }
    }

    private async void DeleteItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Get the tag from the menu item to identify which navigation item to delete
            var menuItem = sender as System.Windows.Controls.MenuItem;
            var tag = menuItem?.Tag?.ToString();

            if (string.IsNullOrEmpty(tag))
            {
                await ShowErrorMessage("Error", "Unable to identify the item to delete.");
                return;
            }

            // Show confirmation dialog
            var confirmDialog = new ModernWpf.Controls.ContentDialog
            {
                Title = "Confirm Deletion",
                Content = $"Are you sure you want to delete '{tag}'? This action cannot be undone.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ModernWpf.Controls.ContentDialogButton.Close};
            ApplyDialogStyle(confirmDialog);

            var result = await Helpers.DialogManager.ShowAsync(confirmDialog);
            if (result == ModernWpf.Controls.ContentDialogResult.Primary)
            {
                // Handle deletion based on the item type
                switch (tag)
                {
                    case "AllItems":
                    case "Favorites":
                        await ShowInfoMessage("Delete Item", $"'{tag}' is a system item and cannot be deleted.");
                        break;

                    case "LoginCategory":
                    case "CreditCardCategory":
                    case "SecureNotesCategory":
                    case "PasskeysCategory":
                        await ShowInfoMessage("Delete Category", $"Category '{tag}' deletion would be handled here.");
                        // Could implement actual category deletion here
                        break;

                    default:
                        await ShowInfoMessage("Delete Item", $"'{tag}' has been marked for deletion.");
                        break;
                }

            }
        }
        catch (Exception ex)
        {
            await ShowErrorMessage("Error", $"Failed to delete item: {ex.Message}");
        }
    }

    private async void DeleteCategoryItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var menuItem = sender as System.Windows.Controls.MenuItem;
            var tag = menuItem?.Tag?.ToString();

            if (string.IsNullOrEmpty(tag))
            {
                await ShowErrorMessage("Error", "Unable to identify the category to delete.");
                return;
            }

            // Check if this is a protected default category
            if (ProtectedCategoryTags.Contains(tag))
            {
                await ShowErrorMessage("Cannot Delete", "This is a default category and cannot be deleted.");
                return;
            }

            var confirmDialog = new ModernWpf.Controls.ContentDialog
            {
                Title = "Delete Category",
                Content = $"Are you sure you want to delete this category? Items in this category will not be deleted, but they will lose their category assignment.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ModernWpf.Controls.ContentDialogButton.Close};
            ApplyDialogStyle(confirmDialog);

            var result = await Helpers.DialogManager.ShowAsync(confirmDialog);
            if (result == ModernWpf.Controls.ContentDialogResult.Primary)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var categoryService = scope.ServiceProvider.GetRequiredService<ICategoryInterface>();
                    
                    // Try to parse as category ID first (for dynamic categories from navigation)
                    if (int.TryParse(tag, out int categoryId))
                    {
                        var category = await categoryService.GetByIdAsync(categoryId);
                        if (category != null)
                        {
                            await categoryService.DeleteAsync(category.Id);
                            await RefreshCategoriesAsync();
                            await ShowInfoMessage("Category Deleted", $"Category '{category.Name}' was successfully deleted.");
                            return;
                        }
                    }
                    
                    // Fallback: Try to match by category name patterns (for legacy hardcoded categories)
                    var categories = await categoryService.GetAllAsync();
                    const string categorySuffix = "Category";
                    var tagWithoutSuffix = tag.EndsWith(categorySuffix, StringComparison.OrdinalIgnoreCase) 
                        ? tag[..^categorySuffix.Length] 
                        : tag;
                    var categoryToDelete = categories.FirstOrDefault(c => 
                        string.Equals(c.Name, tag, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(c.Name, tagWithoutSuffix, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals($"{c.Name}{categorySuffix}", tag, StringComparison.OrdinalIgnoreCase));
                    
                    if (categoryToDelete != null)
                    {
                        await categoryService.DeleteAsync(categoryToDelete.Id);
                        await RefreshCategoriesAsync();
                        await ShowInfoMessage("Category Deleted", $"Category '{categoryToDelete.Name}' was successfully deleted.");
                    }
                    else
                    {
                        await ShowErrorMessage("Error", "Category not found in database.");
                    }
                }
                catch (Exception deleteEx)
                {
                    await ShowErrorMessage("Error", $"Failed to delete category: {deleteEx.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            await ShowErrorMessage("Error", $"Failed to delete category: {ex.Message}");
        }
    }

    // Applies the polished, elevated dialog chrome (rounded corners + drop shadow + dim backdrop)
    // so confirmation/info dialogs don't look flat. Safe no-op if the style isn't found.
    private static void ApplyDialogStyle(ModernWpf.Controls.ContentDialog dialog)
    {
        try
        {
            dialog.Style = dialog.TryFindResource("Modern1PasswordDialogStyle") as Style;
        }
        catch (System.Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Suppressed exception", logEx); }
    }

    // Helper methods for dialogs
    private async Task ShowErrorMessage(string title, string message)
    {
        var errorDialog = new ModernWpf.Controls.ContentDialog
        {
            Title = title,
            Content = MakeDialogMessage(message),
            CloseButtonText = "OK"};
        ApplyDialogStyle(errorDialog);
        await Helpers.DialogManager.ShowAsync(errorDialog);
    }

    private async Task ShowInfoMessage(string title, string message)
    {
        var infoDialog = new ModernWpf.Controls.ContentDialog
        {
            Title = title,
            Content = MakeDialogMessage(message),
            CloseButtonText = "OK"};
        ApplyDialogStyle(infoDialog);
        await Helpers.DialogManager.ShowAsync(infoDialog);
    }

    // Plain strings don't wrap inside ContentDialog's default content presenter, so a long error
    // message stretches the dialog far past its styled MaxWidth. Wrap explicitly instead.
    private static TextBlock MakeDialogMessage(string message) => new()
    {
        Text = message,
        TextWrapping = TextWrapping.Wrap,
        MaxWidth = 480
    };

    private Style? TryGetNavItemStyle()
    {
        try
        {
            // Check current content tree
            if (this.Content is FrameworkElement fe && fe.Resources.Contains("ModernWpfNavigationViewItemStyle"))
            {
                var styleObj2 = fe.Resources["ModernWpfNavigationViewItemStyle"];
                if (styleObj2 is Style s2)
                    return s2;
            }
            // Check application resources
            if (Application.Current.Resources.Contains("ModernWpfNavigationViewItemStyle"))
            {
                var appStyle = Application.Current.Resources["ModernWpfNavigationViewItemStyle"];
                if (appStyle is Style s3)
                    return s3;
            }
        }
        catch (System.Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Suppressed exception", logEx); }
        return null; // fallback - style optional
    }
    
    private async void ConfigureCategoriesButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Navigate to categories page for configuration
            NavigateToPage("Categories");
        }
        catch (Exception ex)
        {
            await ShowErrorMessage("Error", $"Failed to open categories page: {ex.Message}");
        }
    }
    
    private async void ToggleCategoryFavorite_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var menuItem = sender as System.Windows.Controls.MenuItem;
            var categoryIdStr = menuItem?.Tag?.ToString();
            
            if (string.IsNullOrEmpty(categoryIdStr) || !int.TryParse(categoryIdStr, out int categoryId))
            {
                await ShowErrorMessage("Error", "Unable to identify the category.");
                return;
            }
            
            using var scope = _serviceProvider.CreateScope();
            var categoryService = scope.ServiceProvider.GetRequiredService<ICategoryInterface>();
            
            var category = await categoryService.GetByIdAsync(categoryId);
            if (category != null)
            {
                category.IsFavorite = !category.IsFavorite;
                await categoryService.UpdateAsync(category);
                await RefreshCategoriesAsync();
            }
        }
        catch (Exception ex)
        {
            await ShowErrorMessage("Error", $"Failed to update category: {ex.Message}");
        }
    }
}
