using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using VaultGuard.Models;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.WPF.Views;

public sealed partial class SecurityPage : Page
{
    private IServiceProvider? _serviceProvider;
    private IPasswordItemService? _passwordItemService;
    private ISecurityAuditService? _auditService;

    // Lightweight view model for the issue-section template.
    private sealed class IssueSection
    {
        public string Icon { get; init; } = "";
        public string Heading { get; init; } = "";
        public string Subtitle { get; init; } = "";
        public Brush Color { get; init; } = Brushes.Gray;
        public List<SecurityAuditEntry> Entries { get; init; } = new();
    }

    public SecurityPage() => InitializeComponent();

    public async void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
    {
        if (e.ExtraData is IServiceProvider sp)
        {
            _serviceProvider = sp;
            _passwordItemService = sp.GetService<IPasswordItemService>();
            _auditService = sp.GetService<ISecurityAuditService>();
        }
        await LoadAsync();
    }

    private async void RescanButton_Click(object sender, RoutedEventArgs e) => await LoadAsync();

    private async Task LoadAsync()
    {
        if (_passwordItemService == null || _auditService == null) return;

        try
        {
            var items = await _passwordItemService.GetAllAsync();
            var report = _auditService.Analyze(items);
            Render(report);
        }
        catch
        {
            Render(_auditService.Analyze(Array.Empty<PasswordItem>()));
        }
    }

    private void Render(VaultHealthReport report)
    {
        var color = ParseBrush(report.ColorHex);

        ScoreText.Text = report.Score.ToString();
        ScoreText.Foreground = color;
        RatingText.Text = report.Rating;
        RatingText.Foreground = color;
        CredentialsText.Text = $"{report.TotalCredentials} credentials analysed";

        ScoreCircle.BorderBrush = color;

        WeakCountText.Text = report.WeakCount.ToString();
        ReusedCountText.Text = report.ReusedCount.ToString();
        UnsecuredCountText.Text = report.UnsecuredCount.ToString();
        OldCountText.Text = report.OldCount.ToString();
        No2faCountText.Text = report.MissingTwoFactorCount.ToString();

        var sections = new List<IssueSection>();

        if (report.WeakCount > 0)
            sections.Add(new IssueSection
            {
                Icon = "", Color = ParseBrush("#EA580C"),
                Heading = $"Weak passwords ({report.WeakCount})",
                Subtitle = "These are easy to guess — replace them with stronger ones.",
                Entries = report.WeakPasswords.ToList()
            });

        if (report.ReusedPasswords.Count > 0)
            sections.Add(new IssueSection
            {
                Icon = "", Color = ParseBrush("#D97706"),
                Heading = $"Reused passwords ({report.ReusedPasswords.Count} groups)",
                Subtitle = "The same password is used on multiple items — a breach on one exposes them all.",
                Entries = report.ReusedPasswords
                    .SelectMany(g => g.Items.Select(i => new SecurityAuditEntry
                    {
                        ItemId = i.ItemId,
                        Title = i.Title,
                        Detail = $"shared by {g.Count}"
                    }))
                    .ToList()
            });

        if (report.UnsecuredCount > 0)
            sections.Add(new IssueSection
            {
                Color = ParseBrush("#DC2626"),
                Heading = $"Unsecured websites ({report.UnsecuredCount})",
                Subtitle = "These logins use an unencrypted http:// address - credentials can be intercepted. Switch them to https://.",
                Entries = report.UnsecuredWebsites.ToList()
            });

        if (report.OldCount > 0)
            sections.Add(new IssueSection
            {
                Icon = "", Color = ParseBrush("#0891B2"),
                Heading = $"Old passwords ({report.OldCount})",
                Subtitle = "Not changed in over a year — consider rotating them.",
                Entries = report.OldPasswords.ToList()
            });

        if (report.MissingTwoFactorCount > 0)
            sections.Add(new IssueSection
            {
                Icon = "", Color = ParseBrush("#7C3AED"),
                Heading = $"Logins without 2FA ({report.MissingTwoFactorCount})",
                Subtitle = "These sites have a login but no authenticator code — add one for stronger protection.",
                Entries = report.MissingTwoFactor.ToList()
            });

        IssueSectionsList.ItemsSource = sections;
        AllClearBorder.Visibility = (report.TotalIssues == 0 && report.MissingTwoFactorCount == 0)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private static SolidColorBrush ParseBrush(string hex)
    {
        try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); }
        catch (System.Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Recovered from a suppressed exception", logEx); return new SolidColorBrush(Colors.Gray); }
    }
}
