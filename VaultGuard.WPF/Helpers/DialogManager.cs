using System;
using System.Threading;
using System.Threading.Tasks;
using MwControls = ModernWpf.Controls;

namespace VaultGuard.WPF.Helpers;

/// <summary>
/// Serializes ModernWpf <see cref="MwControls.ContentDialog"/> display.
///
/// ModernWpf only permits ONE ContentDialog open per window at a time — calling ShowAsync while
/// another is open throws ContentDialog.ThrowAlreadyOpenException(). That used to crash flows like
/// "rename vault → error → ShowErrorMessage" because the error dialog tried to open on top of a
/// dialog that hadn't finished closing yet.
///
/// Every dialog in the app should be shown through <see cref="ShowAsync"/>. A single static gate
/// guarantees the next dialog never starts until the previous one has fully closed, so the
/// "already open" exception can no longer occur.
/// </summary>
public static class DialogManager
{
    private static readonly SemaphoreSlim _gate = new(1, 1);
    private static MwControls.ContentDialog? _current;
    private static int _generation;

    // Every ContentDialog on screen, however it was shown (many call ShowAsync directly), so a lock can close them all.
    private static readonly List<WeakReference<MwControls.ContentDialog>> _open = new();

    /// <summary>Call once at startup: starts tracking open dialogs.</summary>
    public static void Initialize()
    {
        System.Windows.EventManager.RegisterClassHandler(typeof(MwControls.ContentDialog), System.Windows.FrameworkElement.LoadedEvent,
            new System.Windows.RoutedEventHandler((s, _) => { if (s is MwControls.ContentDialog d) lock (_open) _open.Add(new(d)); }));
        System.Windows.EventManager.RegisterClassHandler(typeof(MwControls.ContentDialog), System.Windows.FrameworkElement.UnloadedEvent,
            new System.Windows.RoutedEventHandler((s, _) => { lock (_open) _open.RemoveAll(w => !w.TryGetTarget(out var d) || ReferenceEquals(d, s)); }));
    }

    /// <summary>True while a dialog is currently being shown through the manager.</summary>
    public static bool IsDialogOpen => _gate.CurrentCount == 0;

    public static async Task<MwControls.ContentDialogResult> ShowAsync(MwControls.ContentDialog dialog)
    {
        if (dialog == null) throw new ArgumentNullException(nameof(dialog));

        var generation = _generation;
        await _gate.WaitAsync().ConfigureAwait(true);
        try
        {
            // The vault locked while this dialog was waiting its turn: don't show it over the sign-in screen.
            if (generation != _generation) return MwControls.ContentDialogResult.None;
            _current = dialog;
            return await dialog.ShowAsync();
        }
        finally
        {
            _current = null;
            _gate.Release();
        }
    }

    /// <summary>
    /// Closes the dialog on screen (e.g. one showing a password) and cancels any waiting to be shown. Called when
    /// the vault locks, so nothing from the vault stays visible on top of the sign-in screen.
    /// </summary>
    public static void CloseAll()
    {
        Interlocked.Increment(ref _generation);
        List<MwControls.ContentDialog> open;
        lock (_open) open = _open.Select(w => w.TryGetTarget(out var d) ? d : null).OfType<MwControls.ContentDialog>().ToList();
        if (_current != null && !open.Contains(_current)) open.Add(_current);
        foreach (var d in open)
        {
            try { d.Hide(); }
            catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Warning("Couldn't close an open dialog on lock", ex); }
        }
    }
}
