using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Services.Interfaces;

namespace PasswordManager.WinUi.ViewModels;

public class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly IVaultSessionService _vaultSessionService;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;

    public LoginViewModel(IServiceProvider serviceProvider)
    {
        _authService = serviceProvider.GetRequiredService<IAuthService>();
        _vaultSessionService = serviceProvider.GetRequiredService<IVaultSessionService>();
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public async Task<bool> LoginAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Please enter both email and password.";
                return false;
            }

            // Attempt login
            var loginResult = await _authService.LoginAsync(Email, Password);
            
            if (loginResult)
            {
                return true;
            }
            else
            {
                ErrorMessage = "Login failed. Please check your credentials and try again.";
                return false;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Login failed: {ex.Message}";
            return false;
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasError));
        }
    }

    public async Task<bool> RegisterAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Please enter both email and password.";
                return false;
            }

            // Attempt registration
            var registerResult = await _authService.RegisterAsync(Email, Password);
            
            if (registerResult)
            {
                return true;
            }
            else
            {
                ErrorMessage = "Registration failed. Please try again.";
                return false;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Registration failed: {ex.Message}";
            return false;
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasError));
        }
    }
}