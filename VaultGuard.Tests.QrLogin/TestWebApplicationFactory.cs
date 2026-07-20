using Microsoft.AspNetCore.Mvc.Testing;

namespace VaultGuard.Tests.QrLogin;

/// <summary>
/// Test host for the API that turns off the per-IP rate limiter (RateLimiting:Enabled=false).
/// In production the "auth" policy only allows 10 requests/minute per IP; because every test in a
/// class shares one loopback client, that limit is exhausted mid-run and endpoints start returning
/// 429 instead of the behaviour under test. Disabling it here lets each test assert the real
/// endpoint response.
///
/// The flag is set as an environment variable (read via AddEnvironmentVariables) rather than through
/// ConfigureAppConfiguration because Program.cs reads it eagerly while the host is being built, before
/// the factory's ConfigureAppConfiguration sources are applied.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    public TestWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("RateLimiting__Enabled", "false");
    }
}
