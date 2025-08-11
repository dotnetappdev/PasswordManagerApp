using Microsoft.UI.Xaml;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Services.Interfaces;

namespace PasswordManager.WinUi;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly IServiceProvider _serviceProvider;

    public MainWindow(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        this.InitializeComponent();
        this.Title = "Password Manager - WinUI";
        
        // Set window size
        this.AppWindow.Resize(new Windows.Graphics.SizeInt32(1200, 800));
        
        // Initialize the navigation frame
        MainFrame.Navigate(typeof(Views.LoginPage), _serviceProvider);
    }
}