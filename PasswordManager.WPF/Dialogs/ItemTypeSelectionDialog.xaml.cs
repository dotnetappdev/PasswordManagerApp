using System.Windows.Controls;
using System.Windows;
using PasswordManager.Models;

namespace PasswordManager.WPF.Dialogs;

public sealed partial class ItemTypeSelectionDialog : ModernWpf.Controls.ContentDialog
{
    public ItemType? SelectedItemType { get; private set; }
    public string? SelectedCategoryName { get; private set; }

    public ItemTypeSelectionDialog()
    {
        InitializeComponent();
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
                "WiFi" => ItemType.WiFi,
                "Password" => ItemType.Password,
                "Passkey" => ItemType.Passkey,
                "Identity" => ItemType.Identity,
                _ => ItemType.Login // Default fallback
            };

            this.Hide();
        }
    }

    private void ContentDialog_PrimaryButtonClick(ModernWpf.Controls.ContentDialog sender, ModernWpf.Controls.ContentDialogButtonClickEventArgs args)
    {
        // Handle primary button if needed - categories are selected via button clicks now
        if (SelectedItemType == null)
        {
            SelectedItemType = ItemType.Login; // Default fallback
            SelectedCategoryName = "Login";
        }
    }
}
