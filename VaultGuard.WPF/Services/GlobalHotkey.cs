using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace VaultGuard.WPF.Services;

/// <summary>
/// A system-wide keyboard shortcut (Windows RegisterHotKey), delivered to a hidden message-only window, so it
/// works whichever app has focus and while Vault Guard is minimised to the tray.
/// </summary>
internal sealed class GlobalHotkey : IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const int HotkeyId = 0x5647; // "VG"
    [Flags] public enum Mod : uint { Alt = 0x1, Ctrl = 0x2, Shift = 0x4, Win = 0x8, NoRepeat = 0x4000 }

    [DllImport("user32.dll", SetLastError = true)] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint mods, uint vk);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private readonly HwndSource _window;
    private readonly Action _pressed;
    private bool _registered;

    public GlobalHotkey(Action pressed)
    {
        _pressed = pressed;
        // HWND_MESSAGE parent: an invisible window that only receives messages.
        _window = new HwndSource(new HwndSourceParameters("VaultGuardQuickAccessHotkey") { ParentWindow = new IntPtr(-3), Width = 0, Height = 0 });
        _window.AddHook(WndProc);
    }

    /// <summary>Registers the shortcut (replacing any previous one). False if another app already owns it.</summary>
    public bool Register(Mod mods, uint virtualKey)
    {
        Unregister();
        _registered = RegisterHotKey(_window.Handle, HotkeyId, (uint)(mods | Mod.NoRepeat), virtualKey);
        return _registered;
    }

    public void Unregister()
    {
        if (_registered) UnregisterHotKey(_window.Handle, HotkeyId);
        _registered = false;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HotkeyId)
        {
            handled = true;
            _pressed();
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        Unregister();
        _window.RemoveHook(WndProc);
        _window.Dispose();
    }

    /// <summary>The shortcuts offered in Settings ("Off" disables Quick Access).</summary>
    public static readonly (string Name, Mod Mods, uint Key)[] Choices =
    {
        ("Ctrl+Shift+Space", Mod.Ctrl | Mod.Shift, 0x20),
        ("Ctrl+Alt+Space",   Mod.Ctrl | Mod.Alt,   0x20),
        ("Ctrl+Shift+\\",    Mod.Ctrl | Mod.Shift, 0xDC), // VK_OEM_5
        ("Ctrl+Alt+V",       Mod.Ctrl | Mod.Alt,   0x56),
    };
}
