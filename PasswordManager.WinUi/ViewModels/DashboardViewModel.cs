using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Models;
using PasswordManager.Services.Interfaces;

namespace PasswordManager.WinUi.ViewModels;

public class DashboardViewModel : BaseViewModel
{
    private readonly IPasswordItemService _passwordItemService;
    private readonly ICategoryInterface _categoryService;
    private readonly IAuthService _authService;
    private string _welcomeText = "Welcome!";
    private string _statusText = "Ready";
    private int _totalPasswordsCount;
    private int _favoritesCount;
    private int _categoriesCount;
    private PasswordItem? _selectedPasswordItem;
    private bool _isEditing;
    private string _selectedNavItem = "AllItems";

    public DashboardViewModel(IServiceProvider serviceProvider)
    {
        _passwordItemService = serviceProvider.GetRequiredService<IPasswordItemService>();
        _categoryService = serviceProvider.GetRequiredService<ICategoryInterface>();
        _authService = serviceProvider.GetRequiredService<IAuthService>();
        
        RecentPasswordItems = new ObservableCollection<PasswordItem>();
        FavoritePasswordItems = new ObservableCollection<PasswordItem>();
        AllPasswordItems = new ObservableCollection<PasswordItem>();
        
        LoadDashboardDataAsync();
    }

    public string WelcomeText
    {
        get => _welcomeText;
        set => SetProperty(ref _welcomeText, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public int TotalPasswordsCount
    {
        get => _totalPasswordsCount;
        set => SetProperty(ref _totalPasswordsCount, value);
    }

    public int FavoritesCount
    {
        get => _favoritesCount;
        set => SetProperty(ref _favoritesCount, value);
    }

    public int CategoriesCount
    {
        get => _categoriesCount;
        set => SetProperty(ref _categoriesCount, value);
    }

    public ObservableCollection<PasswordItem> RecentPasswordItems { get; }
    public ObservableCollection<PasswordItem> FavoritePasswordItems { get; }
    public ObservableCollection<PasswordItem> AllPasswordItems { get; }

    public PasswordItem? SelectedPasswordItem
    {
        get => _selectedPasswordItem;
        set => SetProperty(ref _selectedPasswordItem, value);
    }

    public bool IsEditing
    {
        get => _isEditing;
        set => SetProperty(ref _isEditing, value);
    }

    public string SelectedNavItem
    {
        get => _selectedNavItem;
        set
        {
            if (SetProperty(ref _selectedNavItem, value))
            {
                OnNavigationChanged();
            }
        }
    }

    private async Task LoadDashboardDataAsync()
    {
        try
        {
            IsLoading = true;
            StatusText = "Loading dashboard data...";

            // Load counts
            var allPasswordItems = await _passwordItemService.GetAllAsync();
            var activeItems = allPasswordItems.Where(i => !i.IsDeleted && !i.IsArchived).ToList();
            
            TotalPasswordsCount = activeItems.Count;
            FavoritesCount = activeItems.Count(i => i.IsFavorite);

            var categories = await _categoryService.GetAllAsync();
            CategoriesCount = categories.Count();

            // Load recent items (last 5)
            RecentPasswordItems.Clear();
            var recentItems = activeItems
                .OrderByDescending(i => i.CreatedAt)
                .Take(5);
            
            foreach (var item in recentItems)
            {
                RecentPasswordItems.Add(item);
            }

            // Load favorites
            FavoritePasswordItems.Clear();
            var favoriteItems = activeItems
                .Where(i => i.IsFavorite)
                .OrderByDescending(i => i.LastModified)
                .Take(10);

            foreach (var item in favoriteItems)
            {
                FavoritePasswordItems.Add(item);
            }

            // Load all items
            AllPasswordItems.Clear();
            foreach (var item in activeItems.OrderBy(i => i.Title))
            {
                AllPasswordItems.Add(item);
            }

            // Set default selection to first item if available
            if (AllPasswordItems.Any() && SelectedPasswordItem == null)
            {
                SelectedPasswordItem = AllPasswordItems.First();
            }

            // Set welcome message
            WelcomeText = $"Welcome! You have {TotalPasswordsCount} passwords secured.";
            StatusText = "Ready";
        }
        catch (Exception ex)
        {
            StatusText = $"Error loading dashboard: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Dashboard load error: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task RefreshAsync()
    {
        await LoadDashboardDataAsync();
    }

    public async Task LogoutAsync()
    {
        try
        {
            await _authService.LogoutAsync();
            StatusText = "Logged out successfully";
        }
        catch (Exception ex)
        {
            StatusText = $"Logout error: {ex.Message}";
        }
    }

    private void OnNavigationChanged()
    {
        // Clear selection when navigation changes
        SelectedPasswordItem = null;
        IsEditing = false;
    }

    public void SelectPasswordItem(PasswordItem item)
    {
        SelectedPasswordItem = item;
        IsEditing = false;
    }

    public void StartEditing()
    {
        IsEditing = true;
    }

    public void StopEditing()
    {
        IsEditing = false;
    }

    public async Task FilterPasswordItemsAsync(string searchText)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                // If search is empty, reload all items
                await LoadDashboardDataAsync();
                return;
            }

            // Get all password items and filter them
            var allPasswordItems = await _passwordItemService.GetAllAsync();
            var activeItems = allPasswordItems.Where(i => !i.IsDeleted && !i.IsArchived);
            
            // Filter items based on search text (case-insensitive)
            var filteredItems = activeItems.Where(item =>
                (item.Title?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (item.Username?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (item.Website?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (item.Notes?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false)
            ).ToList();

            // Update the collection
            AllPasswordItems.Clear();
            foreach (var item in filteredItems)
            {
                AllPasswordItems.Add(item);
            }

            // Clear selection if current item is not in filtered results
            if (SelectedPasswordItem != null && !filteredItems.Contains(SelectedPasswordItem))
            {
                SelectedPasswordItem = null;
            }

            StatusText = $"Found {filteredItems.Count} matches";
        }
        catch (Exception ex)
        {
            StatusText = $"Search error: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Search error: {ex}");
        }
    }
}