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

            SelectedItemType = categoryName switch
            {
                "Login"                  => ItemType.Login,
                "Credit Card"            => ItemType.CreditCard,
                "Secure Note"            => ItemType.SecureNote,
                "WiFi"                   => ItemType.WiFi,
                "Password"               => ItemType.Password,
                "Passkey"                => ItemType.Passkey,
                "Identity"               => ItemType.Identity,
                "SSH Key"                => ItemType.SshKey,
                "API Credentials"        => ItemType.ApiCredentials,
                "Bank Account"           => ItemType.BankAccount,
                "Crypto Wallet"          => ItemType.CryptoWallet,
                "Database"               => ItemType.Database,
                "Driver License"         => ItemType.DriversLicense,
                "Email Account"          => ItemType.EmailAccount,
                "Medical Record"         => ItemType.MedicalRecord,
                "Membership"             => ItemType.Membership,
                "Outdoor License"        => ItemType.OutdoorLicense,
                "Passport"               => ItemType.Passport,
                "Rewards"                => ItemType.RewardsProgram,
                "Server"                 => ItemType.Server,
                "Social Security Number" => ItemType.SocialSecurityNumber,
                "Software License"       => ItemType.SoftwareLicense,
                "Wireless Router"        => ItemType.WirelessRouter,
                "Document"               => ItemType.Document,
                _                        => ItemType.Login
            };

            this.Hide();
        }
    }

    private void ShowMoreButton_Click(object sender, RoutedEventArgs e)
    {
        var isVisible = ExtendedTypesPanel.Visibility == Visibility.Visible;
        ExtendedTypesPanel.Visibility = isVisible ? Visibility.Collapsed : Visibility.Visible;
        ShowMoreText.Text   = isVisible ? "Show more" : "Show less";
        ShowMoreChevron.Text = isVisible ? " " : " ";
    }

    private void ContentDialog_PrimaryButtonClick(ModernWpf.Controls.ContentDialog sender, ModernWpf.Controls.ContentDialogButtonClickEventArgs args)
    {
        if (SelectedItemType == null)
        {
            SelectedItemType = ItemType.Login;
            SelectedCategoryName = "Login";
        }
    }
}
