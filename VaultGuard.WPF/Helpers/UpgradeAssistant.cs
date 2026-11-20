using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ModernWpf.Controls;
using VaultGuard.DAL;
using VaultGuard.Services.Services;
using VaultGuard.WPF.Services;

namespace VaultGuard.WPF.Helpers;

/// <summary>
/// Help ▸ Check for Updates / Back Up Vault: checks for a new version and, before installing it, offers
/// to take a verified snapshot of the local vault database so the user can roll back if the upgrade
/// (which may migrate the schema) goes wrong.
/// </summary>
public static class UpgradeAssistant
{
    public static string BackupFolder => SqliteVaultSnapshot.DefaultFolder;

    // ── Backup ──────────────────────────────────────────────────────────────────────────────

    /// <summary>Snapshots the vault database. Throws with a readable message on failure.</summary>
    public static async Task<VaultSnapshotResult> CreateBackupAsync(IServiceProvider sp, string label)
    {
        string? dbPath;
        using (var scope = sp.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VaultGuardDbContext>();
            dbPath = SqliteVaultSnapshot.GetDatabasePath(db.Database.GetConnectionString());
        }
        if (dbPath == null)
            throw new InvalidOperationException("Couldn't locate the vault database file to back up.");
        return await SqliteVaultSnapshot.CreateAsync(dbPath, BackupFolder, label, keepLatest: 5);
    }

    /// <summary>Help ▸ Back Up Vault Now: snapshot + result dialog with an "Open folder" button.</summary>
    public static async Task BackUpNowAsync(IServiceProvider sp, string currentVersion)
    {
        try
        {
            var result = await CreateBackupAsync(sp, $"v{currentVersion}-manual");
            await ShowBackupResultAsync(result, "Vault backed up");
        }
        catch (Exception ex)
        {
            VaultGuard.Services.Logging.AppLogger.Error("Manual vault backup failed", ex);
            await ShowAsync(new ContentDialog
            {
                Title = "Backup failed",
                Content = Message($"The vault couldn't be backed up: {ex.Message}"),
                CloseButtonText = "OK"
            });
        }
    }

    /// <summary>
    /// Asks whether to back up before installing <paramref name="update"/> and does it.
    /// Returns false if the user cancelled (or chose not to continue after a failed backup).
    /// </summary>
    public static async Task<bool> OfferBackupBeforeInstallAsync(IServiceProvider sp, UpdateInfo update, string currentVersion)
    {
        var choice = await ShowAsync(new ContentDialog
        {
            Title = "Back up before upgrading?",
            Content = Message(
                $"Before installing v{update.Version}, Vault Guard can save a copy of your vault (v{currentVersion}) " +
                "so you can roll back if anything goes wrong. Your passwords stay encrypted in the copy.\n\n" +
                $"Backups are kept in:\n{BackupFolder}"),
            PrimaryButtonText = "Back up & continue",
            SecondaryButtonText = "Skip backup",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary
        });

        if (choice == ContentDialogResult.None) return false;
        if (choice == ContentDialogResult.Secondary) return true;

        try
        {
            var result = await CreateBackupAsync(sp, $"v{currentVersion}-before-v{update.Version}");
            ToastService.Instance.Success($"Vault backed up ({result.ItemCount} items) — {Path.GetFileName(result.FilePath)}");
            return true;
        }
        catch (Exception ex)
        {
            VaultGuard.Services.Logging.AppLogger.Error("Pre-upgrade vault backup failed", ex);
            var cont = await ShowAsync(new ContentDialog
            {
                Title = "Backup failed",
                Content = Message($"The vault couldn't be backed up: {ex.Message}\n\nInstall the update anyway?"),
                PrimaryButtonText = "Install anyway",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close
            });
            return cont == ContentDialogResult.Primary;
        }
    }

    // ── Check → backup → download → install ─────────────────────────────────────────────────

    /// <summary>Help ▸ Check for Updates: the whole flow, ending in the installer if the user agrees.</summary>
    public static async Task CheckForUpdatesAsync(IServiceProvider sp, string? manifestUrl)
    {
        var svc = sp.GetService<UpdateService>();
        if (svc == null) return;
        var current = svc.GetCurrentVersion();

        UpdateInfo? update;
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            update = await svc.CheckManifestAsync(manifestUrl, cts.Token);
        }
        catch (Exception ex)
        {
            await ShowAsync(new ContentDialog { Title = "Check for Updates", Content = Message($"Could not check for updates: {ex.Message}"), CloseButtonText = "OK" });
            return;
        }

        if (update == null)
        {
            var upToDate = await ShowAsync(new ContentDialog
            {
                Title = "Check for Updates",
                Content = Message($"You're up to date (v{current}).\n\nYou can still back up your vault at any time from Help ▸ Back Up Vault Now."),
                PrimaryButtonText = "Back up now",
                CloseButtonText = "OK"
            });
            if (upToDate == ContentDialogResult.Primary) await BackUpNowAsync(sp, current);
            return;
        }

        var appName = string.IsNullOrWhiteSpace(update.AppName) ? "Vault Guard" : update.AppName;
        var features = update.Features is { Count: > 0 }
            ? "\n\nWhat's new:\n" + string.Join("\n", update.Features.Select(f => "  •  " + f))
            : string.Empty;
        var canInstall = update.InstallerDownloadUrl != null;

        var go = await ShowAsync(new ContentDialog
        {
            Title = "Update available",
            Content = Message(
                $"{appName} v{update.Version} is available (you have v{current})."
                + (update.RequiresRestart ? " A restart is required to apply it." : string.Empty)
                + features
                + (canInstall ? "\n\nYou'll be offered a backup of your vault before it installs." : string.Empty)),
            PrimaryButtonText = canInstall ? "Update now…" : "Open download page",
            CloseButtonText = "Later",
            DefaultButton = ContentDialogButton.Primary
        });
        if (go != ContentDialogResult.Primary) return;

        if (!canInstall)
        {
            svc.OpenReleasesPage();
            return;
        }

        if (!await OfferBackupBeforeInstallAsync(sp, update, current)) return;

        var installer = await DownloadWithProgressAsync(svc, update);
        if (installer == null) return;

        try { svc.LaunchInstallerAndExit(installer); }
        catch (Exception ex)
        {
            await ShowAsync(new ContentDialog { Title = "Update", Content = Message($"Couldn't start the installer: {ex.Message}\n\nIt was saved to:\n{installer}"), CloseButtonText = "OK" });
        }
    }

    /// <summary>Downloads the installer behind a cancellable progress dialog. Returns its path, or null.</summary>
    private static async Task<string?> DownloadWithProgressAsync(UpdateService svc, UpdateInfo update)
    {
        var path = Path.Combine(Path.GetTempPath(), $"VaultGuardSetup-{update.Version}.exe");
        var bar = new System.Windows.Controls.ProgressBar { Minimum = 0, Maximum = 100, Height = 6, Margin = new Thickness(0, 12, 0, 0) };
        var text = new TextBlock { Text = "Downloading…", TextWrapping = TextWrapping.Wrap };
        var dlg = new ContentDialog
        {
            Title = $"Downloading v{update.Version}",
            Content = new StackPanel { Width = 420, Children = { text, bar } },
            CloseButtonText = "Cancel"
        };

        using var cts = new CancellationTokenSource();
        var dialogTask = ShowAsync(dlg);
        // Closing the dialog (Cancel) cancels the download.
        _ = dialogTask.ContinueWith(_ => cts.Cancel(), TaskScheduler.FromCurrentSynchronizationContext());

        try
        {
            await svc.DownloadInstallerAsync(update.InstallerDownloadUrl!, path,
                new Progress<int>(p => { bar.Value = p; text.Text = $"Downloading… {p}%"; }), cts.Token);
            dlg.Hide();
            await dialogTask;
            return path;
        }
        catch (OperationCanceledException)
        {
            TryDelete(path);
            return null;
        }
        catch (Exception ex)
        {
            dlg.Hide();
            await dialogTask;
            TryDelete(path); // don't leave a truncated installer that could be run later
            await ShowAsync(new ContentDialog { Title = "Download failed", Content = Message(ex.Message), CloseButtonText = "OK" });
            return null;
        }
    }

    // ── UI helpers ──────────────────────────────────────────────────────────────────────────

    private static async Task ShowBackupResultAsync(VaultSnapshotResult result, string title)
    {
        var open = await ShowAsync(new ContentDialog
        {
            Title = title,
            Content = Message(
                $"Saved {result.ItemCount} items ({result.SizeBytes / 1024:N0} KB) to:\n{result.FilePath}\n\n" +
                "To roll back, close Vault Guard and put this file back in place of your vault database. " +
                "The 5 most recent backups are kept."),
            PrimaryButtonText = "Open folder",
            CloseButtonText = "OK"
        });
        if (open == ContentDialogResult.Primary) OpenBackupFolder(result.FilePath);
    }

    /// <summary>Opens Explorer on the backup folder (selecting <paramref name="select"/> if given).</summary>
    public static void OpenBackupFolder(string? select = null)
    {
        try
        {
            Directory.CreateDirectory(BackupFolder);
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = select != null && File.Exists(select) ? $"/select,\"{select}\"" : $"\"{BackupFolder}\"",
                UseShellExecute = true
            });
        }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Warning("Couldn't open backup folder", ex); }
    }

    private static TextBlock Message(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap, MaxWidth = 480 };

    private static Task<ContentDialogResult> ShowAsync(ContentDialog dialog)
    {
        try { dialog.Style = dialog.TryFindResource("Modern1PasswordDialogStyle") as Style; }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Warning("Dialog style not found", ex); }
        return DialogManager.ShowAsync(dialog);
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Warning($"Couldn't delete {path}", ex); }
    }
}
