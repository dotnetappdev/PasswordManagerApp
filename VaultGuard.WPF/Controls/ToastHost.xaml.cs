using System.Windows.Controls;
using VaultGuard.WPF.Services;

namespace VaultGuard.WPF.Controls;

public partial class ToastHost : UserControl
{
    public ToastHost()
    {
        InitializeComponent();
        ToastService.Instance.SetHost(ToastPanel);
    }
}
