using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.WinUi.ViewModels;
using PasswordManager.Models;
using System;
using System.Threading.Tasks;

namespace PasswordManager.WinUi.Views;

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
    private Microsoft.UI.Xaml.XamlRoot? GetMainXamlRoot()
    {
        return (App.Current as App)?.MainWindow?.Content?.XamlRoot;
    }

    /// <summary>
    /// Helper method to properly configure dialog for centering
    /// </summary>
    private void ConfigureDialogForCentering(ContentDialog dialog)
    {
        try
        {
            // Set XamlRoot to the main window's content for proper centering
            var mainXamlRoot = GetMainXamlRoot();
            if (mainXamlRoot != null)
            {
                dialog.XamlRoot = mainXamlRoot;
            }
            else if (this.XamlRoot != null)
            {
                dialog.XamlRoot = this.XamlRoot;
            }
            
            // Ensure the dialog uses the proper style for centering if it doesn't have one already
            if (dialog.Style == null)
            {
                // Apply the Modern1PasswordDialogStyle from resources
                if (Application.Current.Resources.ContainsKey("Modern1PasswordDialogStyle"))
                {
                    dialog.Style = Application.Current.Resources["Modern1PasswordDialogStyle"] as Style;
                }
            }
        }
        catch (Exception ex)
        {
            // Fallback to page XamlRoot
            if (this.XamlRoot != null)
            {
                dialog.XamlRoot = this.XamlRoot;
            }
        }
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        
        if (e.Parameter is IServiceProvider serviceProvider)
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
            var dialog = new ContentDialog
            {
                Title = "Create New Vault",
                PrimaryButtonText = "Create",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary
            };

            ConfigureDialogForCentering(dialog);

            var nameTextBox = new TextBox
            {
                PlaceholderText = "Vault name",
                Margin = new Thickness(0, 0, 0, 16)
            };

            var descriptionTextBox = new TextBox
            {
                PlaceholderText = "Description (optional)",
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Height = 80
            };

            var stackPanel = new StackPanel
            {
                Spacing = 12
            };
            stackPanel.Children.Add(new TextBlock { Text = "Name", Margin = new Thickness(0, 0, 0, 4) });
            stackPanel.Children.Add(nameTextBox);
            stackPanel.Children.Add(new TextBlock { Text = "Description", Margin = new Thickness(0, 8, 0, 4) });
            stackPanel.Children.Add(descriptionTextBox);

            dialog.Content = stackPanel;

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && _viewModel != null)
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
                var dialog = new ContentDialog
                {
                    Title = "Edit Vault",
                    PrimaryButtonText = "Save",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Primary
                };

                ConfigureDialogForCentering(dialog);

                var nameTextBox = new TextBox
                {
                    Text = vault.Name,
                    PlaceholderText = "Vault name",
                    Margin = new Thickness(0, 0, 0, 16)
                };

                var descriptionTextBox = new TextBox
                {
                    Text = vault.Description,
                    PlaceholderText = "Description (optional)",
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap,
                    Height = 80
                };

                var stackPanel = new StackPanel
                {
                    Spacing = 12
                };
                stackPanel.Children.Add(new TextBlock { Text = "Name", Margin = new Thickness(0, 0, 0, 4) });
                stackPanel.Children.Add(nameTextBox);
                stackPanel.Children.Add(new TextBlock { Text = "Description", Margin = new Thickness(0, 8, 0, 4) });
                stackPanel.Children.Add(descriptionTextBox);

                dialog.Content = stackPanel;

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
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
                var confirmDialog = new ContentDialog
                {
                    Title = "Delete Vault",
                    Content = $"Are you sure you want to delete '{vault.Name}'? Items in this vault will be moved to the default vault.",
                    PrimaryButtonText = "Delete",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Close
                };

                ConfigureDialogForCentering(confirmDialog);

                var result = await confirmDialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
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
        var dialog = new ContentDialog
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
        var dialog = new ContentDialog
        {
            Title = "Success",
            Content = message,
            CloseButtonText = "OK"
        };

        ConfigureDialogForCentering(dialog);
        await dialog.ShowAsync();
    }
}
