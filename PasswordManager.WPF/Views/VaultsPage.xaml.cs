using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.WPF.ViewModels;
using PasswordManager.WPF.Services;
using PasswordManager.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using MwControls = ModernWpf.Controls;

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
            if (dialog.Style == null && Application.Current.Resources.Contains("Modern1PasswordDialogStyle"))
                dialog.Style = Application.Current.Resources["Modern1PasswordDialogStyle"] as Style;
        }
        catch { }
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
                await _viewModel.DeleteVaultAsync(vault);
                ToastService.Instance.Success($"Vault \"{vault.Name}\" deleted");
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

    private async Task<(string? Name, string? Desc, string? Color, string? Icon)> ShowVaultDialogAsync(
        string title,
        string? existingName = null, string? existingDesc = null,
        string? existingColor = null, string? existingIcon = null)
    {
        var dialog = new MwControls.ContentDialog
        {
            Title = title,
            PrimaryButtonText = existingName == null ? "Create" : "Save",
            CloseButtonText = "Cancel",
            DefaultButton = MwControls.ContentDialogButton.Primary
        };
        ConfigureDialogForCentering(dialog);

        var nameBox = new TextBox { Text = existingName ?? "", Margin = new Thickness(0,0,0,12) };
        var descBox = new TextBox
        {
            Text = existingDesc ?? "", AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap, Height = 64,
            Margin = new Thickness(0,0,0,12)
        };
        var colorBox = new TextBox { Text = existingColor ?? "#2563EB", Margin = new Thickness(0,0,0,12) };
        var iconBox  = new TextBox { Text = existingIcon  ?? "🔐" };

        var colorSwatches = new[] { "#2563EB","#7C3AED","#059669","#DC2626","#D97706","#0891B2","#EC4899","#374151" };
        var swatchPanel = new WrapPanel { Margin = new Thickness(0,0,0,12) };
        foreach (var hex in colorSwatches)
        {
            var swatch = new Border
            {
                Width = 28, Height = 28, CornerRadius = new CornerRadius(6), Margin = new Thickness(0,0,8,0),
                Background = TryParseBrush(hex), Cursor = System.Windows.Input.Cursors.Hand
            };
            var captured = hex;
            swatch.MouseLeftButtonDown += (_, _) => colorBox.Text = captured;
            swatchPanel.Children.Add(swatch);
        }

        var panel = new StackPanel { Margin = new Thickness(4) };
        panel.Children.Add(MakeLabel("Vault Name *")); panel.Children.Add(nameBox);
        panel.Children.Add(MakeLabel("Description")); panel.Children.Add(descBox);
        panel.Children.Add(MakeLabel("Accent Color")); panel.Children.Add(swatchPanel);
        panel.Children.Add(colorBox);
        panel.Children.Add(MakeLabel("Icon Emoji")); panel.Children.Add(iconBox);
        dialog.Content = panel;

        var result = await dialog.ShowAsync();
        if (result != MwControls.ContentDialogResult.Primary) return (null, null, null, null);
        var n = nameBox.Text?.Trim();
        if (string.IsNullOrEmpty(n)) return (null, null, null, null);
        return (n, descBox.Text?.Trim(), colorBox.Text?.Trim(), iconBox.Text?.Trim());
    }

    private static TextBlock MakeLabel(string text) =>
        new() { Text = text, FontSize = 12, FontWeight = FontWeights.SemiBold,
                Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x9D, 0x9D, 0x9D)),
                Margin = new Thickness(0,0,0,4) };

    private static System.Windows.Media.SolidColorBrush TryParseBrush(string hex)
    {
        try { return new((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex)); }
        catch { return new(System.Windows.Media.Color.FromRgb(0x37, 0x37, 0x37)); }
    }

    private async Task ShowMsgAsync(string title, string msg)
    {
        var d = new MwControls.ContentDialog { Title = title, Content = msg, CloseButtonText = "OK" };
        ConfigureDialogForCentering(d);
        await d.ShowAsync();
    }
}
