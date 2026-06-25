using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PasswordManager.WinUi.ViewModels;

public class BaseViewModel : INotifyPropertyChanged
{
    private bool _isLoading = false;

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsButtonEnabled)); // Notify button state change
        }
    }

    public virtual bool IsButtonEnabled => !IsLoading;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}