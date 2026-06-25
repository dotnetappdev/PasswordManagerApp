using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VaultGuard.Models;
using VaultGuard.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Windows.Shapes;

namespace VaultGuard.WPF.Dialogs;

public sealed partial class TagDialog : ModernWpf.Controls.ContentDialog
{
    private readonly ITagService? _tagService;
    private readonly IAuthService _authService;
    private Tag? _tag;
    private readonly bool _isEditMode;

    public Tag? Result { get; private set; }

    public event EventHandler<Tag?>? TagSaved;

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

        _authService = serviceProvider.GetRequiredService<IAuthService>();
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
            var ellipse = stackPanel?.Children[0] as Ellipse;
            if (ellipse?.Fill is SolidColorBrush colorBrush)
            {
                ColorPreview.Fill = colorBrush;
            }
        }
    }

    private async void TagDialog_PrimaryButtonClick(ModernWpf.Controls.ContentDialog sender, ModernWpf.Controls.ContentDialogButtonClickEventArgs args)
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

                // Set user ID from current authenticated user
                if (_authService.CurrentUser != null)
                {
                    _tag.UserId = _authService.CurrentUser.Id;
                }

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
                    LastModified = DateTime.UtcNow,
                    UserId = _authService.CurrentUser?.Id
                };

                if (_tagService != null)
                {
                    await _tagService.CreateAsync(newTag);
                }
                Result = newTag;
                TagSaved?.Invoke(this, Result);
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
        ErrorMessageText.Text = message;
        ErrorMessageBorder.Visibility = Visibility.Visible;

        // Auto-hide error after 5 seconds
        await Task.Delay(5000);
        ErrorMessageBorder.Visibility = Visibility.Collapsed;
    }
}
