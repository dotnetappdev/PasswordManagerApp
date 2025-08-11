using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Models;
using PasswordManager.Services.Interfaces;

namespace PasswordManager.WinUi.ViewModels;

public class CategoriesViewModel : BaseViewModel
{
    private readonly ICategoryInterface _categoryService;
    private readonly IPasswordItemService _passwordItemService;
    private string _searchText = string.Empty;
    private Category? _selectedCategory;

    public CategoriesViewModel(IServiceProvider serviceProvider)
    {
        _categoryService = serviceProvider.GetRequiredService<ICategoryInterface>();
        _passwordItemService = serviceProvider.GetRequiredService<IPasswordItemService>();
        
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
            
            // Load categories with password counts
            foreach (var category in categories)
            {
                // Get password count for this category
                var passwordItems = await _passwordItemService.GetAllAsync();
                var categoryPasswordCount = passwordItems.Count(p => p.CategoryId == category.Id && !p.IsDeleted && !p.IsArchived);
                
                // Store count in description field for display (this is a workaround)
                category.Description = $"{category.Description} ({categoryPasswordCount} passwords)";
                
                Categories.Add(category);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading categories: {ex.Message}");
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

            var createdCategory = await _categoryService.CreateAsync(category);
            
            if (createdCategory != null)
            {
                Categories.Add(createdCategory);
                OnPropertyChanged(nameof(HasNoCategories));
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating category: {ex.Message}");
            return false;
        }
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating category: {ex.Message}");
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<bool> DeleteCategoryAsync(Category category)
    {
        try
        {
            IsLoading = true;
            
            await _categoryService.DeleteAsync(category.Id);
            Categories.Remove(category);
            OnPropertyChanged(nameof(HasNoCategories));
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting category: {ex.Message}");
            return false;
        }
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