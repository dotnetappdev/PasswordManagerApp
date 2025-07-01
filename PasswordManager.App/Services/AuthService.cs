using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace PasswordManager.App.Services
{
    public class AuthService
    {
        private readonly IJSRuntime _jsRuntime;
        private bool _isAuthenticated = false;

        public AuthService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public bool IsAuthenticated => _isAuthenticated;

        public async Task<bool> CheckAuthenticationStatusAsync()
        {
            try
            {
                var hasAuth = await _jsRuntime.InvokeAsync<bool>("localStorage.getItem", "isAuthenticated");
                _isAuthenticated = hasAuth;
                return _isAuthenticated;
            }
            catch
            {
                _isAuthenticated = false;
                return false;
            }
        }

        public async Task SetAuthenticatedAsync(bool authenticated)
        {
            _isAuthenticated = authenticated;
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "isAuthenticated", authenticated);
        }

        public async Task LogoutAsync()
        {
            _isAuthenticated = false;
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "isAuthenticated");
        }
    }
}
