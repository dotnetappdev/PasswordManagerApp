using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using VaultGuard.Models;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.WPF.ViewModels;

public class CategoriesViewModel : BaseViewModel
{
    private readonly ICategoryInterface _categoryService;
    private readonly IPasswordItemService _passwordItemService;
    private readonly IAuthService _authService;
    private readonly IServiceProvider _serviceProvider;
    private string _searchText = string.Empty;
    private Category? _selectedCategory;

    public CategoriesViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _categoryService = serviceProvider.GetRequiredService<ICategoryInterface>();
        _passwordItemService = serviceProvider.GetRequiredService<IPasswordItemService>();
        _authService = serviceProvider.GetRequiredService<IAuthService>();
        
        Categories = new ObservableCollection<Category>();
        
        LoadCategoriesAsync();
    }

    public ObservableCollection<Category> Categories { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            SetProperty(ref _searchText, value);
            FilterCategories();
        }
    }

    public Category? SelectedCategory
    {
        get => _selectedCategory;
        set => SetProperty(ref _selectedCategory, value);
    }

    public bool HasNoCategories => !IsLoading && Categories.Count == 0;

    private async Task LoadCategoriesAsync()
    {
        try
        {
            IsLoading = true;
            
            var categories = await _categoryService.GetAllAsync();

            Categories.Clear();

            // Note: Password counts should be displayed in the UI via separate service calls
            // or computed properties, not by mutating the Category model's Description field
            foreach (var category in categories)
            {
                Categories.Add(category);
            }
        }
        catch (Exception ex)
        {
            VaultGuard.Services.Logging.AppLogger.Error($"Failed to load categories", ex);
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasNoCategories));
        }
    }

    private void FilterCategories()
    {
        // This would need a more sophisticated implementation
        // For now, we'll reload all categories when search changes
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            _ = LoadCategoriesAsync();
        }
    }

    public async Task<bool> CreateCategoryAsync(string name, string? description = null)
    {
        try
        {
            IsLoading = true;
            
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            var category = new Category
            {
                Name = name.Trim(),
                Description = description?.Trim()
            };

            // Set user ID from current authenticated user
            if (_authService.CurrentUser != null)
            {
                category.UserId = _authService.CurrentUser.Id;
            }

            var createdCategory = await _categoryService.CreateAsync(category);
            
            if (createdCategory != null)
            {
                Categories.Add(createdCategory);
                OnPropertyChanged(nameof(HasNoCategories));
                return true;
            }
            
            return false;
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Warning("Recovered from a suppressed exception", ex); return false; }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<bool> UpdateCategoryAsync(Category category)
    {
        try
        {
            IsLoading = true;
            
            var updatedCategory = await _categoryService.UpdateAsync(category);
            
            if (updatedCategory != null)
            {
                // Update the item in the collection
                var index = Categories.ToList().FindIndex(c => c.Id == category.Id);
                if (index >= 0)
                {
                    Categories[index] = updatedCategory;
                }
                return true;
            }
            
            return false;
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Warning("Recovered from a suppressed exception", ex); return false; }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<bool> DeleteCategoryAsync(Category category)
    {
        try
        {
            if (!await VaultGuard.WPF.Helpers.SecurityGateHelper.RequireCodeForActionAsync(
                    _serviceProvider, VaultGuard.WPF.Helpers.SecurityGateHelper.GateAction.CategoryDelete))
            {
                return false;
            }

            IsLoading = true;

            await _categoryService.DeleteAsync(category.Id);
            Categories.Remove(category);
            OnPropertyChanged(nameof(HasNoCategories));
            return true;
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Warning("Recovered from a suppressed exception", ex); return false; }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task RefreshAsync()
    {
        await LoadCategoriesAsync();
    }
}
