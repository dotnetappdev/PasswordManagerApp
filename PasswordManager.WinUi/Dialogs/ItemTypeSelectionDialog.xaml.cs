using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using PasswordManager.Models;

namespace PasswordManager.WinUi.Dialogs;

public sealed partial class ItemTypeSelectionDialog : ContentDialog
{
    public ItemType? SelectedItemType { get; private set; }
    public string? SelectedCategoryName { get; private set; }
    private bool _showExtendedCategories = false;

    public ItemTypeSelectionDialog()
    {
        this.InitializeComponent();
        UpdateShowMoreButton();
    }

    private void CategoryButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string categoryName)
        {
            SelectedCategoryName = categoryName;
            
            // Map category name to ItemType
            SelectedItemType = categoryName switch
            {
                "Login" => ItemType.Login,
                "Credit Card" => ItemType.CreditCard,
                "Secure Note" => ItemType.SecureNote,
                "Identity" => ItemType.SecureNote, // Use SecureNote for identity items
                "Password" => ItemType.Password,
                "Document" => ItemType.SecureNote, // Use SecureNote for documents
                _ => ItemType.Login // Default fallback
            };
            
            this.Hide();
        }
    }

    private void ShowMoreButton_Click(object sender, RoutedEventArgs e)
    {
        _showExtendedCategories = !_showExtendedCategories;
        ExtendedCategoriesPanel.Visibility = _showExtendedCategories ? Visibility.Visible : Visibility.Collapsed;
        UpdateShowMoreButton();
    }

    private void UpdateShowMoreButton()
    {
        ShowMoreButton.Content = _showExtendedCategories ? "Show less" : "Show more";
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Handle primary button if needed - categories are selected via button clicks now
        if (SelectedItemType == null)
        {
            SelectedItemType = ItemType.Login; // Default fallback
            SelectedCategoryName = "Login";
        }
    }
}