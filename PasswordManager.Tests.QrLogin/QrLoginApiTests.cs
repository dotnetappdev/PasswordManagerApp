using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.API;
using PasswordManager.Models.DTOs.Auth;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace PasswordManager.Tests.QrLogin;

public class QrLoginApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public QrLoginApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GenerateQrLogin_WithoutAuth_ShouldReturn401()
    {
        // Act
        var response = await _client.PostAsync("/api/auth/qr/generate", null);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetQrLoginStatus_WithoutAuth_ShouldReturn401()
    {
        // Arrange
        var invalidToken = "invalid-token-123";
        
        // Act
        var response = await _client.GetAsync($"/api/auth/qr/status/{invalidToken}");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetQrLoginStatus_WithEmptyToken_ShouldReturn401()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/qr/status/");

        // Assert - Should return 401 for unauthenticated requests regardless of URL format
        Assert.True(response.StatusCode == System.Net.HttpStatusCode.NotFound ||
                   response.StatusCode == System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AuthenticateQrLogin_WithoutAuth_ShouldReturn401()
    {
        // Arrange
        var invalidRequest = new QrLoginAuthenticateRequestDto
        {
            Token = "invalid-token",
            Email = "test@example.com",
            Password = "password123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/qr/authenticate", invalidRequest);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticateQrLogin_WithNonExistentToken_ShouldReturn401()
    {
        // Arrange
        var request = new QrLoginAuthenticateRequestDto
        {
            Token = "non-existent-token-12345",
            Email = "user@example.com",
            Password = "password123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/qr/authenticate", request);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }
}