using Microsoft.UI.Xaml.Controls;
using PasswordManager.Models;

namespace PasswordManager.WinUi.Dialogs;

public sealed partial class ItemTypeSelectionDialog : ContentDialog
{
    public ItemType? SelectedItemType { get; private set; }

    public ItemTypeSelectionDialog()
    {
        this.InitializeComponent();
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (LoginRadioButton.IsChecked == true)
            SelectedItemType = ItemType.Login;
        else if (PasskeyRadioButton.IsChecked == true)
            SelectedItemType = ItemType.Passkey;
        else if (CreditCardRadioButton.IsChecked == true)
            SelectedItemType = ItemType.CreditCard;
        else if (SecureNoteRadioButton.IsChecked == true)
            SelectedItemType = ItemType.SecureNote;
        else if (WiFiRadioButton.IsChecked == true)
            SelectedItemType = ItemType.WiFi;
        else if (PasswordRadioButton.IsChecked == true)
            SelectedItemType = ItemType.Password;
        else
            SelectedItemType = ItemType.Login; // Default fallback
    }
}