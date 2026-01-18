using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.WPF.ViewModels;
using PasswordManager.Models;
using System;
using System.Threading.Tasks;

namespace PasswordManager.WPF.Views;

public sealed partial class VaultsPage : Page
{
    private VaultsViewModel? _viewModel;
    private IServiceProvider? _serviceProvider;

    public VaultsPage()
    {
        this.InitializeComponent();
    }

    public VaultsViewModel? ViewModel => _viewModel;

    // Helper to prefer the main window XamlRoot so dialogs center on the app window
    // WPF: null removed - not needed

    /// <summary>
    /// Helper method to properly configure dialog for centering
    /// </summary>
    private void ConfigureDialogForCentering(ModernWpf.Controls.ContentDialog dialog)
    {
        try
        {
            // WPF: XamlRoot not needed in WPF
            
            // Ensure the dialog uses the proper style for centering if it doesn't have one already
            if (dialog.Style == null)
            {
                // Apply the Modern1PasswordDialogStyle from resources
                if (Application.Current.Resources.Contains("Modern1PasswordDialogStyle"))
                {
                    dialog.Style = Application.Current.Resources["Modern1PasswordDialogStyle"] as Style;
                }
            }
        }
        catch (Exception ex)
        {
            // Fallback - no action needed in WPF
        }
    }

    public void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
    {
        // Note: WPF Page doesn't have base.OnNavigatedTo
        if (e.ExtraData is IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _viewModel = new VaultsViewModel(serviceProvider);
            this.DataContext = _viewModel;
        }
    }

    private async void AddVaultButton_Click(object sender, RoutedEventArgs e)
    {
        if (_serviceProvider == null) return;

        try
        {
            var dialog = new ModernWpf.Controls.ContentDialog
            {
                Title = "Create New Vault",
                PrimaryButtonText = "Create",
                CloseButtonText = "Cancel",
                DefaultButton = ModernWpf.Controls.ContentDialogButton.Primary
            };

            ConfigureDialogForCentering(dialog);

            var nameTextBox = new TextBox
            {
                // PlaceholderText = "Vault name",
                Margin = new Thickness(0, 0, 0, 16)
            };

            var descriptionTextBox = new TextBox
            {
                // PlaceholderText = "Description (optional)",
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Height = 80
            };

            var stackPanel = new StackPanel
            { };
            stackPanel.Children.Add(new TextBlock { Text = "Name", Margin = new Thickness(0, 0, 0, 4) });
            stackPanel.Children.Add(nameTextBox);
            stackPanel.Children.Add(new TextBlock { Text = "Description", Margin = new Thickness(0, 8, 0, 4) });
            stackPanel.Children.Add(descriptionTextBox);

            dialog.Content = stackPanel;

            var result = await dialog.ShowAsync();
            if (result == ModernWpf.Controls.ContentDialogResult.Primary && _viewModel != null)
            {
                var name = nameTextBox.Text?.Trim();
                if (!string.IsNullOrEmpty(name))
                {
                    var success = await _viewModel.CreateVaultAsync(name, descriptionTextBox.Text?.Trim());
                    if (success)
                    {
                        await ShowSuccessDialog("Vault created successfully");
                    }
                    else
                    {
                        await ShowErrorDialog("Failed to create vault");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await ShowErrorDialog($"Error adding vault: {ex.Message}");
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            await _viewModel.RefreshAsync();
        }
    }

    private async void EditVaultButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Vault vault && _viewModel != null)
        {
            try
            {
                var dialog = new ModernWpf.Controls.ContentDialog
                {
                    Title = "Edit Vault",
                    PrimaryButtonText = "Save",
                    CloseButtonText = "Cancel",
                    DefaultButton = ModernWpf.Controls.ContentDialogButton.Primary
                };

                ConfigureDialogForCentering(dialog);

                var nameTextBox = new TextBox
                {
                    Text = vault.Name,
                    // PlaceholderText = "Vault name",
                    Margin = new Thickness(0, 0, 0, 16)
                };

                var descriptionTextBox = new TextBox
                {
                    Text = vault.Description,
                    // PlaceholderText = "Description (optional)",
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap,
                    Height = 80
                };

                var stackPanel = new StackPanel
                { };
                stackPanel.Children.Add(new TextBlock { Text = "Name", Margin = new Thickness(0, 0, 0, 4) });
                stackPanel.Children.Add(nameTextBox);
                stackPanel.Children.Add(new TextBlock { Text = "Description", Margin = new Thickness(0, 8, 0, 4) });
                stackPanel.Children.Add(descriptionTextBox);

                dialog.Content = stackPanel;

                var result = await dialog.ShowAsync();
                if (result == ModernWpf.Controls.ContentDialogResult.Primary)
                {
                    var name = nameTextBox.Text?.Trim();
                    if (!string.IsNullOrEmpty(name))
                    {
                        vault.Name = name;
                        vault.Description = descriptionTextBox.Text?.Trim();
                        
                        var success = await _viewModel.UpdateVaultAsync(vault);
                        if (success)
                        {
                            await ShowSuccessDialog("Vault updated successfully");
                        }
                        else
                        {
                            await ShowErrorDialog("Failed to update vault");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialog($"Error editing vault: {ex.Message}");
            }
        }
    }

    private async void DeleteVaultButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Vault vault && _viewModel != null)
        {
            try
            {
                var confirmDialog = new ModernWpf.Controls.ContentDialog
                {
                    Title = "Delete Vault",
                    Content = $"Are you sure you want to delete '{vault.Name}'? Items in this vault will be moved to the default vault.",
                    PrimaryButtonText = "Delete",
                    CloseButtonText = "Cancel",
                    DefaultButton = ModernWpf.Controls.ContentDialogButton.Close
                };

                ConfigureDialogForCentering(confirmDialog);

                var result = await confirmDialog.ShowAsync();
                if (result == ModernWpf.Controls.ContentDialogResult.Primary)
                {
                    var success = await _viewModel.DeleteVaultAsync(vault);
                    if (success)
                    {
                        await ShowSuccessDialog("Vault deleted successfully");
                    }
                    else
                    {
                        await ShowErrorDialog("Failed to delete vault");
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialog($"Error deleting vault: {ex.Message}");
            }
        }
    }

    private async Task ShowErrorDialog(string message)
    {
        var dialog = new ModernWpf.Controls.ContentDialog
        {
            Title = "Error",
            Content = message,
            CloseButtonText = "OK"
        };

        ConfigureDialogForCentering(dialog);
        await dialog.ShowAsync();
    }

    private async Task ShowSuccessDialog(string message)
    {
        var dialog = new ModernWpf.Controls.ContentDialog
        {
            Title = "Success",
            Content = message,
            CloseButtonText = "OK"
        };

        ConfigureDialogForCentering(dialog);
        await dialog.ShowAsync();
    }
}
