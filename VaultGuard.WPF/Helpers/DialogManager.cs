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

    /// <summary>True while a dialog is currently being shown through the manager.</summary>
    public static bool IsDialogOpen => _gate.CurrentCount == 0;

    public static async Task<MwControls.ContentDialogResult> ShowAsync(MwControls.ContentDialog dialog)
    {
        if (dialog == null) throw new ArgumentNullException(nameof(dialog));

        await _gate.WaitAsync().ConfigureAwait(true);
        try
        {
            return await dialog.ShowAsync();
        }
        finally
        {
            _gate.Release();
        }
    }
}
