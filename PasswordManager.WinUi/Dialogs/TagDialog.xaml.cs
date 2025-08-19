using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using PasswordManager.Models;
using PasswordManager.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace PasswordManager.WinUi.Dialogs;

public sealed partial class TagDialog : ContentDialog
{
    private readonly ITagService? _tagService;
    private Tag? _tag;
    private readonly bool _isEditMode;

    public Tag? Result { get; private set; }

    public TagDialog(IServiceProvider serviceProvider, Tag? tag = null)
    {
        this.InitializeComponent();

        // Try to get tag service if available
        try
        {
            _tagService = serviceProvider.GetService<ITagService>();
        }
        catch
        {
            _tagService = null;
        }

        _tag = tag;
        _isEditMode = tag != null;

        // Update dialog title
        Title = _isEditMode ? "Edit Tag" : "Add Tag";
        PrimaryButtonText = _isEditMode ? "Update" : "Create";

        // Wire up events
        TagColorComboBox.SelectionChanged += TagColorComboBox_SelectionChanged;
        this.PrimaryButtonClick += TagDialog_PrimaryButtonClick;

        // Load existing data if editing
        if (_isEditMode && _tag != null)
        {
            LoadTagData();
        }
    }

    private void LoadTagData()
    {
        if (_tag == null) return;

        TagNameTextBox.Text = _tag.Name;
        TagDescriptionTextBox.Text = _tag.Description ?? string.Empty;

        // Set default color
        TagColorComboBox.SelectedIndex = 0; // Default to blue
    }

    private void TagColorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TagColorComboBox.SelectedItem is ComboBoxItem selectedItem)
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

    private async void TagDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Validate input
        var name = TagNameTextBox.Text?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            args.Cancel = true;
            await ShowErrorMessage("Tag name is required.");
            return;
        }

        // Show loading
        this.IsPrimaryButtonEnabled = false;

        try
        {
            if (_isEditMode && _tag != null)
            {
                // Update existing tag
                _tag.Name = name;
                _tag.Description = TagDescriptionTextBox.Text?.Trim();
                _tag.LastModified = DateTime.UtcNow;

                if (_tagService != null)
                {
                    await _tagService.UpdateAsync(_tag);
                }
                Result = _tag;
            }
            else
            {
                // Create new tag
                var newTag = new Tag
                {
                    Name = name,
                    Description = TagDescriptionTextBox.Text?.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };

                if (_tagService != null)
                {
                    await _tagService.CreateAsync(newTag);
                }
                Result = newTag;
            }
        }
        catch (Exception ex)
        {
            args.Cancel = true;
            await ShowErrorMessage($"Error saving tag: {ex.Message}");
        }
        finally
        {
            this.IsPrimaryButtonEnabled = true;
        }
    }

    private async Task ShowErrorMessage(string message)
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