using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using PasswordManager.WinUi.Converters;

namespace PasswordManager.WinUi.Tests;

/// <summary>
/// Tests to verify WinUI 3 compatibility and proper usage of WinUI patterns
/// instead of WPF-style triggers and bindings.
/// </summary>
public class WinUiCompatibilityTests
{
    /// <summary>
    /// Verifies that all value converters implement IValueConverter properly for WinUI 3
    /// </summary>
    public void TestValueConvertersImplementation()
    {
        // Test BoolToVisibilityConverter
        var boolToVisConverter = new BoolToVisibilityConverter();
        Assert.AreEqual(Visibility.Visible, boolToVisConverter.Convert(true, typeof(Visibility), null, ""));
        Assert.AreEqual(Visibility.Collapsed, boolToVisConverter.Convert(false, typeof(Visibility), null, ""));
        Assert.AreEqual(true, boolToVisConverter.ConvertBack(Visibility.Visible, typeof(bool), null, ""));
        Assert.AreEqual(false, boolToVisConverter.ConvertBack(Visibility.Collapsed, typeof(bool), null, ""));

        // Test InverseBoolToVisibilityConverter
        var inverseBoolConverter = new InverseBoolToVisibilityConverter();
        Assert.AreEqual(Visibility.Collapsed, inverseBoolConverter.Convert(true, typeof(Visibility), null, ""));
        Assert.AreEqual(Visibility.Visible, inverseBoolConverter.Convert(false, typeof(Visibility), null, ""));

        // Test StringToVisibilityConverter
        var stringConverter = new StringToVisibilityConverter();
        Assert.AreEqual(Visibility.Visible, stringConverter.Convert("test", typeof(Visibility), null, ""));
        Assert.AreEqual(Visibility.Collapsed, stringConverter.Convert("", typeof(Visibility), null, ""));
        Assert.AreEqual(Visibility.Collapsed, stringConverter.Convert(null, typeof(Visibility), null, ""));

        // Test StatusToIconConverter
        var statusConverter = new StatusToIconConverter();
        Assert.AreEqual("✅", statusConverter.Convert("success", typeof(string), null, ""));
        Assert.AreEqual("❌", statusConverter.Convert("error", typeof(string), null, ""));
        Assert.AreEqual("⚠️", statusConverter.Convert("warning", typeof(string), null, ""));

        // Test ApiModeToVisibilityConverter
        var apiModeConverter = new ApiModeToVisibilityConverter();
        Assert.AreEqual(Visibility.Visible, apiModeConverter.Convert("API Server", typeof(Visibility), null, ""));
        Assert.AreEqual(Visibility.Collapsed, apiModeConverter.Convert("Local Database", typeof(Visibility), null, ""));

        // Test LocalModeToEnabledConverter
        var localModeConverter = new LocalModeToEnabledConverter();
        Assert.AreEqual(true, localModeConverter.Convert("Local Database", typeof(bool), null, ""));
        Assert.AreEqual(false, localModeConverter.Convert("API Server", typeof(bool), null, ""));
    }

    /// <summary>
    /// Demonstrates WinUI 3 alternatives to WPF triggers
    /// </summary>
    public void DemonstrateWinUiAlternatives()
    {
        // Alternative 1: Value Converters (used throughout the app)
        // Instead of DataTrigger for visibility, use BoolToVisibilityConverter
        
        // Alternative 2: VisualStateManager (recommended for complex states)
        // Example in code-behind:
        // VisualStateManager.GoToState(this, "LoadingState", true);
        
        // Alternative 3: Property binding with computed properties
        // Instead of triggers, expose computed properties from ViewModels
        
        // Alternative 4: x:Bind for compiled bindings (performance improvement)
        // {x:Bind ViewModel.Property, Mode=OneWay} is faster than {Binding}
    }
}

// Simple assertion class for testing
public static class Assert
{
    public static void AreEqual<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new Exception($"Expected {expected}, but got {actual}");
        }
    }
}
