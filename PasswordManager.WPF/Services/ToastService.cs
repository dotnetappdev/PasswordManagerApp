using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PasswordManager.WPF.Services;

public enum ToastType { Success, Error, Warning, Info }

public sealed class ToastMessage
{
    public string Title   { get; init; } = "";
    public string Message { get; init; } = "";
    public ToastType Type { get; init; } = ToastType.Info;
    public int DurationMs { get; init; } = 3500;
}

/// <summary>
/// Creates and manages overlay toast notifications anchored to a Panel host.
/// Call SetHost() once when the main window is ready, then Show() from anywhere.
/// </summary>
public sealed class ToastService
{
    private static readonly ToastService _instance = new();
    public static ToastService Instance => _instance;

    private Panel? _host;

    public void SetHost(Panel host) => _host = host;

    public void Show(string message, ToastType type = ToastType.Info, string? title = null, int durationMs = 3500)
    {
        if (_host == null) return;
        _host.Dispatcher.BeginInvoke(() => ShowToast(new ToastMessage
        {
            Title = title ?? DefaultTitle(type),
            Message = message,
            Type = type,
            DurationMs = durationMs
        }));
    }

    public void Success(string message, string? title = null) => Show(message, ToastType.Success, title);
    public void Error  (string message, string? title = null) => Show(message, ToastType.Error,   title);
    public void Warning(string message, string? title = null) => Show(message, ToastType.Warning, title);
    public void Info   (string message, string? title = null) => Show(message, ToastType.Info,    title);

    private static string DefaultTitle(ToastType t) => t switch
    {
        ToastType.Success => "Success",
        ToastType.Error   => "Error",
        ToastType.Warning => "Warning",
        _                 => "Info"
    };

    private void ShowToast(ToastMessage toast)
    {
        var card = BuildCard(toast);
        _host!.Children.Add(card);

        // Slide in
        var slideIn = new DoubleAnimation(60, 0, TimeSpan.FromMilliseconds(280))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
        var transform = (TranslateTransform)((Border)card).RenderTransform;
        transform.BeginAnimation(TranslateTransform.YProperty, slideIn);
        card.BeginAnimation(UIElement.OpacityProperty, fadeIn);

        // Auto-dismiss
        Task.Delay(toast.DurationMs).ContinueWith(_ =>
            _host.Dispatcher.BeginInvoke(() => DismissCard(card)));
    }

    private static void DismissCard(UIElement card)
    {
        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
        fadeOut.Completed += (_, _) =>
        {
            if (card is FrameworkElement fe && fe.Parent is Panel parent)
                parent.Children.Remove(card);
        };
        card.BeginAnimation(UIElement.OpacityProperty, fadeOut);
    }

    private static UIElement BuildCard(ToastMessage toast)
    {
        // NOTE: icons must be real Unicode chars (\u escapes), NOT XML "&#x...;" entities — those only
        // work in XAML and render as empty squares when assigned to TextBlock.Text in code.
        var (accent, _, bgHex) = toast.Type switch
        {
            ToastType.Success => (ToastSettings.Success.Accent,"", ToastSettings.Success.Background), // CheckMark
            ToastType.Error   => (ToastSettings.Error.Accent,"", ToastSettings.Error.Background), // ErrorBadge
            ToastType.Warning => (ToastSettings.Warning.Accent,"", ToastSettings.Warning.Background), // Warning
            _                 => (ToastSettings.Info.Accent,"", ToastSettings.Info.Background)  // Info
        };

        var theme = ToastSettings.For(toast.Type);

        Brush SafeBrush(string hex, string fallback)
        {
            try { return (Brush)new BrushConverter().ConvertFrom(hex)!; }
            catch { return (Brush)new BrushConverter().ConvertFrom(fallback)!; }
        }

        var accentBrush  = SafeBrush(accent, "#60A5FA");
        var bgBrush      = SafeBrush(bgHex, "#0A1929");
        var transform    = new TranslateTransform(0, 60);

        // Icon
        var iconBlock = new TextBlock
        {
            // Build the glyph from its (user-customisable) code point so no literal
            // private-use char is needed in source.
            Text        = char.ConvertFromUtf32(theme.IconGlyph),
            FontFamily  = new FontFamily("Segoe MDL2 Assets"),
            FontSize    = 18,
            Foreground  = accentBrush,
            VerticalAlignment = VerticalAlignment.Top,
            Margin      = new Thickness(0, 1, 0, 0)
        };

        // Title
        var titleBlock = new TextBlock
        {
            Text       = toast.Title,
            FontSize   = 13,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0xE5, 0xE5, 0xE5)),
            Margin     = new Thickness(0, 0, 0, 3)
        };

        // Message
        var msgBlock = new TextBlock
        {
            Text        = toast.Message,
            FontSize    = 12,
            Foreground  = new SolidColorBrush(Color.FromRgb(0x9D, 0x9D, 0x9D)),
            TextWrapping = TextWrapping.Wrap,
            MaxWidth    = 260
        };

        var textStack = new StackPanel { Margin = new Thickness(10, 0, 0, 0) };
        textStack.Children.Add(titleBlock);
        textStack.Children.Add(msgBlock);

        var row = new StackPanel { Orientation = Orientation.Horizontal };
        row.Children.Add(iconBlock);
        row.Children.Add(textStack);

        // Close button
        var closeBtn = new Button
        {
            Content = "✕",
            FontSize = 11,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Foreground = new SolidColorBrush(Color.FromRgb(0x6A, 0x6A, 0x6A)),
            VerticalAlignment = VerticalAlignment.Top,
            Cursor = System.Windows.Input.Cursors.Hand,
            Padding = new Thickness(0)
        };

        // Accent left bar
        var accentBar = new Border
        {
            Width      = 3,
            Background = accentBrush,
            CornerRadius = new CornerRadius(2, 0, 0, 2)
        };

        var contentGrid = new Grid { Margin = new Thickness(12, 12, 12, 12) };
        contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(row, 1);
        Grid.SetColumn(closeBtn, 2);
        contentGrid.Children.Add(row);
        contentGrid.Children.Add(closeBtn);

        var inner = new Grid();
        inner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        inner.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(accentBar, 0);
        Grid.SetColumn(contentGrid, 1);
        inner.Children.Add(accentBar);
        inner.Children.Add(contentGrid);

        var card = new Border
        {
            MinWidth        = 300,
            MaxWidth        = 360,
            Background      = bgBrush,
            CornerRadius    = new CornerRadius(10),
            BorderBrush     = accentBrush,
            BorderThickness = new Thickness(1),
            Margin          = new Thickness(0, 0, 0, 8),
            Child           = inner,
            RenderTransform = transform,
            Effect          = new System.Windows.Media.Effects.DropShadowEffect
            {
                ShadowDepth = 0, BlurRadius = 16, Color = Colors.Black, Opacity = 0.5
            }
        };

        closeBtn.Click += (_, _) => DismissCard(card);
        return card;
    }
}
