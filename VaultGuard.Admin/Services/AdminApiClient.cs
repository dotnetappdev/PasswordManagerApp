using System.Net.Http.Headers;
using System.Net.Http.Json;
using VaultGuard.Models.DTOs.Auth;
using VaultGuard.Models.Tenancy;

namespace VaultGuard.Admin.Services;

public class LoginResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public string? AccessToken { get; init; }
    public AdminWhoAmIResponse? Who { get; init; }

    public static LoginResult Fail(string error) => new() { Success = false, Error = error };
    public static LoginResult Ok(string token, AdminWhoAmIResponse who) => new() { Success = true, AccessToken = token, Who = who };
}

/// <summary>
/// The only thing VaultGuard.Admin talks to for data: VaultGuard.API. Every method here attaches the
/// current super admin's bearer token (from <see cref="AdminSession"/>) — the API enforces
/// [RequireSuperAdmin] on every endpoint this calls except login/register themselves. See
/// docs/ADMIN_MULTITENANCY.md.
/// </summary>
public class AdminApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AdminSession _session;

    public AdminApiClient(IHttpClientFactory httpClientFactory, AdminSession session)
    {
        _httpClientFactory = httpClientFactory;
        _session = session;
    }

    private HttpClient CreateClient(bool authenticated = true)
    {
        var client = _httpClientFactory.CreateClient("VaultGuardAPI");
        if (authenticated && !string.IsNullOrEmpty(_session.AccessToken))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _session.AccessToken);
        return client;
    }

    // ── Auth ─────────────────────────────────────────────────────────────────────────────────────

    public async Task<LoginResult> LoginAsync(string email, string password)
    {
        var client = CreateClient(authenticated: false);
        HttpResponseMessage resp;
        try
        {
            resp = await client.PostAsJsonAsync("login?useCookies=false", new { email, password });
        }
        catch (Exception ex)
        {
            return LoginResult.Fail($"Could not reach the API: {ex.Message}");
        }

        if (!resp.IsSuccessStatusCode)
            return LoginResult.Fail("Invalid email or password.");

        var token = await resp.Content.ReadFromJsonAsync<AccessTokenResponse>();
        if (string.IsNullOrEmpty(token?.AccessToken))
            return LoginResult.Fail("Login response from the API was empty.");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        var meResp = await client.GetAsync("api/admin/me");
        if (!meResp.IsSuccessStatusCode)
            return LoginResult.Fail("Could not verify this account with the API.");

        var who = await meResp.Content.ReadFromJsonAsync<AdminWhoAmIResponse>();
        if (who is null) return LoginResult.Fail("The API returned an empty identity response.");
        if (!who.IsSuperAdmin) return LoginResult.Fail("This account does not have SuperAdmin access.");

        return LoginResult.Ok(token.AccessToken, who);
    }

    public async Task<(bool Success, string? Error)> RegisterAsync(string email, string password)
    {
        var client = CreateClient(authenticated: false);
        HttpResponseMessage resp;
        try
        {
            resp = await client.PostAsJsonAsync("register", new { email, password });
        }
        catch (Exception ex)
        {
            return (false, $"Could not reach the API: {ex.Message}");
        }

        if (resp.IsSuccessStatusCode) return (true, null);
        var body = await resp.Content.ReadAsStringAsync();
        return (false, string.IsNullOrWhiteSpace(body) ? $"Registration failed ({(int)resp.StatusCode})." : body);
    }

    // ── Users / roles ────────────────────────────────────────────────────────────────────────────

    public async Task<List<UserDto>> GetUsersAsync() =>
        await CreateClient().GetFromJsonAsync<List<UserDto>>("api/userprofile") ?? new();

    public async Task<UserProfileDetailsDto?> GetUserAsync(string id) =>
        await CreateClient().GetFromJsonAsync<UserProfileDetailsDto>($"api/userprofile/{id}");

    public async Task<(bool Success, string? Error)> UpdateUserAsync(UpdateUserProfileDto dto)
    {
        var resp = await CreateClient().PutAsJsonAsync($"api/userprofile/{dto.Id}", dto);
        return resp.IsSuccessStatusCode ? (true, null) : (false, await resp.Content.ReadAsStringAsync());
    }

    public async Task<bool> DeactivateUserAsync(string id) =>
        (await CreateClient().PutAsync($"api/userprofile/{id}/deactivate", null)).IsSuccessStatusCode;

    public async Task<bool> ReactivateUserAsync(string id) =>
        (await CreateClient().PutAsync($"api/userprofile/{id}/reactivate", null)).IsSuccessStatusCode;

    public async Task<bool> DeleteUserAsync(string id) =>
        (await CreateClient().DeleteAsync($"api/userprofile/{id}")).IsSuccessStatusCode;

    public async Task<List<string>> GetAllRolesAsync() =>
        await CreateClient().GetFromJsonAsync<List<string>>("api/admin/roles") ?? new();

    public async Task<List<string>> GetUserRolesAsync(string userId) =>
        await CreateClient().GetFromJsonAsync<List<string>>($"api/admin/users/{userId}/roles") ?? new();

    public async Task<bool> AddUserToRoleAsync(string userId, string role) =>
        (await CreateClient().PostAsync($"api/admin/users/{userId}/roles/{role}", null)).IsSuccessStatusCode;

    public async Task<bool> RemoveUserFromRoleAsync(string userId, string role) =>
        (await CreateClient().DeleteAsync($"api/admin/users/{userId}/roles/{role}")).IsSuccessStatusCode;

    public async Task<bool> SetUserTenantAsync(string userId, Guid? tenantId) =>
        (await CreateClient().PutAsJsonAsync($"api/admin/users/{userId}/tenant", new { TenantId = tenantId })).IsSuccessStatusCode;

    // ── Dashboard / API keys ─────────────────────────────────────────────────────────────────────

    public async Task<AdminDashboardStats?> GetDashboardStatsAsync() =>
        await CreateClient().GetFromJsonAsync<AdminDashboardStats>("api/admin/dashboard-stats");

    public async Task<List<AdminApiKeyResponse>> GetAllApiKeysAsync() =>
        await CreateClient().GetFromJsonAsync<List<AdminApiKeyResponse>>("api/admin/api-keys") ?? new();

    // ── Licensing ────────────────────────────────────────────────────────────────────────────────

    public async Task<List<LicenseKeyResponse>> GetLicensesAsync(string? customerEmail = null) =>
        await CreateClient().GetFromJsonAsync<List<LicenseKeyResponse>>(
            string.IsNullOrWhiteSpace(customerEmail) ? "api/license" : $"api/license?customerEmail={Uri.EscapeDataString(customerEmail)}") ?? new();

    public async Task<List<LicenseKeyResponse>> GetLicensesForUserAsync(string userId) =>
        await CreateClient().GetFromJsonAsync<List<LicenseKeyResponse>>($"api/license?userId={Uri.EscapeDataString(userId)}") ?? new();

    public async Task<(bool Success, LicenseKeyResponse? License, string? Error)> IssueLicenseAsync(IssueLicenseRequest request)
    {
        var resp = await CreateClient().PostAsJsonAsync("api/license", request);
        if (!resp.IsSuccessStatusCode) return (false, null, await resp.Content.ReadAsStringAsync());
        return (true, await resp.Content.ReadFromJsonAsync<LicenseKeyResponse>(), null);
    }

    public async Task<(bool Success, LicenseKeyResponse? License, string? Error)> UpdateLicenseAsync(Guid id, UpdateLicenseRequest request)
    {
        var resp = await CreateClient().PutAsJsonAsync($"api/license/{id}", request);
        if (!resp.IsSuccessStatusCode) return (false, null, await resp.Content.ReadAsStringAsync());
        return (true, await resp.Content.ReadFromJsonAsync<LicenseKeyResponse>(), null);
    }

    public async Task<bool> DeleteLicenseAsync(Guid id) =>
        (await CreateClient().DeleteAsync($"api/license/{id}")).IsSuccessStatusCode;

    public async Task<bool> RevokeLicenseAsync(Guid id, string? reason) =>
        (await CreateClient().PostAsJsonAsync($"api/license/{id}/revoke", new { reason })).IsSuccessStatusCode;

    public async Task<LicensingSettingsResponse?> GetLicensingSettingsAsync() =>
        await CreateClient().GetFromJsonAsync<LicensingSettingsResponse>("api/license/settings");

    public async Task<LicensingSettingsResponse?> UpdateLicensingDefaultsAsync(UpdateLicensingDefaultsRequest request)
    {
        var resp = await CreateClient().PutAsJsonAsync("api/license/settings", request);
        return resp.IsSuccessStatusCode ? await resp.Content.ReadFromJsonAsync<LicensingSettingsResponse>() : null;
    }

    public async Task<(bool Success, LicensingSettingsResponse? Settings, string? Error)> GenerateSigningKeysAsync()
    {
        var resp = await CreateClient().PostAsync("api/license/settings/generate-keys", null);
        if (!resp.IsSuccessStatusCode) return (false, null, await resp.Content.ReadAsStringAsync());
        return (true, await resp.Content.ReadFromJsonAsync<LicensingSettingsResponse>(), null);
    }

    // ── Tenants ──────────────────────────────────────────────────────────────────────────────────

    public async Task<List<TenantResponse>> GetTenantsAsync() =>
        await CreateClient().GetFromJsonAsync<List<TenantResponse>>("api/tenants") ?? new();

    public async Task<(bool Success, TenantResponse? Tenant, string? Error)> CreateTenantAsync(CreateTenantRequest request)
    {
        var resp = await CreateClient().PostAsJsonAsync("api/tenants", request);
        if (!resp.IsSuccessStatusCode) return (false, null, await resp.Content.ReadAsStringAsync());
        return (true, await resp.Content.ReadFromJsonAsync<TenantResponse>(), null);
    }

    public async Task<bool> UpdateTenantAsync(Guid id, string? name, TenantStatus? status) =>
        (await CreateClient().PutAsJsonAsync($"api/tenants/{id}", new { name, status })).IsSuccessStatusCode;

    public async Task<bool> DeleteTenantAsync(Guid id) =>
        (await CreateClient().DeleteAsync($"api/tenants/{id}")).IsSuccessStatusCode;

    public async Task<(bool Success, CustomDomainSetupResponse? Setup, string? Error)> SetCustomDomainAsync(Guid tenantId, string domain)
    {
        var resp = await CreateClient().PostAsJsonAsync($"api/tenants/{tenantId}/custom-domain", new { domain });
        if (!resp.IsSuccessStatusCode) return (false, null, await resp.Content.ReadAsStringAsync());
        return (true, await resp.Content.ReadFromJsonAsync<CustomDomainSetupResponse>(), null);
    }

    public async Task<bool> VerifyCustomDomainAsync(Guid tenantId) =>
        (await CreateClient().PostAsync($"api/tenants/{tenantId}/custom-domain/verify", null)).IsSuccessStatusCode;

    // ── Subscriptions ────────────────────────────────────────────────────────────────────────────

    public async Task<List<SubscriptionResponse>> GetSubscriptionsAsync() =>
        await CreateClient().GetFromJsonAsync<List<SubscriptionResponse>>("api/subscriptions") ?? new();

    public async Task<(bool Success, string? Error)> CreateSubscriptionAsync(CreateSubscriptionRequest request)
    {
        var resp = await CreateClient().PostAsJsonAsync("api/subscriptions", request);
        return resp.IsSuccessStatusCode ? (true, null) : (false, await resp.Content.ReadAsStringAsync());
    }

    public async Task<bool> CancelSubscriptionAsync(Guid id) =>
        (await CreateClient().PostAsync($"api/subscriptions/{id}/cancel", null)).IsSuccessStatusCode;
}
