using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VaultGuard.Uno.Services.LocalDatabase;
using System.Collections.ObjectModel;

namespace VaultGuard.Mobile.Presentation.Pages.Categories;

public partial class CategoriesModel : ObservableObject
{
    private readonly LocalDatabaseService _localDatabase;
    private readonly ILogger<CategoriesModel> _logger;

    [ObservableProperty]
    private ObservableCollection<LocalCategory> categories = new();

    [ObservableProperty]
    private bool isLoading;

    public CategoriesModel(
        LocalDatabaseService localDatabase,
        ILogger<CategoriesModel> logger)
    {
        _localDatabase = localDatabase;
        _logger = logger;
    }

    public async Task LoadCategoriesAsync()
    {
        IsLoading = true;
        try
        {
            var items = await _localDatabase.GetAllCategoriesAsync();
            Categories.Clear();
            foreach (var item in items)
            {
                Categories.Add(item);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading categories");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
