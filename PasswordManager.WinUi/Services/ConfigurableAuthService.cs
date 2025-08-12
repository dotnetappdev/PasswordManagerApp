using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using PasswordManager.Models;
using PasswordManager.Models.DTOs.Auth;
using PasswordManager.Services.Interfaces;
using Microsoft.Extensions.Logging;
using PasswordManager.WinUi.Services;

namespace PasswordManager.WinUi.Services;

/// <summary>
/// Configurable authentication service that can switch between local database and API server modes
/// </summary>
public class ConfigurableAuthService : IAuthService
{
    private readonly WinUiAuthService _localAuthService;
    private readonly HttpClient _httpClient;
    private readonly ISecureStorageService _secureStorageService;
    private readonly ILogger<ConfigurableAuthService> _logger;
    
    private bool _isAuthenticated = false;
    private ApplicationUser? _currentUser;

    public ConfigurableAuthService(
        WinUiAuthService localAuthService,
        HttpClient httpClient,
        ISecureStorageService secureStorageService,
        ILogger<ConfigurableAuthService> logger)
    {
        _localAuthService = localAuthService;
        _httpClient = httpClient;
        _secureStorageService = secureStorageService;
        _logger = logger;
    }

    public bool IsAuthenticated => _isAuthenticated;
    public ApplicationUser? CurrentUser => _currentUser;

    /// <summary>
    /// Gets the current authentication mode from settings
    /// </summary>
    private async Task<string> GetAuthenticationModeAsync()
    {
        var mode = await _secureStorageService.GetAsync("AuthenticationMode");
        return mode ?? "Local Database";
    }

    /// <summary>
    /// Gets the API base URL from settings
    /// </summary>
    private async Task<string> GetApiBaseUrlAsync()
    {
        var url = await _secureStorageService.GetAsync("ApiBaseUrl");
        return url ?? "https://localhost:7001/api";
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var mode = await GetAuthenticationModeAsync();
        
        if (mode == "Local Database")
        {
            var isAuth = await _localAuthService.IsAuthenticatedAsync();
            _isAuthenticated = isAuth;
            _currentUser = _localAuthService.CurrentUser;
            return isAuth;
        }
        else
        {
            // For API mode, check if we have a valid session token
            var token = await _secureStorageService.GetAsync("apiSessionToken");
            if (string.IsNullOrEmpty(token))
            {
                _isAuthenticated = false;
                return false;
            }

            try
            {
                var apiUrl = await GetApiBaseUrlAsync();
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                
                var response = await _httpClient.GetAsync($"{apiUrl}/auth/verify");
                if (response.IsSuccessStatusCode)
                {
                    var userDto = await response.Content.ReadFromJsonAsync<UserDto>();
                    if (userDto != null)
                    {
                        _currentUser = new ApplicationUser
                        {
                            Id = userDto.Id,
                            Email = userDto.Email,
                            UserName = userDto.Email,
                            FirstName = userDto.FirstName,
                            LastName = userDto.LastName,
                            CreatedAt = userDto.CreatedAt,
                            LastLoginAt = userDto.LastLoginAt,
                            IsActive = userDto.IsActive
                        };
                        _isAuthenticated = true;
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying API session");
            }
            
            _isAuthenticated = false;
            return false;
        }
    }

    public async Task<bool> LoginAsync(string usernameOrEmail, string password)
    {
        var mode = await GetAuthenticationModeAsync();
        
        if (mode == "Local Database")
        {
            var result = await _localAuthService.LoginAsync(usernameOrEmail, password);
            _isAuthenticated = result;
            _currentUser = _localAuthService.CurrentUser;
            return result;
        }
        else
        {
            try
            {
                var apiUrl = await GetApiBaseUrlAsync();
                var loginRequest = new LoginRequestDto
                {
                    Email = usernameOrEmail,
                    Password = password
                };

                var response = await _httpClient.PostAsJsonAsync($"{apiUrl}/auth/login", loginRequest);
                if (response.IsSuccessStatusCode)
                {
                    var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
                    if (authResponse != null)
                    {
                        // Store the session token
                        await _secureStorageService.SetAsync("apiSessionToken", authResponse.Token);
                        await _secureStorageService.SetAsync("apiTokenExpiry", authResponse.ExpiresAt.ToString());
                        
                        _currentUser = new ApplicationUser
                        {
                            Id = authResponse.User.Id,
                            Email = authResponse.User.Email,
                            UserName = authResponse.User.Email,
                            FirstName = authResponse.User.FirstName,
                            LastName = authResponse.User.LastName,
                            CreatedAt = authResponse.User.CreatedAt,
                            LastLoginAt = authResponse.User.LastLoginAt,
                            IsActive = authResponse.User.IsActive
                        };
                        
                        _isAuthenticated = true;
                        _logger.LogInformation("API login successful for user {Email}", usernameOrEmail);
                        return true;
                    }
                }
                else
                {
                    _logger.LogWarning("API login failed for user {Email}: {StatusCode}", 
                        usernameOrEmail, response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during API login for user {Email}", usernameOrEmail);
            }
            
            return false;
        }
    }

    public async Task<bool> RegisterAsync(string username, string password)
    {
        var mode = await GetAuthenticationModeAsync();
        
        if (mode == "Local Database")
        {
            var result = await _localAuthService.RegisterAsync(username, password);
            _isAuthenticated = result;
            _currentUser = _localAuthService.CurrentUser;
            return result;
        }
        else
        {
            try
            {
                var apiUrl = await GetApiBaseUrlAsync();
                var registerRequest = new RegisterRequestDto
                {
                    Email = username, // In API mode, username is treated as email
                    Password = password,
                    FirstName = "User", // Default values for API registration
                    LastName = "WinUI"
                };

                var response = await _httpClient.PostAsJsonAsync($"{apiUrl}/auth/register", registerRequest);
                if (response.IsSuccessStatusCode)
                {
                    var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
                    if (authResponse != null)
                    {
                        // Store the session token
                        await _secureStorageService.SetAsync("apiSessionToken", authResponse.Token);
                        await _secureStorageService.SetAsync("apiTokenExpiry", authResponse.ExpiresAt.ToString());
                        
                        _currentUser = new ApplicationUser
                        {
                            Id = authResponse.User.Id,
                            Email = authResponse.User.Email,
                            UserName = authResponse.User.Email,
                            FirstName = authResponse.User.FirstName,
                            LastName = authResponse.User.LastName,
                            CreatedAt = authResponse.User.CreatedAt,
                            LastLoginAt = authResponse.User.LastLoginAt,
                            IsActive = authResponse.User.IsActive
                        };
                        
                        _isAuthenticated = true;
                        _logger.LogInformation("API registration successful for user {Email}", username);
                        return true;
                    }
                }
                else
                {
                    _logger.LogWarning("API registration failed for user {Email}: {StatusCode}", 
                        username, response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during API registration for user {Email}", username);
            }
            
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        var mode = await GetAuthenticationModeAsync();
        
        if (mode == "Local Database")
        {
            await _localAuthService.LogoutAsync();
        }
        else
        {
            try
            {
                var apiUrl = await GetApiBaseUrlAsync();
                var token = await _secureStorageService.GetAsync("apiSessionToken");
                
                if (!string.IsNullOrEmpty(token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    
                    await _httpClient.PostAsync($"{apiUrl}/auth/logout", null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during API logout");
            }
            finally
            {
                // Clear stored tokens regardless of API call success
                _secureStorageService.Remove("apiSessionToken");
                _secureStorageService.Remove("apiTokenExpiry");
            }
        }
        
        _isAuthenticated = false;
        _currentUser = null;
    }

    public async Task<bool> IsFirstTimeSetupAsync()
    {
        var mode = await GetAuthenticationModeAsync();
        
        if (mode == "Local Database")
        {
            return await _localAuthService.IsFirstTimeSetupAsync();
        }
        else
        {
            // For API mode, we can always register new users
            // but we check if this device has been used before
            var hasApiToken = await _secureStorageService.GetAsync("apiSessionToken");
            return string.IsNullOrEmpty(hasApiToken);
        }
    }

    public async Task<bool> CheckAuthenticationStatusAsync()
    {
        return await IsAuthenticatedAsync();
    }
}