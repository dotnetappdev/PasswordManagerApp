using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PasswordManager.Uno.Services.LocalDatabase;
using PasswordManager.Uno.Services.Sync;
using System.Collections.ObjectModel;

namespace PasswordManager.Mobile.Presentation.Pages.Passwords;

public partial class PasswordsModel : ObservableObject
{
    private readonly LocalDatabaseService _localDatabase;
    private readonly SyncService _syncService;
    private readonly ILogger<PasswordsModel> _logger;

    [ObservableProperty]
    private ObservableCollection<LocalPasswordItem> passwordItems = new();

    [ObservableProperty]
    private LocalPasswordItem? selectedItem;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool isSyncing;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private string? statusMessage;

    public PasswordsModel(
        LocalDatabaseService localDatabase,
        SyncService syncService,
        ILogger<PasswordsModel> logger)
    {
        _localDatabase = localDatabase;
        _syncService = syncService;
        _logger = logger;
    }

    public async Task LoadPasswordsAsync()
    {
        IsLoading = true;
        try
        {
            var items = await _localDatabase.GetAllPasswordItemsAsync();
            PasswordItems.Clear();
            foreach (var item in items)
            {
                PasswordItems.Add(item);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading passwords");
            StatusMessage = $"Error loading passwords: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsSyncing = true;
        StatusMessage = "Syncing with server...";
        
        try
        {
            var result = await _syncService.SyncFromServerAsync();
            
            if (result.Success)
            {
                StatusMessage = $"Synced successfully! Added {result.ItemsAdded}, Updated {result.ItemsUpdated}";
                await LoadPasswordsAsync();
            }
            else
            {
                StatusMessage = $"Sync failed: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during sync");
            StatusMessage = $"Sync error: {ex.Message}";
        }
        finally
        {
            IsSyncing = false;
        }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            await LoadPasswordsAsync();
            return;
        }

        IsLoading = true;
        try
        {
            var items = await _localDatabase.SearchPasswordItemsAsync(SearchText);
            PasswordItems.Clear();
            foreach (var item in items)
            {
                PasswordItems.Add(item);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching passwords");
            StatusMessage = $"Search error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void SelectItem(LocalPasswordItem item)
    {
        SelectedItem = item;
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync(LocalPasswordItem item)
    {
        try
        {
            item.IsFavorite = !item.IsFavorite;
            await _localDatabase.SavePasswordItemAsync(item);
            StatusMessage = item.IsFavorite ? "Added to favorites" : "Removed from favorites";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling favorite");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DeleteItemAsync(LocalPasswordItem item)
    {
        try
        {
            await _localDatabase.DeletePasswordItemAsync(item);
            PasswordItems.Remove(item);
            StatusMessage = "Password deleted";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting password");
            StatusMessage = $"Error: {ex.Message}";
        }
    }
}
