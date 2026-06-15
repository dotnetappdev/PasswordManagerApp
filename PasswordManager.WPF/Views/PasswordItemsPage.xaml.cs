using ModernWpf.Controls;
using System.Windows;
using System.Windows.Controls;
using ListView = System.Windows.Controls.ListView;
using Controls = PasswordManager.WPF.Controls;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.WPF.ViewModels;
using PasswordManager.WPF.Helpers;
using PasswordManager.WPF.Models;
using PasswordManager.Models;
using PasswordManager.Services.Interfaces;
using PasswordManager.DAL.Seed;
using PasswordManager.WPF.Services;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace PasswordManager.WPF.Views;

public sealed partial class PasswordItemsPage : System.Windows.Controls.Page
{
    private PasswordItemsViewModel? _viewModel;
    private IServiceProvider? _serviceProvider;
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
        this.InitializeComponent();

        // Refresh the live list when data is cleared/changed elsewhere (e.g. Settings → Delete Seed Data).
        Loaded += (_, _) =>
        {
            PasswordManager.WPF.Services.AppEvents.VaultDataChanged -= OnVaultDataChanged;
            PasswordManager.WPF.Services.AppEvents.VaultDataChanged += OnVaultDataChanged;
        };
        Unloaded += (_, _) =>
            PasswordManager.WPF.Services.AppEvents.VaultDataChanged -= OnVaultDataChanged;
    }

    private async void OnVaultDataChanged()
    {
        try
        {
            if (_viewModel != null)
                await _viewModel.RefreshAsync();
            await LoadCategoriesAsync();
        }
        catch { /* refresh is best-effort */ }
    }

    private T? GetElement<T>(string name) where T : class
    {
        try
        {
            return this.FindName(name) as T;
        }
        catch
        {
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
            catch (Exception ex)
            { }

            // Reload view model items after seeding to ensure UI shows newly created items
            try
            {
                if (_viewModel != null)
                {
                    await _viewModel.RefreshAsync();
                }
            }
            catch (Exception ex)
            { }
        }
    }

    private void ApplyNavigationFilter(NavigationFilterData filterData)
    {
        if (_viewModel == null) return;

        // Update page title
        if (!string.IsNullOrEmpty(filterData.FilterName))
        {
            var contentTitle = GetElement<TextBlock>("ContentTitle");
            var contentSubtitle = GetElement<TextBlock>("ContentSubtitle");
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
        catch (Exception ex)
        { }
    }

    // De-duplicates categories by trimmed, case-insensitive name so the dropdown never repeats
    // entries (e.g. when sample data has been seeded more than once).
    private static IEnumerable<Category> DistinctCategories(IEnumerable<Category> source)
        => source
            .Where(c => c != null)
            .GroupBy(c => (c.Name ?? string.Empty).Trim().ToLowerInvariant())
            .Select(g => g.First());

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
                catch { }
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
                catch
                {
                    // Ignore count errors
                }
            }

            item.Content = stackPanel;
            item.Tag = category;
            categoryDropdown.Items.Add(item);
        }

        // Select first item (All Categories)
        categoryDropdown.SelectedIndex = 0;
    }

    public void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_viewModel != null && sender is TextBox textBox)
        {
            _viewModel.SearchText = textBox.Text;
        }
    }

    // ── Global search bar (items list) ──────────────────────────────────────
    private void GlobalSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox tb) return;

        // Toggle placeholder + clear button
        var placeholder = GetElement<TextBlock>("SearchPlaceholder");
        if (placeholder != null)
            placeholder.Visibility = string.IsNullOrEmpty(tb.Text) ? Visibility.Visible : Visibility.Collapsed;

        var clearBtn = GetElement<Button>("ClearSearchButton");
        if (clearBtn != null)
            clearBtn.Visibility = string.IsNullOrEmpty(tb.Text) ? Visibility.Collapsed : Visibility.Visible;

        // Live filter as you type (still global within the current view).
        if (_viewModel != null)
            _viewModel.SearchText = tb.Text;
    }

    private void GlobalSearch_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            RunGlobalSearch();
            e.Handled = true;
        }
        else if (e.Key == System.Windows.Input.Key.Escape)
        {
            ClearGlobalSearch();
            e.Handled = true;
        }
    }

    private void RunSearchButton_Click(object sender, RoutedEventArgs e) => RunGlobalSearch();

    private void ClearSearchButton_Click(object sender, RoutedEventArgs e) => ClearGlobalSearch();

    /// <summary>
    /// Runs a global search like VS Code: searches across ALL items regardless of the currently
    /// selected category (the category filter is reset so nothing is hidden from the results).
    /// </summary>
    private void RunGlobalSearch()
    {
        if (_viewModel == null) return;

        var box = GetElement<TextBox>("GlobalSearchTextBox");
        var query = box?.Text ?? string.Empty;

        // Make it global: drop any active category filter so all items are searched.
        _viewModel.SelectedCategoryId = null;
        var dropdown = GetElement<ComboBox>("CategoryDropdown");
        if (dropdown != null && dropdown.Items.Count > 0)
            dropdown.SelectedIndex = 0;

        _viewModel.SearchText = query;
    }

    private void ClearGlobalSearch()
    {
        var box = GetElement<TextBox>("GlobalSearchTextBox");
        if (box != null) box.Text = string.Empty;
        if (_viewModel != null) _viewModel.SearchText = string.Empty;
    }

    // ── Public entry points for application keyboard shortcuts (wired in MainWindow) ──
    public void TriggerAddNew() => ShowAddPasswordDialog();

    public void FocusSearch()
    {
        var box = GetElement<TextBox>("GlobalSearchTextBox");
        if (box != null)
        {
            box.Focus();
            System.Windows.Input.Keyboard.Focus(box);
            box.SelectAll();
        }
    }

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

        var dialog = new ModernWpf.Controls.ContentDialog
        {
            Title = "Delete Item",
            Content = $"Are you sure you want to delete '{_selectedItem.Title}'?",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ModernWpf.Controls.ContentDialogButton.Close
        };
        ConfigureDialogForCentering(dialog);

        if (await dialog.ShowAsync() == ModernWpf.Controls.ContentDialogResult.Primary)
        {
            var title = _selectedItem.Title;
            await _viewModel.DeleteItemAsync(_selectedItem);
            ToastService.Instance.Success($"'{title}' deleted");
        }
    }

    private void FilterButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string filterType)
        {
            // Update UI to show selected filter
            UpdateFilterButtonStyles(button);

            // Apply filter to view model
            if (_viewModel != null)
            {
                _viewModel.FilterType = filterType;
            }

            // Update content titles
            UpdateContentTitles(filterType);
        }
    }

    private void UpdateFilterButtonStyles(Button selectedButton)
    {
        // Safely reset filter buttons if they exist (some layouts removed buttons)
        try
        {
            var allItemsBtn = this.FindName("AllItemsButton") as Button;
            if (allItemsBtn != null)
            {
                allItemsBtn.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Transparent);
            }

            // Example: if there are more named filter buttons, try to reset them too
            var namedButtons = new[] { "AllItemsButton", "FavoritesButton", "RecentButton", "LoginButton" };
            foreach (var name in namedButtons)
            {
                var btn = this.FindName(name) as Button;
                if (btn != null)
                {
                    btn.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Transparent);
                }
            }

            // Set selected button style if provided (defensive resource lookup)
            if (selectedButton != null)
            {
                selectedButton.Background = Helpers.ResourceHelper.GetBrush("ModernPrimaryBrush", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Transparent));
            }
        }
        catch
        {
            // Defensive: if FindName or resource lookup fails, ignore and continue
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
    }

    private void CategoryDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem selectedItem)
        {
            if (_viewModel != null)
            {
                if (selectedItem.Tag is string tag && tag == "all")
                {
                    // Show all items
                    _viewModel.SelectedCategoryId = null;
                }
                else if (selectedItem.Tag is Category category)
                {
                    // Filter by specific category using CategoryId only
                    _viewModel.SelectedCategoryId = category.Id;
                }
            }
        }
    }

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

    private void RevealPasswordButton_Click(object sender, RoutedEventArgs e)
    {
        var detailPassword = GetElement<Controls.ReadOnlyField>("DetailPassword");
        if (detailPassword == null) return;

        // CopyText always holds the real value (set in ShowItemDetails for every type)
        var real = detailPassword.CopyText ?? string.Empty;
        if (string.IsNullOrEmpty(real)) return;

        // Toggle between masked and revealed
        if (!string.IsNullOrEmpty(detailPassword.Text) && detailPassword.Text.StartsWith("•"))
            detailPassword.Text = real;                                   // reveal
        else
            detailPassword.Text = new string('•', Math.Max(8, real.Length)); // mask
    }

    private async void CopyPasswordButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var list = GetElement<ListView>("ItemsList");
            var selected = _selectedItem ?? (list?.SelectedItem as PasswordItem);
            var pwd = selected?.Password ?? selected?.LoginItem?.Password;
            if (string.IsNullOrEmpty(pwd))
            {
                await ShowTemporaryMessageAsync("No password available to copy");
                return;
            }

            System.Windows.Clipboard.SetText(pwd);
            ToastService.Instance.Success("Password copied to clipboard");
        }
        catch (Exception ex)
        { }
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
            try { System.Windows.Clipboard.SetText(username); } catch { }
            ToastService.Instance.Info("Username copied — paste it (Ctrl+V), then Tab to the password field.");

            if (!string.IsNullOrEmpty(password))
            {
                await Task.Delay(9000);
                try { System.Windows.Clipboard.SetText(password); } catch { }
                ToastService.Instance.Success("Password copied — paste it into the password field.");
            }
        }
        else if (!string.IsNullOrEmpty(password))
        {
            try { System.Windows.Clipboard.SetText(password); } catch { }
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
        catch (Exception ex)
        { }
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
            try { System.Windows.Clipboard.SetText(url); ToastService.Instance.Success("Website copied to clipboard"); } catch { }
        }
    }

    private void RevealCvvButton_Click(object sender, RoutedEventArgs e)
    {
        var tb = GetElement<TextBlock>("DetailCardCvv");
        if (tb == null || string.IsNullOrEmpty(_currentCardCvv)) return;

        // Toggle between masked dots and the real CVV.
        tb.Text = tb.Text.StartsWith("•") ? _currentCardCvv : new string('•', _currentCardCvv.Length);
    }

    private void CopyCvvButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentCardCvv)) return;
        try { System.Windows.Clipboard.SetText(_currentCardCvv); ToastService.Instance.Success("CVV copied to clipboard"); } catch { }
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
        var usernameSection = GetElement<StackPanel>("DetailUsernameSection");
        var passwordSection = GetElement<StackPanel>("DetailPasswordSection");
        var websiteSection  = GetElement<StackPanel>("DetailWebsiteSection");
        var creditCardSection = GetElement<StackPanel>("DetailCreditCardSection");
        var usernameLabel   = GetElement<TextBlock>("UsernameLabel");
        var passwordLabel   = GetElement<TextBlock>("PasswordLabel");
        var websiteLabel    = GetElement<TextBlock>("WebsiteLabel");

        // Default: all login sections visible, CC section hidden
        if (usernameSection   != null) usernameSection.Visibility   = Visibility.Visible;
        if (passwordSection   != null) passwordSection.Visibility   = Visibility.Visible;
        if (websiteSection    != null) websiteSection.Visibility    = Visibility.Visible;
        if (creditCardSection != null) creditCardSection.Visibility = Visibility.Collapsed;

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

            if (detailUsername != null) { detailUsername.Text = string.IsNullOrEmpty(cardNumber) ? "—" : cardNumber; detailUsername.CopyText = cardNumber; }
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
                detailPassword.Text     = string.IsNullOrEmpty(pwd) ? "—" : new string('•', Math.Max(8, pwd.Length));
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
            // ── Passkey layout ──────────────────────────────────────────────
            if (usernameLabel != null) usernameLabel.Text = "USERNAME";
            if (passwordLabel != null) passwordLabel.Text = "DISPLAY NAME";
            if (websiteLabel  != null) websiteLabel.Text  = "WEBSITE";

            username = item.PasskeyItem?.Username ?? item.Username ?? string.Empty;
            website  = item.PasskeyItem?.WebsiteUrl ?? item.PasskeyItem?.Website ?? item.Website ?? string.Empty;
            var display = item.PasskeyItem?.DisplayName ?? string.Empty;

            if (detailUsername != null) { detailUsername.Text = username; detailUsername.CopyText = username; }
            if (detailWebsite  != null) { detailWebsite.Text  = website;  detailWebsite.CopyText  = website; }
            if (detailPassword != null) { detailPassword.Text = string.IsNullOrEmpty(display) ? "—" : display; detailPassword.CopyText = display; }
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
                detailPassword.Text     = string.IsNullOrEmpty(pwd) ? "—" : new string('•', Math.Max(8, pwd.Length));
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
                var isProtected = PasswordManager.Services.Utilities.ProtectedItemHelper.IsProtected(item)
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
        catch { }

        // Custom fields
        try
        {
            var cfList = GetElement<ItemsControl>("DetailCustomFieldsList");
            var cfSection = GetElement<StackPanel>("DetailCustomFieldsView");
            if (cfList != null && item.CustomFields?.Any() == true)
            {
                cfList.ItemsSource = item.CustomFields;
                if (cfSection != null) cfSection.Visibility = Visibility.Visible;
            }
            else if (cfSection != null)
            {
                cfSection.Visibility = Visibility.Collapsed;
            }
        }
        catch { }

        // Tags
        try
        {
            var detailTags = GetElement<ItemsControl>("DetailTagsPanel");
            if (detailTags != null) detailTags.ItemsSource = item.Tags ?? new List<Tag>();

            var editTagsPanel = GetElement<ItemsControl>("EditTagsPanel");
            if (editTagsPanel != null) editTagsPanel.ItemsSource = item.Tags ?? new List<Tag>();
        }
        catch { }

        // Live verification code (TOTP)
        SetupTotp(item);
    }

    /// <summary>
    /// Shows a live, auto-refreshing TOTP verification code for items that carry an authenticator
    /// secret; hides the section (and stops the timer) for everything else.
    /// </summary>
    private void SetupTotp(PasswordItem item)
    {
        var section = GetElement<StackPanel>("DetailTotpSection");
        _currentTotpSecret = PasswordManager.Services.Utilities.TotpHelper.GetSecret(item);

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
        catch { }
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
            var db = sp.GetRequiredService<PasswordManager.DAL.PasswordManagerDbContext>();
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
                    Title = "Error",
                    Content = "Service provider not initialized. Please navigate to this page properly.",
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
                var dialog = new Dialogs.AddPasswordDialog(_serviceProvider);
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
        if (sender is System.Windows.Controls.MenuItem menuItem &&
            menuItem.DataContext is PasswordItem item &&
            _viewModel != null)
        {
            var dialog = new ModernWpf.Controls.ContentDialog
            {
                Title = "Delete Password Item",
                Content = $"Are you sure you want to delete '{item.Title}'?",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ModernWpf.Controls.ContentDialogButton.Close};

            var result = await dialog.ShowAsync();
            if (result == ModernWpf.Controls.ContentDialogResult.Primary)
            {
                await _viewModel.DeleteItemAsync(item);
                ToastService.Instance.Success($"'{item.Title}' deleted");
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
                    Title = "Error",
                    Content = "Service provider not initialized. Please navigate to this page properly.",
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
        catch { }
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
        catch { }
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
        catch { }
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
        catch (Exception ex)
        { }
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
        catch (Exception ex)
        { }
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
        catch (Exception ex)
        { }
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
        catch (Exception ex)
        {
            // Fallback - no action needed
        }
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
                    Title = "Error",
                    Content = "Service provider not initialized. Please navigate to this page properly.",
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
                    Title = "Error",
                    Content = "Service provider not initialized. Please navigate to this page properly.",
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
        catch (Exception ex)
        { }
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
        catch
        {
            // Ignore feedback errors
        }
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
                catch { }
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
        catch (Exception ex)
        { }
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
        catch (Exception ex)
        { }
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
                    Title = "Error",
                    Content = "Service provider not initialized. Please navigate to this page properly.",
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
        try
        {
            var customFieldsContainer = GetElement<StackPanel>("CustomFieldsContainer");
            if (customFieldsContainer != null)
            {
                // Create a new custom field UI
                var fieldGrid = new Grid();
                fieldGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                fieldGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                fieldGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var nameTextBox = new TextBox
                {
                    // PlaceholderText = "Field name",
                    Margin = new Thickness(0, 0, 4, 0)
                };
                Grid.SetColumn(nameTextBox, 0);

                var valueTextBox = new TextBox
                {
                    // PlaceholderText = "Field value",
                    Margin = new Thickness(4, 0, 4, 0)
                };
                Grid.SetColumn(valueTextBox, 1);

                var removeButton = new Button
                {
                    Content = "✕",
                    Width = 32,
                    Height = 32,
                    Margin = new Thickness(4, 0, 0, 0)
                };
                Grid.SetColumn(removeButton, 2);

                removeButton.Click += (s, args) => customFieldsContainer.Children.Remove(fieldGrid);

                fieldGrid.Children.Add(nameTextBox);
                fieldGrid.Children.Add(valueTextBox);
                fieldGrid.Children.Add(removeButton);

                customFieldsContainer.Children.Add(fieldGrid);
            }
        }
        catch (Exception ex)
        { }
    }
}
