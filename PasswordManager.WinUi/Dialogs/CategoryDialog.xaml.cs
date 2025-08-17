using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using PasswordManager.Models;
using PasswordManager.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace PasswordManager.WinUi.Dialogs;

public sealed partial class CategoryDialog : ContentDialog
{
    private readonly ICategoryInterface _categoryService;
    private Category? _category;
    private readonly bool _isEditMode;

    public Category? Result { get; private set; }

    public CategoryDialog(IServiceProvider serviceProvider, Category? category = null)
    {
    this.InitializeComponent();
    _categoryService = serviceProvider.GetRequiredService<ICategoryInterface>();
        _category = category;
        _isEditMode = category != null;

        // Update dialog title
        Title = _isEditMode ? "Edit Category" : "Add Category";
        PrimaryButtonText = _isEditMode ? "Update" : "Create";

        // Wire up events
        CategoryColorComboBox.SelectionChanged += CategoryColorComboBox_SelectionChanged;
        this.PrimaryButtonClick += CategoryDialog_PrimaryButtonClick;

        // Load existing data if editing
        if (_isEditMode && _category != null)
        {
            LoadCategoryData();
        }
    }

    private void LoadCategoryData()
    {
        if (_category == null) return;

        CategoryNameTextBox.Text = _category.Name;
        CategoryDescriptionTextBox.Text = _category.Description ?? string.Empty;

        // Set color (simplified for now - you can enhance this)
        CategoryColorComboBox.SelectedIndex = 0; // Default to blue

        // Set icon (simplified for now)
        CategoryIconComboBox.SelectedIndex = 0; // Default to folder
    }

    private void CategoryColorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CategoryColorComboBox.SelectedItem is ComboBoxItem selectedItem)
        {
            // Extract color from the selected item
            var stackPanel = selectedItem.Content as StackPanel;
            var border = stackPanel?.Children[0] as Border;
            if (border?.Background is SolidColorBrush colorBrush)
            {
                ColorPreview.Background = colorBrush;
            }
        }
    }

    private async void CategoryDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Validate input
        var name = CategoryNameTextBox.Text?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            args.Cancel = true;
            ShowErrorMessage("Category name is required.");
            return;
        }

        // Show loading
        this.IsPrimaryButtonEnabled = false;

        try
        {
            if (_isEditMode && _category != null)
            {
                // Update existing category
                _category.Name = name;
                _category.Description = CategoryDescriptionTextBox.Text?.Trim();
                _category.LastModified = DateTime.UtcNow;

                await _categoryService.UpdateAsync(_category);
                Result = _category;
            }
            else
            {
                // Create new category
                var newCategory = new Category
                {
                    Name = name,
                    Description = CategoryDescriptionTextBox.Text?.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };

                await _categoryService.CreateAsync(newCategory);
                Result = newCategory;
            }
        }
        catch (Exception ex)
        {
            args.Cancel = true;
            ShowErrorMessage($"Error saving category: {ex.Message}");
        }
        finally
        {
            this.IsPrimaryButtonEnabled = true;
        }
    }

    private async void ShowErrorMessage(string message)
    {
        var errorDialog = new ContentDialog
        {
            Title = "Error",
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };
        await errorDialog.ShowAsync();
    }
}