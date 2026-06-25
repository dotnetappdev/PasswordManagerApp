using System.Windows.Controls;
using PasswordManager.WPF.Services;

namespace PasswordManager.WPF.Controls;

public partial class ToastHost : UserControl
{
    public ToastHost()
    {
        InitializeComponent();
        ToastService.Instance.SetHost(ToastPanel);
    }
}
