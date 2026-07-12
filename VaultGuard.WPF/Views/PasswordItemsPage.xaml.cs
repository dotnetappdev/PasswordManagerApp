using ModernWpf.Controls;
using System.Windows;
using System.Windows.Controls;
using ListView = System.Windows.Controls.ListView;
using Controls = VaultGuard.WPF.Controls;
using Microsoft.Extensions.DependencyInjection;
using VaultGuard.WPF.ViewModels;
using VaultGuard.WPF.Helpers;
using VaultGuard.WPF.Models;
using VaultGuard.Models;
using VaultGuard.Services.Interfaces;
using VaultGuard.DAL.Seed;
using VaultGuard.WPF.Services;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System;

namespace VaultGuard.WPF.Views;

public sealed partial class PasswordItemsPage : System.Windows.Controls.Page
{
    private PasswordItemsViewModel? _viewModel;
    private IServiceProvider? _serviceProvider;
    private int? _pendingVaultIdForNewItems;
    private PasswordItem? _selectedItem;
    private ICategoryInterface? _categoryService;
    private List<Category> _categories = new();
    private ITagService? _tagService;
    private IPasswordItemService? _passwordItemService;
    private ITotpService? _totpService;
    private List<Tag> _allTags = new();
    private System.Windows.Threading.DispatcherTimer? _totpTimer;
    private string? _currentTotpSecret;
    private string? _currentCardCvv;
    private string? _protectedNotesPlain;
    private bool _notesRevealed;
    private readonly Services.IWindowsHelloService _hello = new Services.WindowsHelloService();

    public PasswordItemsPage()
    {
        this.
            
            InitializeComponent();

        // Refresh the live list when data is cleared/changed elsewhere (e.g. Settings → Delete Seed Data).
        Loaded += (_, _) =>
        {
            VaultGuard.WPF.Services.AppEvents.VaultDataChanged -= OnVaultDataChanged;
            VaultGuard.WPF.Services.AppEvents.VaultDataChanged += OnVaultDataChanged;
            InitializeAlphabetIndex();
        };
        Unloaded += (_, _) =>
            VaultGuard.WPF.Services.AppEvents.VaultDataChanged -= OnVaultDataChanged;
    }

    private async void OnVaultDataChanged()
    {
        try
        {
            if (_viewModel != null)
                await _viewModel.RefreshAsync();
            await LoadCategoriesAsync();
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error($"Failed to refresh after vault data change", ex); }
    }

    private T? GetElement<T>(string name) where T : class
    {
        try
        {
            return this.FindName(name) as T;
        }
        catch (Exception ex)
        {
            VaultGuard.Services.Logging.AppLogger.Error($"Failed to resolve named element '{name}'", ex);
            return null;
        }
    }

    // Sets the Text of a named TextBlock if it exists (safe no-op otherwise)
    private void SetText(string elementName, string value)
    {
        var tb = GetElement<TextBlock>(elementName);
        if (tb != null) tb.Text = value;
    }

    public async void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
    {
        // Note: WPF Page doesn't have base.OnNavigatedTo
        // Handle both old service provider parameter and new filter data parameter
        if (e.ExtraData is NavigationFilterData filterData)
        {
            _serviceProvider = filterData.ServiceProvider;
            _categoryService = _serviceProvider.GetRequiredService<ICategoryInterface>();
            _viewModel = new PasswordItemsViewModel(_serviceProvider);
            this.DataContext = _viewModel;

            // Apply the filter from navigation
            ApplyNavigationFilter(filterData);
        }
        else if (e.ExtraData is IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _categoryService = serviceProvider.GetRequiredService<ICategoryInterface>();
            _viewModel = new PasswordItemsViewModel(serviceProvider);
            this.DataContext = _viewModel;
        }

        if (_serviceProvider != null)
        {
            // Seed sample data for this user if they have none yet
            await SampleDataSeeder.SeedSampleDataAsync(_serviceProvider);

            // Load categories from database
            await LoadCategoriesAsync();

            // Load tags and services
            try
            {
                _tagService = _serviceProvider.GetService<ITagService>();
                _passwordItemService = _serviceProvider.GetService<IPasswordItemService>();
                _totpService = _serviceProvider.GetService<ITotpService>();
                if (_tagService != null)
                {
                    _allTags = (await _tagService.GetAllAsync()).ToList();
                }
            }
            catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Warning("Suppressed exception", ex); }

            // Reload view model items after seeding to ensure UI shows newly created items
            try
            {
                if (_viewModel != null)
                {
                    await _viewModel.RefreshAsync();
                }
            }
            catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Warning("Suppressed exception", ex); }
        }
    }

    private void ApplyNavigationFilter(NavigationFilterData filterData)
    {
        if (_viewModel == null) return;

        _pendingVaultIdForNewItems = filterData.FilterVaultId;

        // Update page title. When filtering by a vault, show the vault NAME in the header
        // (never the id) so the user can tell which vault they're viewing at a glance.
        var contentTitle = GetElement<TextBlock>("ContentTitle");
        var contentSubtitle = GetElement<TextBlock>("ContentSubtitle");
        if (filterData.FilterVaultId.HasValue && !string.IsNullOrEmpty(filterData.FilterVaultName))
        {
            if (contentTitle != null) contentTitle.Text = filterData.FilterVaultName;
            if (contentSubtitle != null) contentSubtitle.Text = $"Vault · {filterData.FilterVaultName}";
        }
        else if (!string.IsNullOrEmpty(filterData.FilterName))
        {
            if (contentTitle != null) contentTitle.Text = filterData.FilterName;
            if (contentSubtitle != null) contentSubtitle.Text = $"Showing {filterData.FilterName.ToLower()}";
        }

        // Apply type filter
        if (filterData.FilterType.HasValue)
        {
            _viewModel.FilterType = filterData.FilterType.Value.ToString();
        }

        // Apply favorites filter
        if (filterData.ShowFavorites == true)
        {
            _viewModel.FilterType = "Favorites";
        }

        // Apply archived filter
        if (filterData.ShowArchived == true)
        {
            _viewModel.FilterType = "Archive";
        }

        // Apply deleted filter
        if (filterData.ShowDeleted == true)
        {
            _viewModel.FilterType = "RecentlyDeleted";
        }
        
        // Apply category name filter
        if (!string.IsNullOrEmpty(filterData.FilterCategoryName))
        {
            _viewModel.FilterCategoryName = filterData.FilterCategoryName;
        }

        // Apply vault filter — same All Items / Passwords screen, scoped to one vault
        if (filterData.FilterVaultId.HasValue)
        {
            _viewModel.FilterVaultId = filterData.FilterVaultId;
        }

        // Keep the middle list-column header in step with whatever the toolbar title ended up as.
        if (contentTitle != null) SetListColumnTitle(contentTitle.Text);
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            if (_categoryService != null)
            {
                _categories = await _categoryService.GetAllAsync();
                await PopulateCategoryDropdownAsync();
            }
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Warning("Suppressed exception", ex); }
    }

    // De-duplicates categories by trimmed, case-insensitive name so the dropdown never repeats
    // entries (e.g. when sample data has been seeded more than once).
    private static IEnumerable<Category> DistinctCategories(IEnumerable<Category> source)
        => source
            .Where(c => c != null)
            .GroupBy(c => (c.Name ?? string.Empty).Trim().ToLowerInvariant())
            .Select(g => g.First());

    // The built-in item types, shown as a distinct set of filter entries in the category dropdown
    // (separate from user-defined categories) — the same set surfaced in the nav sidebar.
    private static readonly (ItemType Type, string Label)[] DropdownItemTypes =
    {
        (ItemType.Login, "Logins"),
        (ItemType.CreditCard, "Credit Cards"),
        (ItemType.SecureNote, "Secure Notes"),
        (ItemType.WiFi, "Wi-Fi Networks"),
        (ItemType.Identity, "Identities"),
    };

    private static void AddItemTypeEntries(ComboBox categoryDropdown)
    {
        var header = new ComboBoxItem
        {
            Content = new TextBlock
            {
                Text = "ITEM TYPES",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Opacity = 0.6
            },
            IsEnabled = false
        };
        categoryDropdown.Items.Add(header);

        foreach (var (type, label) in DropdownItemTypes)
        {
            var item = new ComboBoxItem();
            var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
            stackPanel.Children.Add(new TextBlock
            {
                Text = label,
                FontWeight = FontWeights.Medium,
                VerticalAlignment = VerticalAlignment.Center
            });
            item.Content = stackPanel;
            item.Tag = type;
            categoryDropdown.Items.Add(item);
        }
    }

    private async Task PopulateCategoryDropdownAsync()
    {

        var categoryDropdown = GetElement<ComboBox>("CategoryDropdown");
        if (categoryDropdown == null)
        {
            // If the control is missing, skip population to avoid exceptions
            return;
        }
        categoryDropdown.Items.Clear();

        // Add "All Categories" option
        var allCategoriesItem = new ComboBoxItem();
        var allStackPanel = new StackPanel { Orientation = Orientation.Horizontal };
        allStackPanel.Children.Add(new Border
        {
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGray),
            CornerRadius = new CornerRadius(4),
            Width = 14,
            Height = 14,
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center
        });
        allStackPanel.Children.Add(new TextBlock { Text = "All Categories", FontWeight = FontWeights.Medium, VerticalAlignment = VerticalAlignment.Center });
        allCategoriesItem.Content = allStackPanel;
        allCategoriesItem.Tag = "all";
        categoryDropdown.Items.Add(allCategoriesItem);

        AddItemTypeEntries(categoryDropdown);

        // Add categories from database (de-duplicated by name to avoid repeats)
        foreach (var category in DistinctCategories(_categories))
        {
            var item = new ComboBoxItem();
            var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };

            // Add color indicator
            var colorBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray);
            if (!string.IsNullOrEmpty(category.Color))
            {
                try
                {
                    var converter = new System.Windows.Media.BrushConverter();
                    if (converter.ConvertFromString(category.Color) is System.Windows.Media.SolidColorBrush parsed)
                        colorBrush = parsed;
                }
                catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error($"Failed to parse category color", ex); }
            }

            stackPanel.Children.Add(new Border
            {
                Background = colorBrush,
                CornerRadius = new CornerRadius(4),
                Width = 14,
                Height = 14,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            });

            // Add category name and count
            var categoryText = new TextBlock
            {
                Text = category.Name,
                FontWeight = FontWeights.Medium,
                VerticalAlignment = VerticalAlignment.Center
            };
            stackPanel.Children.Add(categoryText);

            // Add password count if available
            if (_categoryService != null)
            {
                try
                {
                    var count = await _categoryService.GetPasswordItemCountAsync(category.Id);
                    if (count > 0)
                    {
                        stackPanel.Children.Add(new TextBlock
                        {
                            Text = $"({count})",
                            FontSize = 12,
                            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray),
                            Margin = new Thickness(4, 0, 0, 0)
                        });
                    }
                }
                catch (System.Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Suppressed exception", logEx); }
            }

            item.Content = stackPanel;
            item.Tag = category;
            categoryDropdown.Items.Add(item);
        }

        // Select first item (All Categories)
        categoryDropdown.SelectedIndex = 0;
    }

    // Sets the live-filter search text from the global top-bar search box (MainWindow). The page
    // no longer has its own search box, so this is the only entry point that drives SearchText.
    public void SetSearchQuery(string query)
    {
        if (_viewModel != null)
        {
            _viewModel.SearchText = query;
        }
    }

    // ── Public entry points for application keyboard shortcuts (wired in MainWindow) ──
    public void TriggerAddNew() => ShowAddPasswordDialog();

    public void CopyPasswordShortcut()
    {
        if (_selectedItem != null) CopyPasswordButton_Click(this, new RoutedEventArgs());
    }

    public void CopyUsernameShortcut()
    {
        if (_selectedItem != null) CopyUsernameButton_Click(this, new RoutedEventArgs());
    }

    public void OpenAndFillShortcut()
    {
        if (_selectedItem != null) OpenAndFillButton_Click(this, new RoutedEventArgs());
    }

    public void EditSelectedShortcut()
    {
        if (_selectedItem != null) EditDetailButton_Click(this, new RoutedEventArgs());
    }

    public async void DeleteSelectedShortcut()
    {
        if (_selectedItem == null || _viewModel == null) return;

        if (await Helpers.ConfirmDialog.ShowDeleteAsync(
                "Delete Item", $"Are you sure you want to delete “{_selectedItem.Title}”? This cannot be undone."))
        {
            if (_serviceProvider != null &&
                !await Helpers.SecurityGateHelper.RequireCodeForActionAsync(
                    _serviceProvider, Helpers.SecurityGateHelper.GateAction.ItemDelete))
            {
                return;
            }

            var title = _selectedItem.Title;
            await _viewModel.DeleteItemAsync(_selectedItem);
            ToastService.Instance.Success($"'{title}' deleted");
        }
    }

    private void UpdateContentTitles(string filterType)
    {
        var contentTitle = GetElement<TextBlock>("ContentTitle");
        var contentSubtitle = GetElement<TextBlock>("ContentSubtitle");
        if (contentTitle == null || contentSubtitle == null) return;

        switch (filterType)
        {
            case "All":
                contentTitle.Text = "All Items";
                contentSubtitle.Text = "Showing all password items";
                break;
            case "Favorites":
                contentTitle.Text = "Favorites";
                contentSubtitle.Text = "Your favorite password items";
                break;
            case "Recent":
                contentTitle.Text = "Recently Used";
                contentSubtitle.Text = "Recently accessed items";
                break;
            case "Login":
                contentTitle.Text = "Logins";
                contentSubtitle.Text = "Login credentials";
                break;
            case "CreditCard":
                contentTitle.Text = "Credit Cards";
                contentSubtitle.Text = "Payment card information";
                break;
            case "SecureNote":
                contentTitle.Text = "Secure Notes";
                contentSubtitle.Text = "Private notes and documents";
                break;
        }

        // Mirror the final toolbar title into the middle list-column header so both stay in sync.
        SetListColumnTitle(contentTitle.Text);
    }

    private void CategoryDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem selectedItem)
        {
            if (_viewModel == null) return;

            // Item type and category are mutually exclusive in this single dropdown — selecting
            // one always resets the other so a stale filter can't silently narrow the results.
            if (selectedItem.Tag is string tag && tag == "all")
            {
                _viewModel.FilterType = "All";
                _viewModel.SelectedCategoryId = null;
                SetListColumnTitle("All Items");
            }
            else if (selectedItem.Tag is ItemType itemType)
            {
                _viewModel.FilterType = itemType.ToString();
                _viewModel.SelectedCategoryId = null;
                SetListColumnTitle(TypeDisplayName(itemType));
            }
            else if (selectedItem.Tag is Category category)
            {
                _viewModel.FilterType = "All";
                _viewModel.SelectedCategoryId = category.Id;
                SetListColumnTitle(category.Name);
            }
        }
    }

    // Keeps the middle-column header in step with the active filter (1Password-style), so the list
    // always names what it's showing instead of a static "All Items".
    private void SetListColumnTitle(string title) => SetText("ListColumnTitle", title);

    private static string TypeDisplayName(ItemType type) => type switch
    {
        ItemType.Login => "Logins",
        ItemType.CreditCard => "Credit Cards",
        ItemType.SecureNote => "Secure Notes",
        ItemType.WiFi => "Wi-Fi Networks",
        ItemType.Passkey => "Passkeys",
        ItemType.Identity => "Identities",
        ItemType.Password => "Passwords",
        _ => type.ToString(),
    };

    private void ItemsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var listView = sender as ListView ?? GetElement<ListView>("ItemsList");
        if (listView != null && listView.SelectedItem is PasswordItem selectedItem)
        {
            _selectedItem = selectedItem;
            ShowItemDetails(selectedItem);
        }
    }

    private void PasswordCardsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Card view removed; keep method stub for compatibility if referenced elsewhere
    }

    private async void RevealPasswordButton_Click(object sender, RoutedEventArgs e)
    {
        var detailPassword = GetElement<Controls.ReadOnlyField>("DetailPassword");
        if (detailPassword == null) return;

        // Mask → reveal: decrypt on demand
        if (detailPassword.Text.StartsWith("•"))
        {
            if (_selectedItem?.LoginItem != null && _serviceProvider != null)
            {
                try
                {
                    var sessionId = await GetSessionIdAsync();
                    if (!string.IsNullOrEmpty(sessionId))
                    {
                        var svc = _serviceProvider.GetService<IPasswordRevealService>();
                        var decrypted = svc != null ? await svc.RevealPasswordAsync(_selectedItem.LoginItem, sessionId) : null;
                        if (!string.IsNullOrEmpty(decrypted))
                        {
                            detailPassword.Text     = decrypted;
                            detailPassword.CopyText = decrypted;
                            return;
                        }
                    }
                }
                catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error($"Failed to reveal decrypted password for detail view", ex); }
            }
            // Fallback: show raw CopyText if decrypt failed
            var raw = detailPassword.CopyText ?? string.Empty;
            if (!string.IsNullOrEmpty(raw)) detailPassword.Text = raw;
        }
        else
        {
            // Reveal → mask
            detailPassword.Text = "••••••••";
        }
    }

    private async void CopyPasswordButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedItem?.LoginItem == null) return;
        try
        {
            var sessionId = await GetSessionIdAsync();
            if (!string.IsNullOrEmpty(sessionId))
            {
                var svc = _serviceProvider?.GetService<IPasswordRevealService>();
                var pwd = svc != null ? await svc.RevealPasswordAsync(_selectedItem.LoginItem, sessionId) : null;
                if (!string.IsNullOrEmpty(pwd))
                {
                    System.Windows.Clipboard.SetText(pwd);
                    ToastService.Instance.Success("Password copied to clipboard");
                    return;
                }
            }
            await ShowTemporaryMessageAsync("No password available — vault may be locked");
        }
        catch (Exception ex)
        {
            await ShowTemporaryMessageAsync($"Failed to copy: {ex.Message}");
        }
    }

    private async Task<string?> GetSessionIdAsync()
    {
        try
        {
            var storage = _serviceProvider?.GetService<ISecureStorageService>();
            return storage != null ? await storage.GetAsync("sessionId") : null;
        }
        catch (System.Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Recovered from a suppressed exception", logEx); return null; }
    }

    /// <summary>
    /// Opens the selected item's website in the default browser and copies the username, then the
    /// password, to the clipboard in sequence so the user can paste them into the sign-in form.
    /// (True in-page autofill requires a browser extension; this is the standard clipboard fallback.)
    /// </summary>
    private async void OpenAndFillButton_Click(object sender, RoutedEventArgs e)
    {
        var item = _selectedItem;
        if (item == null)
        {
            await ShowTemporaryMessageAsync("No item selected");
            return;
        }

        var url = item.Website
                  ?? item.LoginItem?.WebsiteUrl
                  ?? item.LoginItem?.Website;
        var username = item.Username ?? item.LoginItem?.Username;
        var password = item.Password ?? item.LoginItem?.Password ?? item.LoginItem?.EncryptedPassword;

        if (string.IsNullOrWhiteSpace(url))
        {
            await ShowTemporaryMessageAsync("This item has no website to open");
            return;
        }

        var launchUrl = url.Trim();
        if (!launchUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !launchUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            launchUrl = "https://" + launchUrl;
        }

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = launchUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            await ShowTemporaryMessageAsync($"Couldn't open the website: {ex.Message}");
            return;
        }

        // Copy username now, then the password a few seconds later so each is ready to paste in turn.
        if (!string.IsNullOrEmpty(username))
        {
            try { System.Windows.Clipboard.SetText(username); } catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error($"Failed to copy username to clipboard", ex); }
            ToastService.Instance.Info("Username copied — paste it (Ctrl+V), then Tab to the password field.");

            if (!string.IsNullOrEmpty(password))
            {
                await Task.Delay(9000);
                try { System.Windows.Clipboard.SetText(password); } catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error($"Failed to copy password to clipboard", ex); }
                ToastService.Instance.Success("Password copied — paste it into the password field.");
            }
        }
        else if (!string.IsNullOrEmpty(password))
        {
            try { System.Windows.Clipboard.SetText(password); } catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error($"Failed to copy password to clipboard", ex); }
            ToastService.Instance.Success("Password copied — paste it into the password field.");
        }
    }

    private async void OpenWebsiteButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var list = GetElement<ListView>("ItemsList");
            var selected = _selectedItem ?? (list?.SelectedItem as PasswordItem);
            var url = selected?.Website ?? selected?.LoginItem?.WebsiteUrl;
            if (string.IsNullOrEmpty(url))
            {
                await ShowTemporaryMessageAsync("No website URL available");
                return;
            }

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Warning("Suppressed exception", ex); }
    }

    private Task ShowTemporaryMessageAsync(string message)
    {
        // Route through the global toast service
        ToastService.Instance.Info(message);
        return Task.CompletedTask;
    }

    private void SetRevealNotesButtonState(bool revealed)
    {
        var icon = GetElement<TextBlock>("RevealNotesIcon");
        var label = GetElement<TextBlock>("RevealNotesLabel");
        if (icon != null) icon.Text = revealed ? "" : ""; // eye / shield
        if (label != null) label.Text = revealed ? "Hide" : "Reveal";
    }

    /// <summary>
    /// Reveals (or re-hides) a protected note. The first reveal requires a Windows Hello check
    /// when Hello is available on the device.
    /// </summary>
    private async void RevealNotesButton_Click(object sender, RoutedEventArgs e)
    {
        var notesText = GetElement<TextBlock>("DetailNotesText");
        if (notesText == null) return;

        if (_notesRevealed)
        {
            // Hide again.
            _notesRevealed = false;
            notesText.Text = "•••••••  Protected note — click Reveal to view";
            notesText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x6A, 0x6A, 0x6A));
            SetRevealNotesButtonState(false);
            return;
        }

        // Gate the reveal behind Windows Hello when it's set up.
        try
        {
            if (await _hello.IsAvailableAsync())
            {
                var result = await _hello.VerifyAsync("Verify it's you to view this protected note");
                if (result != Services.HelloResult.Success)
                {
                    ToastService.Instance.Warning("Verification required to view this protected note.");
                    return;
                }
            }
        }
        catch { /* If Hello errors, fall through and still allow reveal. */ }

        _notesRevealed = true;
        notesText.Text = string.IsNullOrWhiteSpace(_protectedNotesPlain) ? "No notes" : _protectedNotesPlain;
        notesText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xC8, 0xC8, 0xC8));
        SetRevealNotesButtonState(true);
    }

    private void CopyWebsiteButton_Click(object sender, RoutedEventArgs e)
    {
        var field = GetElement<Controls.ReadOnlyField>("DetailWebsite");
        var url = field?.CopyText;
        if (string.IsNullOrEmpty(url)) url = _selectedItem?.Website ?? _selectedItem?.LoginItem?.WebsiteUrl;
        if (!string.IsNullOrEmpty(url))
        {
            try { System.Windows.Clipboard.SetText(url); ToastService.Instance.Success("Website copied to clipboard"); } catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error($"Failed to copy website URL to clipboard", ex); }
        }
    }

    private void RevealCvvButton_Click(object sender, RoutedEventArgs e)
    {
        var tb = GetElement<TextBlock>("DetailCardCvv");
        if (tb == null || string.IsNullOrEmpty(_currentCardCvv)) return;

        tb.Text = tb.Text.StartsWith("•") ? _currentCardCvv : new string('•', _currentCardCvv.Length);
    }

    private void RevealCardNumberButton_Click(object sender, RoutedEventArgs e)
    {
        var detailUsername = GetElement<Controls.ReadOnlyField>("DetailUsername");
        if (detailUsername == null) return;

        var full = detailUsername.CopyText;
        if (string.IsNullOrEmpty(full)) return;

        detailUsername.Text = detailUsername.Text.Contains("•") ? full : MaskCardNumber(full);
    }

    private static string MaskCardNumber(string number)
    {
        var digits = new string(number.Where(char.IsDigit).ToArray());
        if (digits.Length < 4) return number;
        return $"•••• •••• •••• {digits[^4..]}";
    }

    private void CopyCvvButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentCardCvv)) return;
        try { System.Windows.Clipboard.SetText(_currentCardCvv); ToastService.Instance.Success("CVV copied to clipboard"); } catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error($"Failed to copy CVV to clipboard", ex); }
    }

    private void ShowItemDetails(PasswordItem item)
    {
        if (item == null) return;

        // Safe lookups for all named XAML elements to avoid compile-time errors when XAML g.i.cs is missing
        var detailPanel = GetElement<StackPanel>("DetailPanel");
        var detailTitle = GetElement<TextBlock>("DetailTitle");
        var detailSubtitle = GetElement<TextBlock>("DetailSubtitle");
        var detailItemTitle = GetElement<TextBlock>("DetailItemTitle");
        var detailItemSubtitle = GetElement<TextBlock>("DetailItemSubtitle");
        var detailUsername = GetElement<Controls.ReadOnlyField>("DetailUsername");
        var detailWebsite = GetElement<Controls.ReadOnlyField>("DetailWebsite");
        var detailPassword = GetElement<Controls.ReadOnlyField>("DetailPassword");
        var detailIcon = GetElement<TextBlock>("DetailIcon");
        var detailCategory = GetElement<TextBlock>("DetailCategory");

        if (detailPanel != null) detailPanel.Visibility = Visibility.Visible;
        if (detailTitle != null) detailTitle.Text = "Item Details";
        if (detailSubtitle != null) detailSubtitle.Text = $"Details for {item.Title}";
        if (detailItemTitle != null) detailItemTitle.Text = item.Title;

        // Prefer login-style display. If item has a LoginItem use that. If the item category contains "Identity",
        // treat it like a login and map passkey/secure-note data where possible into username/website fields.
        string username = string.Empty;
        string website = string.Empty;
        string pwd = string.Empty;

        // Field section + label references for type-aware display
        var usernameSection = GetElement<Grid>("DetailUsernameSection");
        var passwordSection = GetElement<Grid>("DetailPasswordSection");
        var websiteSection  = GetElement<Grid>("DetailWebsiteSection");
        var creditCardSection = GetElement<Grid>("DetailCreditCardSection");
        var usernameLabel   = GetElement<TextBlock>("UsernameLabel");
        var passwordLabel   = GetElement<TextBlock>("PasswordLabel");
        var websiteLabel    = GetElement<TextBlock>("WebsiteLabel");

        // Default: all login sections visible, CC section hidden, card-number reveal hidden
        if (usernameSection   != null) usernameSection.Visibility   = Visibility.Visible;
        if (passwordSection   != null) passwordSection.Visibility   = Visibility.Visible;
        if (websiteSection    != null) websiteSection.Visibility    = Visibility.Visible;
        if (creditCardSection != null) creditCardSection.Visibility = Visibility.Collapsed;
        var revealCardNumberBtn = GetElement<Button>("RevealCardNumberButton");
        if (revealCardNumberBtn != null) revealCardNumberBtn.Visibility = Visibility.Collapsed;

        if (item.Type == ItemType.CreditCard)
        {
            // ── Credit card layout (always, even if CreditCardItem failed to load) ──
            var cc = item.CreditCardItem;
            if (usernameLabel != null) usernameLabel.Text = "CARD NUMBER";
            if (passwordLabel != null) passwordLabel.Text = "CARDHOLDER";
            if (websiteSection != null) websiteSection.Visibility = Visibility.Collapsed;
            if (creditCardSection != null) creditCardSection.Visibility = Visibility.Visible;

            var cardNumber = cc?.CardNumber ?? string.Empty;
            username = cardNumber;
            pwd      = string.Empty; // not used for CC; cardholder shown as plain text below

            var revealCardBtn = GetElement<Button>("RevealCardNumberButton");
            if (detailUsername != null)
            {
                detailUsername.Text     = string.IsNullOrEmpty(cardNumber) ? "—" : MaskCardNumber(cardNumber);
                detailUsername.CopyText = cardNumber;
            }
            if (revealCardBtn != null)
                revealCardBtn.Visibility = string.IsNullOrEmpty(cardNumber) ? Visibility.Collapsed : Visibility.Visible;
            // Reuse the "password" field row to show cardholder name in plain text
            if (detailPassword != null)
            {
                detailPassword.Text     = cc?.CardholderName ?? "—";
                detailPassword.CopyText = cc?.CardholderName ?? string.Empty;
            }

            // Extra CC fields
            SetText("DetailCardExpiry", string.IsNullOrWhiteSpace(cc?.ExpiryDate) ? "—" : cc!.ExpiryDate!);
            _currentCardCvv = cc?.CVV;
            SetText("DetailCardCvv",    string.IsNullOrWhiteSpace(cc?.CVV) ? "—" : new string('•', cc!.CVV!.Length));
            SetText("DetailCardType",   cc?.CardType.ToString() ?? "—");
            SetText("DetailCardBank",   string.IsNullOrWhiteSpace(cc?.IssuingBank) ? "—" : cc!.IssuingBank!);

            website = cc?.BankWebsite ?? string.Empty;
        }
        else if (item.WiFiItem != null)
        {
            // ── WiFi layout ─────────────────────────────────────────────────
            if (usernameLabel != null) usernameLabel.Text = "NETWORK NAME";
            if (passwordLabel != null) passwordLabel.Text = "PASSWORD";
            if (websiteLabel  != null) websiteLabel.Text  = "ROUTER URL";

            username = item.WiFiItem.NetworkName ?? string.Empty;
            website  = item.WiFiItem.RouterAdminUrl ?? item.WiFiItem.RouterIP ?? string.Empty;
            pwd      = item.WiFiItem.Password ?? string.Empty;

            if (detailUsername != null) { detailUsername.Text = username; detailUsername.CopyText = username; }
            if (detailWebsite  != null) { detailWebsite.Text  = website;  detailWebsite.CopyText  = website; }
            if (detailPassword != null)
            {
                detailPassword.Text     = string.IsNullOrEmpty(pwd) ? "—" : "••••••••";
                detailPassword.CopyText = pwd;
            }
        }
        else if (item.Type == ItemType.SecureNote)
        {
            // ── Secure note: hide credential fields, notes section carries content ──
            if (usernameSection != null) usernameSection.Visibility = Visibility.Collapsed;
            if (passwordSection != null) passwordSection.Visibility = Visibility.Collapsed;
            if (websiteSection  != null) websiteSection.Visibility  = Visibility.Collapsed;
        }
        else if (item.PasskeyItem != null || item.Type == ItemType.Passkey)
        {
            // ── Passkey: only Website + the passkey credential (masked, copy/reveal) ──
            if (usernameSection != null) usernameSection.Visibility = Visibility.Collapsed;
            if (passwordLabel != null) passwordLabel.Text = "PASSKEY";
            if (websiteLabel  != null) websiteLabel.Text  = "WEBSITE";

            website = item.PasskeyItem?.WebsiteUrl ?? item.PasskeyItem?.Website ?? item.Website ?? string.Empty;
            var credential = item.PasskeyItem?.CredentialId ?? string.Empty;

            if (detailWebsite  != null) { detailWebsite.Text  = website; detailWebsite.CopyText = website; }
            if (detailPassword != null)
            {
                // The reveal button (RevealPasswordButton_Click) falls back to CopyText when there is
                // no LoginItem, so it reveals the passkey credential here.
                detailPassword.Text     = string.IsNullOrEmpty(credential) ? "—" : "••••••••";
                detailPassword.CopyText = credential;
            }
        }
        else
        {
            // ── Login / generic password layout (default) ───────────────────
            if (usernameLabel != null) usernameLabel.Text = "USERNAME";
            if (passwordLabel != null) passwordLabel.Text = "PASSWORD";
            if (websiteLabel  != null) websiteLabel.Text  = "WEBSITE";

            username = item.LoginItem?.Username ?? item.Username ?? string.Empty;
            website  = item.LoginItem?.WebsiteUrl ?? item.LoginItem?.Website ?? item.Website ?? string.Empty;
            pwd      = item.LoginItem?.Password ?? item.Password ?? item.LoginItem?.EncryptedPassword ?? string.Empty;

            if (detailUsername != null) { detailUsername.Text = username; detailUsername.CopyText = username; }
            if (detailWebsite  != null) { detailWebsite.Text  = website;  detailWebsite.CopyText  = website; }
            if (detailPassword != null)
            {
                detailPassword.Text     = string.IsNullOrEmpty(pwd) ? "—" : "••••••••";
                detailPassword.CopyText = pwd;
            }
        }

        if (detailItemSubtitle != null)
            detailItemSubtitle.Text = !string.IsNullOrEmpty(username) ? username
                                      : !string.IsNullOrEmpty(website) ? website
                                      : item.Description ?? "No additional information";

        // Update icon based on type
        if (detailIcon != null) detailIcon.Text = GetTypeIcon(item.Type.ToString());

        // Category badge
        var cat = item.Category;
        if (detailCategory != null)
            detailCategory.Text = cat != null ? $"  {cat.Icon}  {cat.Name}" : "Uncategorized";

        // Notes
        try
        {
            var notesText = GetElement<System.Windows.Controls.TextBlock>("DetailNotesText");
            if (notesText != null)
            {
                var notes = item.Type == ItemType.CreditCard
                            ? (item.CreditCardItem?.Notes ?? item.Description)
                            : (item.Description
                               ?? item.SecureNoteItem?.Content
                               ?? item.LoginItem?.Notes);
                notes ??= string.Empty;

                // Protected notes are masked until the user reveals them (Windows Hello gated).
                _notesRevealed = false;
                _protectedNotesPlain = notes;
                var isProtected = VaultGuard.Services.Utilities.ProtectedItemHelper.IsProtected(item)
                                  && !string.IsNullOrWhiteSpace(notes);

                var revealBtn = GetElement<Button>("RevealNotesButton");
                if (revealBtn != null) revealBtn.Visibility = isProtected ? Visibility.Visible : Visibility.Collapsed;
                SetRevealNotesButtonState(false);

                if (isProtected)
                {
                    notesText.Text = "•••••••  Protected note — click Reveal to view";
                    notesText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x6A, 0x6A, 0x6A));
                }
                else
                {
                    notesText.Text = string.IsNullOrWhiteSpace(notes) ? "No notes" : notes;
                    notesText.Foreground = string.IsNullOrWhiteSpace(notes)
                        ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x4A, 0x4A, 0x4A))
                        : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xC8, 0xC8, 0xC8));
                }
            }

            // Credit cards typically carry richer notes (billing address, PINs, support numbers) —
            // give the notes display more room.
            var notesBorder = GetElement<System.Windows.Controls.Border>("DetailNotesBorder");
            if (notesBorder != null)
                notesBorder.MinHeight = item.Type == ItemType.CreditCard ? 160 : 64;
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error($"Operation failed", ex); }

        // Custom fields — always visible, refresh list
        try { RefreshDetailCustomFields(); }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error($"Operation failed", ex); }

        // Tags
        try
        {
            var detailTags = GetElement<ItemsControl>("DetailTagsPanel");
            if (detailTags != null) detailTags.ItemsSource = item.Tags ?? new List<Tag>();

            var editTagsPanel = GetElement<ItemsControl>("EditTagsPanel");
            if (editTagsPanel != null) editTagsPanel.ItemsSource = item.Tags ?? new List<Tag>();
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error($"Operation failed", ex); }

        // Live verification code (TOTP)
        SetupTotp(item);

        // Reflect favorite state on the detail star + quick action
        UpdateFavoriteVisuals();

        // Recently Deleted: swap the normal Delete action for Restore / Delete Permanently.
        UpdateTrashActions(item);

        // Wire drag handles on all static field rows
        try { WireFieldDragHandles(); } catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error($"Failed to wire field drag handles", ex); }
    }

    /// <summary>Shows Restore / Delete-Permanently for soft-deleted items and hides them otherwise.</summary>
    private void UpdateTrashActions(PasswordItem item)
    {
        bool deleted = item.IsDeleted;
        var restore = GetElement<Button>("RestoreQuickActionButton");
        var purge = GetElement<Button>("PermanentDeleteQuickActionButton");
        var delete = GetElement<Button>("DeleteQuickActionButton");
        if (restore != null) restore.Visibility = deleted ? Visibility.Visible : Visibility.Collapsed;
        if (purge != null) purge.Visibility = deleted ? Visibility.Visible : Visibility.Collapsed;
        if (delete != null) delete.Visibility = deleted ? Visibility.Collapsed : Visibility.Visible;

        // One-click restore right in the detail header — no need to scroll to Quick Actions.
        var restoreHeader = GetElement<Button>("RestoreDetailButton");
        var editHeader = GetElement<Button>("EditDetailButton");
        if (restoreHeader != null) restoreHeader.Visibility = deleted ? Visibility.Visible : Visibility.Collapsed;
        if (editHeader != null) editHeader.Visibility = deleted ? Visibility.Collapsed : Visibility.Visible;
    }

    private async void RestoreItemButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedItem == null || _passwordItemService == null) return;
        try
        {
            if (await _passwordItemService.RestoreAsync(_selectedItem.Id))
            {
                ToastService.Instance.Success($"'{_selectedItem.Title}' restored");
                // It's no longer deleted — drop it from the Recently Deleted list and clear the detail.
                if (_viewModel != null) await _viewModel.RefreshAsync();
                _selectedItem = null;
                var detail = GetElement<StackPanel>("DetailPanel");
                if (detail != null) detail.Visibility = Visibility.Collapsed;
            }
            else
            {
                ToastService.Instance.Error("Couldn't restore this item.");
            }
        }
        catch (Exception ex)
        {
            ToastService.Instance.Error($"Restore failed: {ex.Message}");
        }
    }

    private async void PermanentlyDeleteItemButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedItem == null || _passwordItemService == null) return;

        if (!await Helpers.ConfirmDialog.ShowDeleteAsync(
                "Delete Permanently",
                $"“{_selectedItem.Title}” will be permanently deleted. This cannot be undone."))
            return;

        if (_serviceProvider != null &&
            !await Helpers.SecurityGateHelper.RequireCodeForActionAsync(
                _serviceProvider, Helpers.SecurityGateHelper.GateAction.ItemDelete))
        {
            return;
        }

        try
        {
            if (await _passwordItemService.PermanentlyDeleteAsync(_selectedItem.Id))
            {
                ToastService.Instance.Success($"'{_selectedItem.Title}' permanently deleted");
                if (_viewModel != null) await _viewModel.RefreshAsync();
                _selectedItem = null;
                var detail = GetElement<StackPanel>("DetailPanel");
                if (detail != null) detail.Visibility = Visibility.Collapsed;
            }
            else
            {
                ToastService.Instance.Error("Couldn't permanently delete this item.");
            }
        }
        catch (Exception ex)
        {
            ToastService.Instance.Error($"Delete failed: {ex.Message}");
        }
    }

    // ── Favorites ───────────────────────────────────────────────────────────────
    // One-click toggle from the list row star, the detail header star, or the quick action —
    // works for every item type. Flips the persisted flag and updates the filled/outline icon.
    private const string StarOutline = "";
    private const string StarFilled  = "";

    private async void FavoriteToggleButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is PasswordItem item)
            await ToggleFavoriteAsync(item);
    }

    private async void FavoriteDetailButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedItem != null)
            await ToggleFavoriteAsync(_selectedItem);
    }

    private async Task ToggleFavoriteAsync(PasswordItem item)
    {
        if (_passwordItemService == null) return;
        try
        {
            // Capture the desired end state up front. The WPF data service returns EF-tracked
            // entities, so ToggleFavoriteAsync() flips this very instance under the hood — if we
            // then blind-flipped it again here it would toggle twice and net back to the original
            // (DB favorited, UI not), which is exactly why the star never stayed gold and the item
            // never moved into the Favorites view. Assign the captured target instead of flipping,
            // so the result is correct whether or not the entity happens to be tracked.
            bool target = !item.IsFavorite;

            var ok = await _passwordItemService.ToggleFavoriteAsync(item.Id);
            if (!ok)
            {
                ToastService.Instance.Error("Couldn't update favorite — please try again.");
                return;
            }

            // PasswordItem raises INotifyPropertyChanged, so the list-row star updates itself —
            // no ListView.Items.Refresh() needed (that was also dropping the selection).
            item.IsFavorite = target;

            // Update the detail header + quick-action icons whenever the toggled item is the one
            // currently shown (covers the row star, the header star, and the quick action).
            if (_selectedItem != null && _selectedItem.Id == item.Id) UpdateFavoriteVisuals();

            // Re-run the active filter so the item appears/disappears from the Favorites view
            // immediately instead of only on the next manual filter change. ApplyFilters()
            // clears/re-adds the bound collection, which drops the ListView selection, so
            // restore it afterwards if the item is still in view.
            if (_viewModel != null)
            {
                await _viewModel.ApplyFilters();
                var list = GetElement<ListView>("ItemsList");
                if (list != null && _selectedItem != null && _viewModel.PasswordItems.Contains(_selectedItem))
                {
                    list.SelectedItem = _selectedItem;
                }
            }

            ToastService.Instance.Success(item.IsFavorite
                ? $"'{item.Title}' added to favorites"
                : $"'{item.Title}' removed from favorites");
        }
        catch (Exception ex)
        {
            ToastService.Instance.Error($"Couldn't update favorite: {ex.Message}");
        }
    }

    private void UpdateFavoriteVisuals()
    {
        bool fav = _selectedItem?.IsFavorite == true;
        // FavoriteStarFill (E735) when on, FavoriteStar outline (E734) when off — built from
        // code points so the glyphs are unambiguous in source.
        string filled = char.ConvertFromUtf32(0xE735);
        string outline = char.ConvertFromUtf32(0xE734);
        var gold = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xF5, 0xB3, 0x01));
        var dim = (System.Windows.Media.Brush?)Application.Current.Resources["ModernTextTertiaryBrush"]
                  ?? System.Windows.Media.Brushes.Gray;

        var icon = GetElement<TextBlock>("FavoriteDetailIcon");
        if (icon != null) { icon.Text = fav ? filled : outline; icon.Foreground = fav ? gold : dim; }

        var qaIcon = GetElement<TextBlock>("FavoriteQuickActionIcon");
        if (qaIcon != null) { qaIcon.Text = fav ? filled : outline; qaIcon.Foreground = fav ? gold : dim; }

        var qaLabel = GetElement<TextBlock>("FavoriteQuickActionLabel");
        if (qaLabel != null) qaLabel.Text = fav ? "Remove from Favorites" : "Add to Favorites";
    }

    // ── A-Z jump index ───────────────────────────────────────────────────────────
    // Phone-book style: click a letter, or press-drag down the column, to jump straight to the
    // first item whose title starts with it. Letters are generated once; lookups happen against
    // the view model's already-sorted/filtered list, so the result always matches what's on screen.
    private static readonly string[] AlphabetLetters =
    {
        "#", "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q",
        "R", "S", "T", "U", "V", "W", "X", "Y", "Z"
    };

    private static readonly Brush AlphabetIdleBrush =
        (Brush?)Application.Current.Resources["ModernTextTertiaryBrush"] ?? Brushes.Gray;
    private static readonly Brush AlphabetActiveBrush =
        new SolidColorBrush(Color.FromRgb(0xF5, 0xB3, 0x01)); // gold, matches the favourite star

    private void InitializeAlphabetIndex()
    {
        var panel = GetElement<Panel>("AlphabetIndexPanel");
        if (panel == null || panel.Children.Count > 0) return;

        foreach (var letter in AlphabetLetters)
        {
            // Each letter sits in its own equal-height UniformGrid cell so they spread down the
            // full list height — bigger, easy-to-hit targets you can click or drag like a Filofax tab.
            panel.Children.Add(new TextBlock
            {
                Text = letter,
                Tag = letter,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = AlphabetIdleBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center
            });
        }
    }

    private void AlphabetIndex_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        JumpToLetterAtPosition(e.GetPosition(GetElement<Panel>("AlphabetIndexPanel")));
    }

    private void AlphabetIndex_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        JumpToLetterAtPosition(e.GetPosition(GetElement<Panel>("AlphabetIndexPanel")));
    }

    private void JumpToLetterAtPosition(Point position)
    {
        var panel = GetElement<Panel>("AlphabetIndexPanel");
        if (panel == null || panel.Children.Count == 0) return;

        double itemHeight = panel.ActualHeight / panel.Children.Count;
        if (itemHeight <= 0) return;

        int index = (int)(position.Y / itemHeight);
        index = Math.Max(0, Math.Min(panel.Children.Count - 1, index));

        // Highlight the letter currently under the cursor so the scrub position is obvious.
        for (int i = 0; i < panel.Children.Count; i++)
        {
            if (panel.Children[i] is TextBlock t)
                t.Foreground = i == index ? AlphabetActiveBrush : AlphabetIdleBrush;
        }

        if (panel.Children[index] is TextBlock tb && tb.Tag is string letter)
        {
            ScrollToLetter(letter);
        }
    }

    private void ScrollToLetter(string letter)
    {
        if (_viewModel == null) return;

        var match = letter == "#"
            ? _viewModel.PasswordItems.FirstOrDefault(i => !string.IsNullOrEmpty(i.Title) && !char.IsLetter(i.Title[0]))
            : _viewModel.PasswordItems.FirstOrDefault(i =>
                !string.IsNullOrEmpty(i.Title) && char.ToUpperInvariant(i.Title[0]) == letter[0]);

        if (match == null) return;

        var list = GetElement<ListView>("ItemsList");
        list?.ScrollIntoView(match);
    }

    // ── Static field drag-drop ─────────────────────────────────────────────────
    private FrameworkElement? _fieldDragSource;

    private void WireFieldDragHandles()
    {
        var detailFields = GetElement<StackPanel>("DetailFields");
        if (detailFields == null) return;

        foreach (UIElement child in detailFields.Children)
        {
            if (child is not Grid row || row.Tag as string != "StaticFieldRow") continue;

            // Find the drag handle TextBlock (Tag="DragHandle") in col 0
            TextBlock? handle = null;
            foreach (UIElement rowChild in row.Children)
            {
                if (rowChild is TextBlock tb && tb.Tag as string == "DragHandle")
                { handle = tb; break; }
            }
            if (handle == null) continue;

            // Capture for closures
            var capturedRow = row;
            handle.PreviewMouseLeftButtonDown += (_, _) => _fieldDragSource = capturedRow;
            handle.PreviewMouseMove += (_, e) =>
            {
                if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed && _fieldDragSource == capturedRow)
                    DragDrop.DoDragDrop(handle, capturedRow, DragDropEffects.Move);
            };

            row.DragEnter += (_, e) =>
            {
                if (e.Data.GetDataPresent(typeof(Grid)))
                    capturedRow.Background = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromArgb(30, 0x25, 0x63, 0xEB));
            };
            row.DragLeave += (_, _) => capturedRow.Background = null;
            row.Drop += (_, e) =>
            {
                capturedRow.Background = null;
                if (e.Data.GetData(typeof(Grid)) is not Grid dragged) return;
                if (dragged == capturedRow) return;

                int targetIdx = detailFields.Children.IndexOf(capturedRow);
                int sourceIdx = detailFields.Children.IndexOf(dragged);
                if (targetIdx < 0 || sourceIdx < 0 || targetIdx == sourceIdx) return;

                detailFields.Children.Remove(dragged);
                int insertAt = detailFields.Children.IndexOf(capturedRow);
                if (insertAt < 0) insertAt = detailFields.Children.Count;
                else if (sourceIdx > targetIdx) insertAt = targetIdx;
                detailFields.Children.Insert(insertAt, dragged);
            };
        }
    }

    /// <summary>
    /// Shows a live, auto-refreshing TOTP verification code for items that carry an authenticator
    /// secret; hides the section (and stops the timer) for everything else.
    /// </summary>
    private async void SetupTotp(PasswordItem item)
    {
        var section = GetElement<Grid>("DetailTotpSection");

        // 1) Manually-entered secret lives in the reserved "TOTP Secret" custom field.
        _currentTotpSecret = VaultGuard.Services.Utilities.TotpHelper.GetSecret(item);

        // 2) Fallback: secrets imported from other managers (1Password, Bitwarden, …) are stored
        //    encrypted on the login item itself. Decrypt them on demand so their codes show too.
        if (string.IsNullOrEmpty(_currentTotpSecret)
            && item.LoginItem != null
            && !string.IsNullOrEmpty(item.LoginItem.EncryptedTotpSecret)
            && _serviceProvider != null)
        {
            try
            {
                var sessionId = await GetSessionIdAsync();
                if (!string.IsNullOrEmpty(sessionId))
                {
                    var reveal = _serviceProvider.GetService<IPasswordRevealService>();
                    if (reveal != null)
                    {
                        var imported = await reveal.RevealTotpSecretAsync(item.LoginItem, sessionId);
                        // Guard against a fast item switch resolving onto the wrong item.
                        if (_selectedItem?.Id == item.Id && !string.IsNullOrWhiteSpace(imported))
                            _currentTotpSecret = imported;
                    }
                }
            }
            catch { /* best-effort — just won't show a code */ }
        }

        bool usable = !string.IsNullOrEmpty(_currentTotpSecret)
                      && _totpService != null
                      && _totpService.TryParse(_currentTotpSecret, out _, out _, out _);

        if (!usable)
        {
            if (section != null) section.Visibility = Visibility.Collapsed;
            _totpTimer?.Stop();
            return;
        }

        if (section != null) section.Visibility = Visibility.Visible;

        _totpTimer ??= new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _totpTimer.Tick -= TotpTimer_Tick;
        _totpTimer.Tick += TotpTimer_Tick;

        UpdateTotpDisplay();
        _totpTimer.Start();
    }

    private void TotpTimer_Tick(object? sender, EventArgs e) => UpdateTotpDisplay();

    private void UpdateTotpDisplay()
    {
        if (_totpService == null || string.IsNullOrEmpty(_currentTotpSecret)) return;

        var code = _totpService.GenerateCode(_currentTotpSecret);
        var remaining = _totpService.GetRemainingSeconds(30);

        var codeTb = GetElement<TextBlock>("DetailTotpCode");
        var cdTb = GetElement<TextBlock>("DetailTotpCountdown");

        if (codeTb != null)
            codeTb.Text = string.IsNullOrEmpty(code)
                ? "——————"
                : (code.Length == 6 ? code.Insert(3, " ") : code);
        if (cdTb != null)
            cdTb.Text = remaining.ToString();
    }

    private void CopyTotpButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var code = _totpService?.GenerateCode(_currentTotpSecret);
            if (!string.IsNullOrEmpty(code))
            {
                System.Windows.Clipboard.SetText(code);
                ToastService.Instance.Success("Verification code copied to clipboard");
            }
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error($"Operation failed", ex); }
    }

    private string GetTypeIcon(string type)
    {
        // Use the new IconHelper to get emoji for type
        if (Enum.TryParse<ItemType>(type, true, out var itemType))
        {
            return IconHelper.GetEmojiForType(itemType);
        }
        return IconHelper.GetEmojiForIcon("folder");
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            await _viewModel.RefreshAsync();
        }
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        ShowAddPasswordDialog();
    }

    private async void SeedDataButton_Click(object sender, RoutedEventArgs e)
    {
        if (_serviceProvider == null) return;

        var dialog = new ModernWpf.Controls.ContentDialog
        {
            Title = "Seed Demo Data",
            Content = "This will add ~100 demo password items (logins, credit cards, secure notes, WiFi) to your vault. Existing demo items will be replaced. Continue?",
            PrimaryButtonText = "Seed Data",
            CloseButtonText = "Cancel",
            DefaultButton = ModernWpf.Controls.ContentDialogButton.Primary
        };
        ConfigureDialogForCentering(dialog);
        var result = await dialog.ShowAsync();
        if (result != ModernWpf.Controls.ContentDialogResult.Primary) return;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var sp = scope.ServiceProvider;
            var db = sp.GetRequiredService<VaultGuard.DAL.VaultGuardDbContext>();
            var authService = sp.GetService<IAuthService>();
            var userId = authService?.CurrentUser?.Id;

            if (string.IsNullOrEmpty(userId))
            {
                var firstUser = await db.Users.FirstOrDefaultAsync();
                userId = firstUser?.Id;
            }

            if (string.IsNullOrEmpty(userId))
            {
                await ShowTemporaryMessageAsync("No user found — please sign in first");
                return;
            }

            // Force re-seed: clear existing items and re-seed
            TestDataSeeder.SeedCollections(db, userId);
            TestDataSeeder.SeedCategories(db, userId);
            TestDataSeeder.SeedTags(db, userId);
            TestDataSeeder.ForceSeedPasswordItems(db, userId);

            if (_viewModel != null)
                await _viewModel.RefreshAsync();

            await ShowTemporaryMessageAsync($"Seeded demo data successfully");
        }
        catch (Exception ex)
        {
            var errorDialog = new ModernWpf.Controls.ContentDialog
            {
                Title = "Seed Error",
                Content = $"Failed to seed data: {ex.Message}",
                CloseButtonText = "OK"
            };
            ConfigureDialogForCentering(errorDialog);
            await errorDialog.ShowAsync();
        }
    }

    private async void ShowAddPasswordDialog()
    {
        try
        {
            // Check if service provider is available
            if (_serviceProvider == null)
            {
                var errorDialog = new ModernWpf.Controls.ContentDialog
                {
                    Title = "Something went wrong",
                    Content = "The app isn't fully loaded yet. Please close this page, reopen it, and try again.",
                    CloseButtonText = "OK"
                };
                ConfigureDialogForCentering(errorDialog);
                await errorDialog.ShowAsync();
                return;
            }

            // First show the item type selection dialog (1Password style)
            var typeSelectionDialog = new Dialogs.ItemTypeSelectionDialog();
            ConfigureDialogForCentering(typeSelectionDialog);

            var typeResult = await typeSelectionDialog.ShowAsync();
            if (typeResult == ModernWpf.Controls.ContentDialogResult.Primary || typeSelectionDialog.SelectedItemType != null)
            {
                // Then show the main add dialog with the selected type pre-filled
                var dialog = new Dialogs.AddPasswordDialog(_serviceProvider, targetVaultId: _pendingVaultIdForNewItems);
                ConfigureDialogForCentering(dialog);

                // Pre-select the item type if one was chosen
                if (typeSelectionDialog.SelectedItemType.HasValue)
                {
                    // Pass the selected type to the dialog
                    dialog.SetInitialItemType(typeSelectionDialog.SelectedItemType.Value, typeSelectionDialog.SelectedCategoryName);
                }

                await dialog.ShowAsync();
                if (dialog.Result != null)
                {
                    if (_viewModel != null)
                        await _viewModel.RefreshAsync();
                    ToastService.Instance.Success("Item saved successfully");
                }
            }
        }
        catch (Exception ex)
        {
            var errorDialog = new ModernWpf.Controls.ContentDialog
            {
                Title = "Error",
                Content = $"Error adding password: {ex.Message}",
                CloseButtonText = "OK"
            };
            ConfigureDialogForCentering(errorDialog);
            await errorDialog.ShowAsync();
        }
    }

    private async void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
    {
        // Works from the row context-menu (MenuItem with a bound item) AND from the
        // detail-pane "Delete" quick action (a Button), which targets the selected item.
        PasswordItem? item = sender switch
        {
            System.Windows.Controls.MenuItem mi when mi.DataContext is PasswordItem di => di,
            Button => _selectedItem,
            _ => null
        };

        if (item == null || _viewModel == null) return;

        if (await Helpers.ConfirmDialog.ShowDeleteAsync(
                "Move to Recently Deleted",
                $"“{item.Title}” will be moved to Recently Deleted, like the Windows Recycle Bin. " +
                "It stays there so you can restore it, and is removed permanently after the retention period " +
                "(or when you empty Recently Deleted)."))
        {
            if (_serviceProvider != null &&
                !await Helpers.SecurityGateHelper.RequireCodeForActionAsync(
                    _serviceProvider, Helpers.SecurityGateHelper.GateAction.ItemDelete))
            {
                return;
            }

            await _viewModel.DeleteItemAsync(item);
            ToastService.Instance.Success($"'{item.Title}' deleted");

            // Collapse the detail pane if we just deleted the item it was showing.
            if (_selectedItem?.Id == item.Id)
            {
                _selectedItem = null;
                var detail = GetElement<StackPanel>("DetailPanel");
                if (detail != null) detail.Visibility = Visibility.Collapsed;
            }
        }
    }

    private async void EditMenuItem_Click(object sender, RoutedEventArgs e)
    {
        PasswordItem? item = null;

        // Support both System.Windows.Controls.MenuItem (context menu) and Button (detail header Edit button)
        if (sender is System.Windows.Controls.MenuItem menuItem && menuItem.DataContext is PasswordItem mi)
        {
            item = mi;
        }
        else if (sender is Button btn)
        {
            // Use the currently selected item in the list/detail view
            item = _selectedItem;
        }

        if (item == null)
        {
            // Nothing to edit
            return;
        }

        try
        {
            if (_serviceProvider == null)
            {
                var errorDialog = new ModernWpf.Controls.ContentDialog
                {
                    Title = "Something went wrong",
                    Content = "The app isn't fully loaded yet. Please close this page, reopen it, and try again.",
                    CloseButtonText = "OK"};
                await errorDialog.ShowAsync();
                return;
            }

            // Open the edit dialog (reuse AddPasswordDialog in edit mode)
            var dialog = new Dialogs.AddPasswordDialog(_serviceProvider, item);
            ConfigureDialogForCentering(dialog);

            await dialog.ShowAsync();
            if (dialog.Result != null)
            {
                if (_viewModel != null)
                {
                    await _viewModel.RefreshAsync();
                }

                // Re-load item details from fresh data source if possible
                if (_viewModel != null)
                {
                    var fresh = _viewModel.PasswordItems?.FirstOrDefault(pi => pi.Id == item.Id);
                    if (fresh != null)
                    {
                        _selectedItem = fresh;
                        ShowItemDetails(fresh);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            var errorDialog = new ModernWpf.Controls.ContentDialog
            {
                Title = "Error",
                Content = $"Error editing password: {ex.Message}",
                CloseButtonText = "OK"};
            await errorDialog.ShowAsync();
        }
    }

    // Tag suggest box handlers
    private void TagSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        try
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var query = sender.Text ?? string.Empty;
                var suggestions = _allTags?
                    .Where(t => t.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                    .Select(t => t.Name)
                    .Distinct()
                    .ToList() ?? new List<string>();

                sender.ItemsSource = suggestions;
            }
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error($"Operation failed", ex); }
    }

    private async void TagSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        try
        {
            var chosen = args.SelectedItem as string;
            if (string.IsNullOrEmpty(chosen)) return;
            await AddTagByNameToSelectedItemAsync(chosen);
            sender.Text = string.Empty;
            sender.ItemsSource = null;
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error($"Operation failed", ex); }
    }

    private async void TagSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        try
        {
            var text = args.QueryText?.Trim();
            if (string.IsNullOrEmpty(text)) return;
            await AddTagByNameToSelectedItemAsync(text!);
            sender.Text = string.Empty;
            sender.ItemsSource = null;
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error($"Operation failed", ex); }
    }

    private async Task AddTagByNameToSelectedItemAsync(string tagName)
    {
        if (_selectedItem == null || _serviceProvider == null) return;

        try
        {
            // Find existing tag or create
            Tag? tag = null;
            if (_tagService != null)
            {
                tag = await _tagService.GetByNameAsync(tagName);
            }

            if (tag == null)
            {
                // create a new tag
                if (_tagService != null)
                {
                    tag = await _tagService.CreateAsync(new Tag { Name = tagName, UserId = _selectedItem.UserId });
                    // update local cache
                    _allTags.Add(tag);
                }
                else
                {
                    // fallback: create in-memory tag object
                    tag = new Tag { Id = 0, Name = tagName };
                }
            }

            // Add to item if not present
            if (!_selectedItem.Tags.Any(t => string.Equals(t.Name, tag.Name, StringComparison.OrdinalIgnoreCase)))
            {
                _selectedItem.Tags.Add(tag);
                if (_passwordItemService != null)
                {
                    await _passwordItemService.UpdateAsync(_selectedItem);
                }

                // Refresh UI panels
                var editTagsPanel = GetElement<ItemsControl>("EditTagsPanel");
                var detailTags = GetElement<ItemsControl>("DetailTagsPanel");
                if (editTagsPanel != null) editTagsPanel.ItemsSource = null; // force refresh
                if (detailTags != null) detailTags.ItemsSource = null;
                if (editTagsPanel != null) editTagsPanel.ItemsSource = _selectedItem.Tags;
                if (detailTags != null) detailTags.ItemsSource = _selectedItem.Tags;
            }
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Warning("Suppressed exception", ex); }
    }

    private async void RemoveTagButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is Button btn && _selectedItem != null)
            {
                var tagIdObj = btn.Tag;
                if (tagIdObj == null) return;

                int tagId = 0;
                if (tagIdObj is int i) tagId = i;
                else if (!int.TryParse(tagIdObj.ToString(), out tagId)) return;

                var existing = _selectedItem.Tags.FirstOrDefault(t => t.Id == tagId || string.Equals(t.Id.ToString(), tagIdObj.ToString(), StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    _selectedItem.Tags.Remove(existing);
                    if (_passwordItemService != null)
                    {
                        await _passwordItemService.UpdateAsync(_selectedItem);
                    }

                    var editTagsPanel = GetElement<ItemsControl>("EditTagsPanel");
                    var detailTags = GetElement<ItemsControl>("DetailTagsPanel");
                    if (editTagsPanel != null) editTagsPanel.ItemsSource = _selectedItem.Tags;
                    if (detailTags != null) detailTags.ItemsSource = _selectedItem.Tags;
                }
            }
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Warning("Suppressed exception", ex); }
    }

    private void AddCategoryButton_Click(object sender, RoutedEventArgs e)
    {
        // Navigate to Categories page via main window
        try
        {
            var mainWindow = GetMainWindow();
            if (mainWindow != null)
            {
                mainWindow.NavigateToPage("Categories");
            }
            else
            {
                // Fallback: navigate to Categories via content frame
                var win = (App.Current as App)?.MainWindow;
                win?.NavigateToPage("Categories");
            }
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Warning("Suppressed exception", ex); }
    }

    private MainWindow? GetMainWindow()
    {
        // Use the MainWindow property exposed in App
        return (App.Current as App)?.MainWindow;
    }

    /// <summary>
    /// Helper method to properly configure dialog for centering
    /// </summary>
    private void ConfigureDialogForCentering(ModernWpf.Controls.ContentDialog dialog)
    {
        try
        {
            // Try to get the main window for proper centering
            var mainWindow = GetMainWindow();

            // Ensure the dialog uses the proper style for centering if it doesn't have one already
            if (dialog.Style == null)
            {
                // Apply the Modern1PasswordDialogStyle from resources
                if (Application.Current.Resources.Contains("Modern1PasswordDialogStyle"))
                {
                    dialog.Style = Application.Current.Resources["Modern1PasswordDialogStyle"] as Style;
                }
            }
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Warning("Suppressed exception", ex); }
    }

    private void ItemsList_DoubleTapped(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var list = GetElement<ListView>("ItemsList");
        if (list != null && list.SelectedItem is PasswordItem item)
        {
            // Open password details view
            ShowPasswordDetails(item);
        }
    }

    private async void ShowPasswordDetails(PasswordItem item)
    {
        try
        {
            if (_serviceProvider is null)
            {
                var errorDialog = new ModernWpf.Controls.ContentDialog
                {
                    Title = "Something went wrong",
                    Content = "The app isn't fully loaded yet. Please close this page, reopen it, and try again.",
                    CloseButtonText = "OK"};
                await errorDialog.ShowAsync();
                return;
            }

            // Use the AddPasswordDialog in read-only mode for a richer view
            var dialog = new Dialogs.AddPasswordDialog(_serviceProvider, item, true);
            ConfigureDialogForCentering(dialog);
            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            var errorDialog = new ModernWpf.Controls.ContentDialog
            {
                Title = "Error",
                Content = $"Error showing password details: {ex.Message}",
                CloseButtonText = "OK"
            };
            ConfigureDialogForCentering(errorDialog);
            await errorDialog.ShowAsync();
        }
    }

    private async void ViewDetailButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedItem is null)
        {
            await ShowTemporaryMessageAsync("No item selected to view");
            return;
        }

        try
        {
            if (_serviceProvider is null)
            {
                var errorDialog = new ModernWpf.Controls.ContentDialog
                {
                    Title = "Something went wrong",
                    Content = "The app isn't fully loaded yet. Please close this page, reopen it, and try again.",
                    CloseButtonText = "OK"};
                await errorDialog.ShowAsync();
                return;
            }

            var dialog = new Dialogs.AddPasswordDialog(_serviceProvider, _selectedItem, true);
            ConfigureDialogForCentering(dialog);
            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            var errorDialog = new ModernWpf.Controls.ContentDialog
            {
                Title = "Error",
                Content = $"Error opening view dialog: {ex.Message}",
                CloseButtonText = "OK"
            };
            ConfigureDialogForCentering(errorDialog);
            await errorDialog.ShowAsync();
        }
    }

    private void ClearFilters_Click(object sender, RoutedEventArgs e)
    {
        // Clear all filter checkboxes and reset to default state
        if (_viewModel != null)
        {
            _viewModel.FilterType = "All";
        }
        
        // Close the filter flyout (safe)
        var filterFlyout = GetElement<Flyout>("FilterFlyout");
        filterFlyout?.Hide();

        // Update content titles
        UpdateContentTitles("All");
    }

    private async void ApplyFilters_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Get the filter flyout once and reuse to avoid variable shadowing
            var filterFlyout = GetElement<Flyout>("FilterFlyout");

            // Apply the selected filters based on checkbox states in the filter flyout
            if (_viewModel != null)
            {
                // Get the filter flyout content and read checkbox states
                if (filterFlyout?.Content is StackPanel filterPanel)
                {
                    // Apply filters based on checkbox selections
                    // This would be implemented based on the specific filter criteria
                    // For now, we'll apply a general filter update
                    await _viewModel.ApplyFilters();
                }
            }

            // Close the flyout
            filterFlyout?.Hide();

            // Show feedback to user
            ShowFilterAppliedFeedback();
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Warning("Suppressed exception", ex); }
    }

    private void ShowFilterAppliedFeedback()
    {
        // Provide visual feedback that filters have been applied
        // This could be a subtle animation or status update
        try
        {
            var contentTitle = GetElement<TextBlock>("ContentTitle");
            if (contentTitle != null)
            {
                // Temporarily update the subtitle to show filter feedback
                var contentSubtitle = GetElement<TextBlock>("ContentSubtitle");
                if (contentSubtitle != null)
                {
                    var originalText = contentSubtitle.Text;
                    contentSubtitle.Text = "Filters applied";

                    // Reset after a brief delay
                    Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() =>
                    {
                        Task.Delay(2000).ContinueWith(_ =>
                        {
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                contentSubtitle.Text = originalText;
                            }));
                        });
                    }));
                }
            }
        }
        catch (System.Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Suppressed exception", logEx); }
    }

    private void CategorySearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox searchBox)
        {
            string searchText = searchBox.Text?.ToLower() ?? "";
            FilterCategoryDropdown(searchText);
        }
    }

    private void FilterCategoryDropdown(string searchText)
    {
        var categoryDropdown = GetElement<ComboBox>("CategoryDropdown");
        if (categoryDropdown == null) return;

        categoryDropdown.Items.Clear();

        // Always add "All Categories" option
        var allCategoriesItem = new ComboBoxItem();
        var allStackPanel = new StackPanel { Orientation = Orientation.Horizontal };
        allStackPanel.Children.Add(new Border
        {
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGray),
            CornerRadius = new CornerRadius(4),
            Width = 14,
            Height = 14,
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center
        });
        allStackPanel.Children.Add(new TextBlock { Text = "All Categories", FontWeight = FontWeights.Medium, VerticalAlignment = VerticalAlignment.Center });
        allCategoriesItem.Content = allStackPanel;
        allCategoriesItem.Tag = "all";
        categoryDropdown.Items.Add(allCategoriesItem);

        AddItemTypeEntries(categoryDropdown);

        // Filter categories based on search text
        var filteredCategories = string.IsNullOrEmpty(searchText)
            ? _categories
            : _categories.Where(c => c.Name.ToLower().Contains(searchText)).ToList();

        // Add filtered categories to dropdown (de-duplicated by name)
        foreach (var category in DistinctCategories(filteredCategories))
        {
            var item = new ComboBoxItem();
            var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };

            // Add color indicator
            var colorBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray);
            if (!string.IsNullOrEmpty(category.Color))
            {
                try
                {
                    var converter = new System.Windows.Media.BrushConverter();
                    if (converter.ConvertFromString(category.Color) is System.Windows.Media.SolidColorBrush parsed)
                        colorBrush = parsed;
                }
                catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error($"Operation failed", ex); }
            }

            stackPanel.Children.Add(new Border
            {
                Background = colorBrush,
                CornerRadius = new CornerRadius(4),
                Width = 14,
                Height = 14,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            });

            // Add category name
            stackPanel.Children.Add(new TextBlock
            {
                Text = category.Name,
                FontWeight = FontWeights.Medium,
                VerticalAlignment = VerticalAlignment.Center
            });

            item.Content = stackPanel;
            item.Tag = category;
            categoryDropdown.Items.Add(item);
        }
    }

    // Missing event handlers implementation
    private async void CopyUsernameButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var list = GetElement<ListView>("ItemsList");
            var selected = _selectedItem ?? (list?.SelectedItem as PasswordItem);
            var username = selected?.Username ?? selected?.LoginItem?.Username;
            if (string.IsNullOrEmpty(username))
            {
                await ShowTemporaryMessageAsync("No username available to copy");
                return;
            }

            System.Windows.Clipboard.SetText(username);
            ToastService.Instance.Success("Username copied to clipboard");
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Warning("Suppressed exception", ex); }
    }

    private void GeneratePasswordButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Generate a secure random password
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            var password = new string(Enumerable.Repeat(chars, 16)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            // Find the password field and set the generated password
            var passwordBox = GetElement<PasswordBox>("EditPasswordBox");
            if (passwordBox != null)
            {
                passwordBox.Password = password;
            }
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Warning("Suppressed exception", ex); }
    }

    private async void EditDetailButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedItem is null)
        {
            await ShowTemporaryMessageAsync("No item selected to edit");
            return;
        }

        try
        {
            if (_serviceProvider is null)
            {
                var errorDialog = new ModernWpf.Controls.ContentDialog
                {
                    Title = "Something went wrong",
                    Content = "The app isn't fully loaded yet. Please close this page, reopen it, and try again.",
                    CloseButtonText = "OK"};
                await errorDialog.ShowAsync();
                return;
            }

            var dialog = new Dialogs.AddPasswordDialog(_serviceProvider, _selectedItem, false);
            ConfigureDialogForCentering(dialog);
            var result = await dialog.ShowAsync();

            if (result == ModernWpf.Controls.ContentDialogResult.Primary)
            {
                // Refresh the view after editing
                await ShowTemporaryMessageAsync("Item updated successfully");
            }
        }
        catch (Exception ex)
        {
            var errorDialog = new ModernWpf.Controls.ContentDialog
            {
                Title = "Error",
                Content = $"Error opening edit dialog: {ex.Message}",
                CloseButtonText = "OK"};
            await errorDialog.ShowAsync();
        }
    }

    private void CategoryFilterBox_TextChanged(object sender, AutoSuggestBoxTextChangedEventArgs e)
    {
        if (sender is AutoSuggestBox autoSuggestBox && e.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            var suggestions = new List<string>();
            string queryText = autoSuggestBox.Text?.ToLower() ?? "";

            if (!string.IsNullOrEmpty(queryText))
            {
                suggestions = _categories
                    .Where(c => c.Name.ToLower().Contains(queryText))
                    .Select(c => c.Name)
                    .ToList();
            }

            autoSuggestBox.ItemsSource = suggestions;
        }
    }

    private void CategoryFilterBox_SuggestionChosen(object sender, AutoSuggestBoxSuggestionChosenEventArgs e)
    {
        if (sender is AutoSuggestBox autoSuggestBox && e.SelectedItem is string selectedCategory)
        {
            autoSuggestBox.Text = selectedCategory;
            // Apply category filter logic here if needed
        }
    }

    private void AddCustomFieldButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement anchor)
            CustomFieldHelper.ShowFieldTypeMenu(anchor, AddDetailFieldOfType);
    }

    private async void AddDetailFieldOfType(CustomFieldType fieldType)
    {
        if (_selectedItem == null) return;

        _selectedItem.CustomFields ??= new List<CustomField>();
        _selectedItem.CustomFields.Add(new CustomField
        {
            Name           = CustomFieldHelper.GetDefaultFieldName(fieldType),
            Value          = "",
            Type           = fieldType,
            DisplayOrder   = _selectedItem.CustomFields.Count,
            PasswordItemId = _selectedItem.Id,
            CreatedAt      = DateTime.UtcNow,
            LastModified   = DateTime.UtcNow,
        });

        if (_passwordItemService != null)
        {
            try { await _passwordItemService.UpdateAsync(_selectedItem); }
            catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error($"Operation failed", ex); }
        }

        RefreshDetailCustomFields();
    }

    private void RefreshDetailCustomFields()
    {
        var cfList      = GetElement<StackPanel>("DetailCustomFieldsList");
        var noFieldsText = GetElement<TextBlock>("NoCustomFieldsText");

        const string totpFieldName      = "TOTP Secret";
        const string brandIconFieldName = "Brand Icon";
        var visible = _selectedItem?.CustomFields?
            .Where(f => !string.Equals(f.Name, totpFieldName, StringComparison.OrdinalIgnoreCase)
                     && !string.Equals(f.Name, brandIconFieldName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f.DisplayOrder)
            .ToList() ?? new List<CustomField>();

        if (cfList != null)
        {
            cfList.Children.Clear();
            foreach (var field in visible)
            {
                var row = CustomFieldHelper.CreateCustomFieldRow(
                    field,
                    cfList,
                    OnDetailCustomFieldChanged,
                    OnDetailCustomFieldRemoved,
                    OnDetailCustomFieldReorder
                );
                cfList.Children.Add(row);
            }
        }

        if (noFieldsText != null)
            noFieldsText.Visibility = visible.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void OnDetailCustomFieldChanged(CustomField field)
    {
        field.LastModified = DateTime.UtcNow;
        if (_passwordItemService != null && _selectedItem != null)
        {
            try { await _passwordItemService.UpdateAsync(_selectedItem); }
            catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error($"Operation failed", ex); }
        }
    }

    private async void OnDetailCustomFieldRemoved(CustomField field)
    {
        _selectedItem?.CustomFields?.Remove(field);
        if (_passwordItemService != null && _selectedItem != null)
        {
            try { await _passwordItemService.UpdateAsync(_selectedItem); }
            catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error($"Operation failed", ex); }
        }
        RefreshDetailCustomFields();
    }

    private async void OnDetailCustomFieldReorder(CustomField dragged, int newIndex)
    {
        if (_selectedItem?.CustomFields == null) return;
        var list = _selectedItem.CustomFields
            .Where(f => !string.Equals(f.Name, "TOTP Secret", StringComparison.OrdinalIgnoreCase)
                     && !string.Equals(f.Name, "Brand Icon",   StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f.DisplayOrder)
            .ToList();

        list.Remove(dragged);
        newIndex = Math.Clamp(newIndex, 0, list.Count);
        list.Insert(newIndex, dragged);
        for (int i = 0; i < list.Count; i++) list[i].DisplayOrder = i;

        if (_passwordItemService != null)
        {
            try { await _passwordItemService.UpdateAsync(_selectedItem); }
            catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error($"Operation failed", ex); }
        }
        RefreshDetailCustomFields();
    }

    private void DetailScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        if (e.Handled) return;
        if (sender is not ScrollViewer sv) return;
        sv.ScrollToVerticalOffset(sv.VerticalOffset - (e.Delta / 2.0));
        e.Handled = true;
    }

    private async void SaveDetailButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedItem == null || _passwordItemService == null) return;
        try { await _passwordItemService.UpdateAsync(_selectedItem); }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error($"Operation failed", ex); }
    }
}
