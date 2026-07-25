using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using MudBlazor.Services;
using VaultGuard.Admin.Components;
using VaultGuard.Admin.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped<AdminSession>();
builder.Services.AddScoped<AdminApiClient>();

// The API this control panel is a thin client of — every data operation, and all licensing crypto,
// stays server-side there. Configurable per environment via ApiSettings:BaseUrl.
builder.Services.AddHttpClient("VaultGuardAPI", client =>
{
    var baseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7001";
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
});

// Cookie auth for the Admin site itself. This app has no Identity store of its own — logging in here
// means "prove to VaultGuard.API's Identity system that you're a SuperAdmin" (see AdminApiClient.LoginAsync),
// then cache that outcome (+ the API bearer token, so later calls don't need to re-authenticate) in this
// cookie. See docs/ADMIN_MULTITENANCY.md.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "VaultGuardAdmin.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });
builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

// ── Auth endpoints ───────────────────────────────────────────────────────────────────────────────
// Plain form posts (not Blazor interactive handlers) so HttpContext.SignInAsync/SignOutAsync can run
// before the response starts — the standard pattern for cookie auth alongside Blazor Server.
// Antiforgery-validated by hand since these are plain MapPost endpoints, not Blazor's own SSR form
// handler (which validates automatically) — Login.razor/Signup.razor render <AntiforgeryToken />.

async Task<bool> ValidAntiforgeryAsync(HttpContext http, IAntiforgery antiforgery)
{
    try { await antiforgery.ValidateRequestAsync(http); return true; }
    catch (AntiforgeryValidationException) { return false; }
}

app.MapPost("/admin-auth/login", async (HttpContext http, AdminApiClient api, IAntiforgery antiforgery) =>
{
    if (!await ValidAntiforgeryAsync(http, antiforgery))
        return Results.Redirect("/login?error=Your+session+expired.+Please+try+again.");

    var form = await http.Request.ReadFormAsync();
    var email = form["email"].ToString();
    var password = form["password"].ToString();

    var result = await api.LoginAsync(email, password);
    if (!result.Success)
        return Results.Redirect($"/login?error={Uri.EscapeDataString(result.Error ?? "Login failed.")}");

    var who = result.Who!;
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, who.Id),
        new(ClaimTypes.Email, who.Email),
        new("display_name", who.DisplayName),
        new("access_token", result.AccessToken!)
    };
    claims.AddRange(who.Roles.Select(r => new Claim(ClaimTypes.Role, r)));

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

    return Results.Redirect("/");
});

app.MapPost("/admin-auth/register", async (HttpContext http, AdminApiClient api, IAntiforgery antiforgery) =>
{
    if (!await ValidAntiforgeryAsync(http, antiforgery))
        return Results.Redirect("/signup?error=Your+session+expired.+Please+try+again.");

    var form = await http.Request.ReadFormAsync();
    var email = form["email"].ToString();
    var password = form["password"].ToString();

    var (success, error) = await api.RegisterAsync(email, password);
    return success
        ? Results.Redirect("/login?registered=1")
        : Results.Redirect($"/signup?error={Uri.EscapeDataString(error ?? "Registration failed.")}");
});

app.MapPost("/admin-auth/logout", async (HttpContext http) =>
{
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
