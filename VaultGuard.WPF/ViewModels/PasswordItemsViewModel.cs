using System.Collections.ObjectModel;
using PasswordManager.Models;
using PasswordManager.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace PasswordManager.WPF.ViewModels;

public class PasswordItemsViewModel : BaseViewModel
{
    private readonly IPasswordItemService _passwordItemService;
    private string _searchText = string.Empty;
    private string _filterType = "All";
    private string? _filterTagName = null;
    private string? _filterCategoryName = null;
    private int? _selectedCategoryId = null;
    private int? _filterVaultId = null;
    private ObservableCollection<PasswordItem> _allItems = new();

    public PasswordItemsViewModel(IServiceProvider serviceProvider)
    {
        _passwordItemService = serviceProvider.GetRequiredService<IPasswordItemService>();
        PasswordItems = new ObservableCollection<PasswordItem>();
        // Do NOT fire-and-forget here — caller invokes RefreshAsync() explicitly after seeding
    }

    public ObservableCollection<PasswordItem> PasswordItems { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                // Use async/await properly instead of fire-and-forget
                _ = ApplyFiltersAsync();
            }
        }
    }

    public string FilterType
    {
        get => _filterType;
        set
        {
            if (SetProperty(ref _filterType, value))
            {
                _ = ApplyFiltersAsync();
            }
        }
    }

    public string? FilterTagName
    {
        get => _filterTagName;
        set
        {
            if (SetProperty(ref _filterTagName, value))
            {
                _ = ApplyFiltersAsync();
            }
        }
    }
    
    public string? FilterCategoryName
    {
        get => _filterCategoryName;
        set
        {
            if (SetProperty(ref _filterCategoryName, value))
            {
                _ = ApplyFiltersAsync();
            }
        }
    }

    public int? FilterVaultId
    {
        get => _filterVaultId;
        set
        {
            if (SetProperty(ref _filterVaultId, value))
            {
                _ = ApplyFiltersAsync();
            }
        }
    }

    public int? SelectedCategoryId
    {
        get => _selectedCategoryId;
        set
        {
            if (SetProperty(ref _selectedCategoryId, value))
            {
                _ = ApplyFiltersAsync();
            }
        }
    }

    public new bool IsLoading
    {
        get => base.IsLoading;
        set
        {
            base.IsLoading = value;
            OnPropertyChanged(nameof(HasNoItems));
        }
    }

    public bool HasNoItems => !IsLoading && PasswordItems.Count == 0;

    private async Task LoadPasswordItemsAsync()
    {
        try
        {
            IsLoading = true;
            var items = await _passwordItemService.GetAllAsync();

            _allItems.Clear();
            foreach (var item in items)
            {
                _allItems.Add(item);
            }

            await ApplyFiltersAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PasswordItemsViewModel] LoadPasswordItemsAsync failed: {ex}");
            ErrorMessage = $"Could not load items: {ex.Message}";
            OnPropertyChanged(nameof(HasError));
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasNoItems));
        }
    }

    private async Task ApplyFiltersAsync()
    {
        try
        {
            // Snapshot to a plain List on the UI thread before going to Task.Run.
            // ObservableCollection must not be accessed from a background thread.
            var snapshot = _allItems.ToList();

            var filteredItems = await Task.Run(() =>
            {
                var items = snapshot.AsEnumerable();

                // Apply default filters first (exclude deleted and archived unless specifically requested)
                if (FilterType != "Archive" && FilterType != "RecentlyDeleted")
                {
                    items = items.Where(item => !item.IsDeleted && !item.IsArchived);
                }

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    items = items.Where(item =>
                        item.Title?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true ||
                        item.Username?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true ||
                        item.Description?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true ||
                        item.Website?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true);
                }

                // Apply type filter
                if (FilterType != "All")
                {
                    switch (FilterType)
                    {
                        case "Favorites":
                            items = items.Where(item => item.IsFavorite && !item.IsDeleted && !item.IsArchived);
                            break;
                        case "Recent":
                            items = items.Where(item => !item.IsDeleted && !item.IsArchived)
                                .OrderByDescending(item => item.LastAccessedAt)
                                .Take(20);
                            break;
                        case "Archive":
                            items = items.Where(item => item.IsArchived && !item.IsDeleted);
                            break;
                        case "RecentlyDeleted":
                            items = items.Where(item => item.IsDeleted);
                            break;
                        default:
                            // Check if it's a valid ItemType
                            if (Enum.TryParse<ItemType>(FilterType, true, out var itemType))
                            {
                                items = items.Where(item =>
                                    item.Type == itemType && !item.IsDeleted && !item.IsArchived);
                            }
                            break;
                    }
                }

                // Apply category filter
                if (SelectedCategoryId.HasValue)
                {
                    items = items.Where(item => item.CategoryId == SelectedCategoryId.Value);
                }
                
                // Apply category name filter
                if (!string.IsNullOrWhiteSpace(FilterCategoryName))
                {
                    items = items.Where(item => 
                        item.Category != null && 
                        string.Equals(item.Category.Name, FilterCategoryName, StringComparison.OrdinalIgnoreCase));
                }

                // Apply vault filter (items belong to a vault via their Collection)
                if (FilterVaultId.HasValue)
                {
                    items = items.Where(item => item.Collection != null && item.Collection.VaultId == FilterVaultId.Value);
                }

                // Apply tag filter
                if (!string.IsNullOrWhiteSpace(FilterTagName))
                {
                    items = items.Where(item => item.Tags != null && item.Tags.Any(t => string.Equals(t.Name, FilterTagName, StringComparison.OrdinalIgnoreCase)));
                }

                return items.ToList();
            });

            // Update UI on main thread
            PasswordItems.Clear();
            foreach (var item in filteredItems)
            {
                PasswordItems.Add(item);
            }
            OnPropertyChanged(nameof(HasNoItems));
        }
        catch (Exception ex)
        {
        }
    }

    private async Task SearchPasswordItemsAsync()
    {
        await ApplyFiltersAsync();
    }

    public async Task RefreshAsync()
    {
        await LoadPasswordItemsAsync();
    }

    public async Task ApplyFilters()
    {
        await ApplyFiltersAsync();
    }

    public async Task DeleteItemAsync(PasswordItem item)
    {
        try
        {
            await _passwordItemService.DeleteAsync(item.Id);
            PasswordItems.Remove(item);
            _allItems.Remove(item); // Also remove from the cached all items list
            OnPropertyChanged(nameof(HasNoItems));
        }
        catch (Exception ex)
        {
        }
    }
}
