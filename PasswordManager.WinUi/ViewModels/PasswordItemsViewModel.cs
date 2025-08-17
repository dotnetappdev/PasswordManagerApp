using System.Collections.ObjectModel;
using PasswordManager.Models;
using PasswordManager.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace PasswordManager.WinUi.ViewModels;

public class PasswordItemsViewModel : BaseViewModel
{
    private readonly IPasswordItemService _passwordItemService;
    private string _searchText = string.Empty;
    private string _filterType = "All";
    private string _selectedCategory = "All Categories";
    private ObservableCollection<PasswordItem> _allItems = new();

    public PasswordItemsViewModel(IServiceProvider serviceProvider)
    {
        _passwordItemService = serviceProvider.GetRequiredService<IPasswordItemService>();
        PasswordItems = new ObservableCollection<PasswordItem>();
        LoadPasswordItemsAsync();
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

    public string SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
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
            foreach (var item in items.Where(i => !i.IsDeleted && !i.IsArchived))
            {
                _allItems.Add(item);
            }
            
            await ApplyFiltersAsync();
        }
        catch (Exception ex)
        {
            // Handle error - could show a message to user
            System.Diagnostics.Debug.WriteLine($"Error loading password items: {ex.Message}");
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
            var filteredItems = await Task.Run(() =>
            {
                var items = _allItems.AsEnumerable();

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
                            items = items.Where(item => item.IsFavorite);
                            break;
                        case "Recent":
                            items = items.OrderByDescending(item => item.LastAccessedAt)
                                .Take(20);
                            break;
                        default:
                            items = items.Where(item => 
                                string.Equals(item.Type.ToString(), FilterType, StringComparison.OrdinalIgnoreCase));
                            break;
                    }
                }

                // Apply category filter
                if (SelectedCategory != "All Categories" && !string.IsNullOrEmpty(SelectedCategory))
                {
                    items = items.Where(item =>
                        item.Category != null && 
                        string.Equals(item.Category.Name, SelectedCategory, StringComparison.OrdinalIgnoreCase));
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
            System.Diagnostics.Debug.WriteLine($"Error applying filters: {ex.Message}");
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

    public async Task DeleteItemAsync(PasswordItem item)
    {
        try
        {
            await _passwordItemService.DeleteAsync(item.Id);
            PasswordItems.Remove(item);
            OnPropertyChanged(nameof(HasNoItems));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting password item: {ex.Message}");
        }
    }
}