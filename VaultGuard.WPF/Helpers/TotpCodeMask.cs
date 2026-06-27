using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace VaultGuard.WPF.Helpers;

/// <summary>
/// Formats a TextBox as a 6-digit authenticator (TOTP) code with a "123-456" display mask.
/// As the user types, non-digits are dropped, the entry is capped at 6 digits, and a dash is
/// inserted after the third digit. Use <see cref="Strip"/> (or just verify through the service,
/// which already strips non-digits) so the dash is removed before the code is checked.
/// </summary>
public static class TotpCodeMask
{
    private static readonly Dictionary<TextBox, TextChangedEventHandler> _handlers = new();
    private static bool _suppress;

    /// <summary>Applies the 123-456 mask to <paramref name="box"/> (idempotent).</summary>
    public static void Attach(TextBox box)
    {
        if (box == null || _handlers.ContainsKey(box)) return;

        box.MaxLength = 7; // "123-456"
        TextChangedEventHandler handler = (_, _) => Format(box);
        _handlers[box] = handler;
        box.TextChanged += handler;
        Format(box);
    }

    /// <summary>Removes the mask and restores a free-form max length (e.g. for recovery codes).</summary>
    public static void Detach(TextBox box, int maxLength = 16)
    {
        if (box == null) return;
        if (_handlers.TryGetValue(box, out var handler))
        {
            box.TextChanged -= handler;
            _handlers.Remove(box);
        }
        box.MaxLength = maxLength;
    }

    /// <summary>Returns just the digits of a (possibly masked) code, e.g. "123-456" -> "123456".</summary>
    public static string Strip(string? text) => new((text ?? string.Empty).Where(char.IsDigit).ToArray());

    private static void Format(TextBox box)
    {
        if (_suppress) return;

        var digits = Strip(box.Text);
        if (digits.Length > 6) digits = digits.Substring(0, 6);
        var formatted = digits.Length > 3 ? digits.Insert(3, "-") : digits;

        if (formatted != box.Text)
        {
            _suppress = true;
            box.Text = formatted;
            box.CaretIndex = formatted.Length;
            _suppress = false;
        }
    }
}
