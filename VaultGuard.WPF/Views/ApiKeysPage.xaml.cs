using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using VaultGuard.Models;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.WPF.Views;

public sealed partial class ApiKeysPage : Page
{
    // Row model for the ItemsControl - pre-formats the fields the XAML binds to (dates, status
    // text/color) since WPF has no built-in date/bool-to-brush converters wired up for this page.
    private sealed class ApiKeyRow
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string CreatedText { get; init; } = string.Empty;
        public string LastUsedText { get; init; } = string.Empty;
        public string StatusText { get; init; } = string.Empty;
        public Brush StatusBrush { get; init; } = Brushes.Gray;
    }

    private IServiceProvider? _serviceProvider;

    public ApiKeysPage()
    {
        InitializeComponent();
    }

    public async void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
    {
        if (e.ExtraData is IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            await LoadKeysAsync();
        }
    }

    private async Task<string?> GetCurrentUserIdAsync()
    {
        if (_serviceProvider is null) return null;
        var authService = _serviceProvider.GetRequiredService<IAuthService>();
        return authService.CurrentUser?.Id ?? await authService.GetCurrentUserIdAsync();
    }

    private async Task LoadKeysAsync()
    {
        if (_serviceProvider is null) return;

        try
        {
            var userId = await GetCurrentUserIdAsync();
            if (string.IsNullOrEmpty(userId))
            {
                ApiKeysItemsControl.ItemsSource = null;
                EmptyStateText.Visibility = Visibility.Visible;
                return;
            }

            var apiKeyService = _serviceProvider.GetRequiredService<IApiKeyService>();
            var keys = await apiKeyService.GetUserApiKeysAsync(userId);

            var rows = keys.Select(k => new ApiKeyRow
            {
                Id = k.Id,
                Name = k.Name,
                CreatedText = k.CreatedAt.ToLocalTime().ToString("MMM d, yyyy"),
                LastUsedText = k.LastUsedAt?.ToLocalTime().ToString("MMM d, yyyy HH:mm") ?? "Never",
                StatusText = k.IsActive ? "Active" : "Inactive",
                StatusBrush = k.IsActive ? new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81))
                                         : new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)),
            }).ToList();

            ApiKeysItemsControl.ItemsSource = new ObservableCollection<ApiKeyRow>(rows);
            EmptyStateText.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            VaultGuard.Services.Logging.AppLogger.Error("Failed to load API keys", ex);
            await ShowErrorDialog($"Failed to load API keys: {ex.Message}");
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e) => await LoadKeysAsync();

    private async void CreateKeyButton_Click(object sender, RoutedEventArgs e)
    {
        if (_serviceProvider is null) return;

        var name = NewKeyNameTextBox.Text?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            await ShowErrorDialog("Enter a name for the key first.");
            return;
        }

        try
        {
            var userId = await GetCurrentUserIdAsync();
            if (string.IsNullOrEmpty(userId))
            {
                await ShowErrorDialog("You must be signed in to create an API key.");
                return;
            }

            var apiKeyService = _serviceProvider.GetRequiredService<IApiKeyService>();
            // CreateApiKeyAsync returns the plaintext key value in KeyHash for this one-time display -
            // same contract VaultGuard.Web's ApiKeyManagement.razor relies on (see its CreateApiKey method).
            var created = await apiKeyService.CreateApiKeyAsync(name, userId);

            NewKeyNameTextBox.Text = string.Empty;
            await LoadKeysAsync();
            await ShowGeneratedKeyDialog(created);
        }
        catch (Exception ex)
        {
            VaultGuard.Services.Logging.AppLogger.Error("Failed to create API key", ex);
            await ShowErrorDialog($"Failed to create API key: {ex.Message}");
        }
    }

    private async void RevokeKeyButton_Click(object sender, RoutedEventArgs e)
    {
        if (_serviceProvider is null) return;
        if (sender is not Button button || button.DataContext is not ApiKeyRow row) return;

        var dialog = new ModernWpf.Controls.ContentDialog
        {
            Title = "Revoke API Key",
            Content = $"Revoke \"{row.Name}\"? Any app using this key will stop working immediately.",
            PrimaryButtonText = "Revoke",
            CloseButtonText = "Cancel",
            DefaultButton = ModernWpf.Controls.ContentDialogButton.Close
        };

        var result = await dialog.ShowAsync();
        if (result != ModernWpf.Controls.ContentDialogResult.Primary) return;

        try
        {
            var userId = await GetCurrentUserIdAsync();
            if (string.IsNullOrEmpty(userId)) return;

            var apiKeyService = _serviceProvider.GetRequiredService<IApiKeyService>();
            await apiKeyService.DeleteApiKeyAsync(row.Id, userId);
            await LoadKeysAsync();
        }
        catch (Exception ex)
        {
            VaultGuard.Services.Logging.AppLogger.Error("Failed to revoke API key", ex);
            await ShowErrorDialog($"Failed to revoke API key: {ex.Message}");
        }
    }

    private async Task ShowGeneratedKeyDialog(ApiKey created)
    {
        var keyBox = new TextBox
        {
            Text = created.KeyHash,
            IsReadOnly = true,
            MinWidth = 380,
            FontFamily = new FontFamily("Consolas"),
            Style = (Style)Application.Current.Resources["ModernTextBoxStyle"],
            Margin = new Thickness(0, 10, 0, 10)
        };

        var panel = new StackPanel();
        panel.Children.Add(new TextBlock
        {
            Text = "This is the only time you'll see this key. Copy it now and store it securely.",
            TextWrapping = TextWrapping.Wrap
        });
        panel.Children.Add(keyBox);

        var dialog = new ModernWpf.Controls.ContentDialog
        {
            Title = "API Key Created",
            Content = panel,
            PrimaryButtonText = "Copy to Clipboard",
            CloseButtonText = "Done",
            DefaultButton = ModernWpf.Controls.ContentDialogButton.Primary
        };

        var result = await dialog.ShowAsync();
        if (result == ModernWpf.Controls.ContentDialogResult.Primary)
        {
            try { Clipboard.SetText(created.KeyHash); }
            catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error("Failed to copy API key to clipboard", ex); }
        }
    }

    private async Task ShowErrorDialog(string message)
    {
        var dialog = new ModernWpf.Controls.ContentDialog { Title = "Error", Content = message, CloseButtonText = "OK" };
        await dialog.ShowAsync();
    }
}
