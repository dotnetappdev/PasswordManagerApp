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
    private IBreachCheckService? _breachService;

    // Kept between the audit scan and the (on-demand, network) breach scan so we can re-render together.
    private List<PasswordItem> _items = new();
    private VaultHealthReport? _report;
    private List<SecurityAuditEntry> _breachedEntries = new();
    private bool _breachScanned;

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
            _breachService = sp.GetService<IBreachCheckService>();
        }
        await LoadAsync();
    }

    private async void RescanButton_Click(object sender, RoutedEventArgs e) => await LoadAsync();

    private async Task LoadAsync()
    {
        if (_passwordItemService == null || _auditService == null) return;

        // A fresh audit invalidates any earlier breach results.
        _breachScanned = false;
        _breachedEntries = new List<SecurityAuditEntry>();

        try
        {
            var items = await _passwordItemService.GetAllAsync();
            _items = items?.ToList() ?? new List<PasswordItem>();
            _report = _auditService.Analyze(_items);
            Render(_report);
        }
        catch
        {
            _items = new List<PasswordItem>();
            _report = _auditService.Analyze(Array.Empty<PasswordItem>());
            Render(_report);
        }
    }

    // On-demand Have I Been Pwned scan (k-anonymity). Network call, so it's a separate button.
    private async void BreachScanButton_Click(object sender, RoutedEventArgs e)
    {
        if (_breachService == null || _report == null) return;

        BreachScanButton.IsEnabled = false;
        BreachScanButtonText.Text = "Checking…";
        try
        {
            var found = new List<SecurityAuditEntry>();
            var seen = new Dictionary<string, BreachCheckResult>(StringComparer.Ordinal);
            var checkFailed = false;

            foreach (var item in _items)
            {
                var pwd = item.LoginItem?.Password ?? item.Password;
                if (string.IsNullOrEmpty(pwd)) continue;

                if (!seen.TryGetValue(pwd, out var result))
                {
                    result = await _breachService.CheckPasswordAsync(pwd);
                    seen[pwd] = result;
                }

                if (result.CheckFailed) checkFailed = true;
                else if (result.IsBreached)
                    found.Add(new SecurityAuditEntry
                    {
                        ItemId = item.Id,
                        Title = item.Title ?? "(untitled)",
                        Detail = $"seen {result.TimesSeen:N0} time(s) in breaches"
                    });
            }

            _breachedEntries = found.OrderByDescending(f =>
            {
                var digits = new string(f.Detail.Where(char.IsDigit).ToArray());
                return long.TryParse(digits, out var n) ? n : 0L;
            }).ToList();
            _breachScanned = true;

            Render(_report);

            if (checkFailed)
                VaultGuard.WPF.Services.ToastService.Instance.Show(
                    "Some passwords couldn't be checked — the breach service was unreachable.",
                    VaultGuard.WPF.Services.ToastType.Warning, "Breach check");
            else if (_breachedEntries.Count == 0)
                VaultGuard.WPF.Services.ToastService.Instance.Success(
                    "None of your passwords were found in known data breaches.", "Breach check");
        }
        finally
        {
            BreachScanButton.IsEnabled = true;
            BreachScanButtonText.Text = "Check for breaches";
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

        // Compromised (breach) results go first — they're the most urgent. Only shown after a scan.
        if (_breachScanned && _breachedEntries.Count > 0)
            sections.Add(new IssueSection
            {
                Color = ParseBrush("#DC2626"),
                Heading = $"Compromised passwords ({_breachedEntries.Count})",
                Subtitle = "These appear in known data breaches (Have I Been Pwned — only a partial hash was sent). Change them as soon as possible.",
                Entries = _breachedEntries
            });

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
        AllClearBorder.Visibility = (report.TotalIssues == 0 && report.MissingTwoFactorCount == 0 && _breachedEntries.Count == 0)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private static SolidColorBrush ParseBrush(string hex)
    {
        try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); }
        catch (System.Exception logEx) { VaultGuard.Services.Logging.AppLogger.Warning("Recovered from a suppressed exception", logEx); return new SolidColorBrush(Colors.Gray); }
    }
}
