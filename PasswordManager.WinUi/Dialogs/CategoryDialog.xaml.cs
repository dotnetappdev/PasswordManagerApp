using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using PasswordManager.Models;
using PasswordManager.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Shapes;
using Windows.System;

namespace PasswordManager.WinUi.Dialogs;

public sealed partial class CategoryDialog : ContentDialog
{
    private readonly ICategoryInterface _categoryService;
    private readonly IAuthService _authService;
    private Category? _category;
    private readonly bool _isEditMode;

    public Category? Result { get; private set; }

    // Event fired when a category is successfully created or updated
    public event EventHandler<Category?>? CategorySaved;

    public CategoryDialog(IServiceProvider serviceProvider, Category? category = null)
    {
        this.InitializeComponent();
        _categoryService = serviceProvider.GetRequiredService<ICategoryInterface>();
        _authService = serviceProvider.GetRequiredService<IAuthService>();
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

        // Set color based on existing value
        var colorIndex = _category.Color switch
        {
            "#3b82f6" => 0, // Blue
            "#10b981" => 1, // Green
            "#f59e0b" => 2, // Orange
            "#ef4444" => 3, // Red
            "#8b5cf6" => 4, // Purple
            "#ec4899" => 5, // Pink
            "#6b7280" => 6, // Gray
            _ => 0          // Default to blue
        };
        CategoryColorComboBox.SelectedIndex = colorIndex;

        // Set icon based on existing glyph value
        var iconIndex = _category.Icon switch
        {
            "\uE8B7" => 0, // Folder
            "\uE72E" => 1, // Key
            "\uE8C7" => 2, // Credit Card
            "\uE70B" => 3, // Note
            "\uE701" => 4, // WiFi
            "\uE72F" => 5, // Security (Lock)
            "\uE734" => 6, // Star
            "\uE8A5" => 7, // List
            _ => 0     // Default to folder
        };
        CategoryIconComboBox.SelectedIndex = iconIndex;
    }

    private void CategoryColorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CategoryColorComboBox.SelectedItem is ComboBoxItem selectedItem)
        {
            // Extract color from the selected item
            var stackPanel = selectedItem.Content as StackPanel;
            var ellipse = stackPanel?.Children[0] as Ellipse;
            if (ellipse?.Fill is SolidColorBrush colorBrush)
            {
                ColorPreview.Fill = colorBrush;
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
                _category.Color = GetSelectedColor();
                _category.Icon = GetSelectedIcon();
                _category.UpdatedAt = DateTime.UtcNow;
                _category.LastModified = DateTime.UtcNow;

                // Set user ID from current authenticated user
                if (_authService.CurrentUser != null)
                {
                    _category.UserId = _authService.CurrentUser.Id;
                }

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
                    Color = GetSelectedColor(),
                    Icon = GetSelectedIcon(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    UserId = _authService.CurrentUser?.Id

                };
                var TEST = newCategory;
                await _categoryService.CreateAsync(newCategory);
                Result = newCategory;
                // Notify listeners that a category was created
                CategorySaved?.Invoke(this, Result);
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
        ErrorMessageText.Text = message;
        ErrorMessageBorder.Visibility = Visibility.Visible;

        // Auto-hide error after 5 seconds
        await Task.Delay(5000);
        ErrorMessageBorder.Visibility = Visibility.Collapsed;
    }

    private string GetSelectedColor()
    {
        var selectedIndex = CategoryColorComboBox.SelectedIndex;
        return selectedIndex switch
        {
            0 => "#3b82f6", // Blue
            1 => "#10b981", // Green
            2 => "#f59e0b", // Orange
            3 => "#ef4444", // Red
            4 => "#8b5cf6", // Purple
            5 => "#ec4899", // Pink
            6 => "#6b7280", // Gray
            _ => "#3b82f6"  // Default to blue
        };
    }

    private string GetSelectedIcon()
    {
        var selectedIndex = CategoryIconComboBox.SelectedIndex;
        // Return Segoe MDL2 glyph character string
        return selectedIndex switch
        {
            0 => "\uE8B7", // Folder
            1 => "\uE72E", // Key
            2 => "\uE8C7", // Credit Card
            3 => "\uE70B", // Note
            4 => "\uE701", // WiFi
            5 => "\uE72F", // Security (Lock)
            6 => "\uE734", // Star
            7 => "\uE8A5", // List
            _ => "\uE8B7"  // Default to folder
        };
    }
}