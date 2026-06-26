using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using VaultGuard.WPF;
using VaultGuard.WPF.ViewModels;
using VaultGuard.WPF.Services;
using VaultGuard.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MwControls = ModernWpf.Controls;

namespace VaultGuard.WPF.Views;

public sealed partial class VaultsPage : Page
{
    private VaultsViewModel? _viewModel;
    private IServiceProvider? _serviceProvider;

    public VaultsPage()
    {
        this.InitializeComponent();
    }

    public VaultsViewModel? ViewModel => _viewModel;

    public void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
    {
        if (e.ExtraData is IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _viewModel = new VaultsViewModel(serviceProvider);
            this.DataContext = this;
            _viewModel.PropertyChanged += (_, _) => UpdateTotalItemsText();
        }
    }

    private void UpdateTotalItemsText()
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (TotalItemsText == null || _viewModel == null) return;
            var total = _viewModel.Vaults.Sum(v => v.ItemCount);
            TotalItemsText.Text = total.ToString();
        });
    }

    private void ConfigureDialogForCentering(MwControls.ContentDialog dialog)
    {
        try
        {
            dialog.Style = dialog.TryFindResource("Modern1PasswordDialogStyle") as Style;
        }
        catch (System.Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Suppressed exception", logEx); }
    }

    private async void AddVaultButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null) return;
        try
        {
            var (name, description, color, icon) = await ShowVaultDialogAsync("Create New Vault");
            if (name == null) return;

            var success = await _viewModel.CreateVaultAsync(name, description, color, icon);
            if (!success)
                ToastService.Instance.Error("Failed to create vault");
            else
            {
                ToastService.Instance.Success($"Vault \"{name}\" created");
                UpdateTotalItemsText();
                _ = (Application.Current.MainWindow as MainWindow)?.RefreshVaultsNavAsync();
            }
        }
        catch (Exception ex) { await ShowMsgAsync("Error", ex.Message); }
    }

    private async void EditVaultButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: Vault vault } || _viewModel == null) return;
        try
        {
            var (name, description, color, icon) = await ShowVaultDialogAsync("Edit Vault",
                vault.Name, vault.Description, vault.Color, vault.Icon);
            if (name == null) return;

            vault.Name = name;
            vault.Description = description;
            vault.Color = string.IsNullOrWhiteSpace(color) ? vault.Color : color;
            vault.Icon = string.IsNullOrWhiteSpace(icon) ? vault.Icon : icon;

            await _viewModel.UpdateVaultAsync(vault);
            _ = (Application.Current.MainWindow as MainWindow)?.RefreshVaultsNavAsync();
        }
        catch (Exception ex) { await ShowMsgAsync("Error", ex.Message); }
    }

    private async void DeleteVaultButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: Vault vault } || _viewModel == null) return;
        try
        {
            var confirm = new MwControls.ContentDialog
            {
                Title = "Delete Vault",
                Content = $"Delete \"{vault.Name}\"? Items in this vault will be moved to the default vault.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = MwControls.ContentDialogButton.Close
            };
            ConfigureDialogForCentering(confirm);
            if (await confirm.ShowAsync() == MwControls.ContentDialogResult.Primary)
            {
                // Optional step-up: when "require authenticator code on vault delete" is enabled and
                // the user has 2FA, they must enter a current TOTP/recovery code before we delete.
                if (_serviceProvider != null &&
                    !await Helpers.SecurityGateHelper.RequireCodeForActionAsync(
                        _serviceProvider, Helpers.SecurityGateHelper.GateAction.VaultDelete))
                {
                    return;
                }

                await _viewModel.DeleteVaultAsync(vault);
                ToastService.Instance.Success($"Vault \"{vault.Name}\" deleted");
                _ = (Application.Current.MainWindow as MainWindow)?.RefreshVaultsNavAsync();
            }
        }
        catch (Exception ex) { await ShowMsgAsync("Error", ex.Message); }
    }

    private async void SetDefaultButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: Vault vault } || _viewModel == null) return;
        try
        {
            await _viewModel.SetDefaultVaultAsync(vault);
        }
        catch (Exception ex) { await ShowMsgAsync("Error", ex.Message); }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null) { await _viewModel.RefreshAsync(); UpdateTotalItemsText(); }
    }

    // ── Shared vault create/edit dialog ─────────────────────────────────────────

    private Task<(string? Name, string? Desc, string? Color, string? Icon)> ShowVaultDialogAsync(
        string title,
        string? existingName = null, string? existingDesc = null,
        string? existingColor = null, string? existingIcon = null)
        => Helpers.VaultDialogHelper.ShowAsync(title, existingName, existingDesc, existingColor, existingIcon,
            configureCentering: ConfigureDialogForCentering);

    private async Task ShowMsgAsync(string title, string msg)
    {
        var d = new MwControls.ContentDialog
        {
            Title = title,
            Content = new TextBlock { Text = msg, TextWrapping = TextWrapping.Wrap, MaxWidth = 480 },
            CloseButtonText = "OK"
        };
        ConfigureDialogForCentering(d);
        await d.ShowAsync();
    }
}
