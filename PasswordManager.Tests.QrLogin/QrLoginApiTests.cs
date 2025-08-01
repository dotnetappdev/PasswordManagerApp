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
    public async Task GetQrLoginStatus_WithInvalidToken_ShouldReturnExpiredStatus()
    {
        // Arrange
        var invalidToken = "invalid-token-123";

        // Act
        var response = await _client.GetAsync($"/api/auth/qr/status/{invalidToken}");

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var statusResponse = JsonSerializer.Deserialize<QrLoginStatusResponseDto>(content, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        Assert.NotNull(statusResponse);
        Assert.Equal(QrLoginStatus.Expired, statusResponse.Status);
        Assert.True(statusResponse.IsExpired);
    }

    [Fact]
    public async Task GetQrLoginStatus_WithEmptyToken_ShouldReturnBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/qr/status/");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticateQrLogin_WithInvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidRequest = new QrLoginAuthenticateRequestDto
        {
            Token = "", // Empty token
            Email = "invalid-email", // Invalid email format
            Password = "" // Empty password
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/qr/authenticate", invalidRequest);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticateQrLogin_WithNonExistentToken_ShouldReturnError()
    {
        // Arrange
        var request = new QrLoginAuthenticateRequestDto
        {
            Token = Guid.NewGuid().ToString("N"),
            Email = "test@example.com",
            Password = "validpassword123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/qr/authenticate", request);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        var authResponse = JsonSerializer.Deserialize<QrLoginAuthenticateResponseDto>(content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(authResponse);
        Assert.False(authResponse.Success);
        Assert.Contains("token", authResponse.Message?.ToLower() ?? "");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid-token")]
    [InlineData("12345")] // Too short
    public async Task GetQrLoginStatus_WithMalformedTokens_ShouldHandleGracefully(string token)
    {
        // Act
        var response = await _client.GetAsync($"/api/auth/qr/status/{Uri.EscapeDataString(token)}");

        // Assert
        if (string.IsNullOrWhiteSpace(token))
        {
            // Empty tokens should result in bad request or not found
            Assert.True(response.StatusCode == System.Net.HttpStatusCode.BadRequest ||
                       response.StatusCode == System.Net.HttpStatusCode.NotFound);
        }
        else
        {
            // Invalid tokens should return expired status
            Assert.True(response.IsSuccessStatusCode);
            var content = await response.Content.ReadAsStringAsync();
            var statusResponse = JsonSerializer.Deserialize<QrLoginStatusResponseDto>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.Equal(QrLoginStatus.Expired, statusResponse?.Status);
        }
    }

    [Fact]
    public async Task QrLoginEndpoints_ShouldHaveCorrectContentTypes()
    {
        // Test that all QR login endpoints return JSON content type

        // 1. Status endpoint with invalid token
        var statusResponse = await _client.GetAsync("/api/auth/qr/status/invalid");
        Assert.Contains("application/json", statusResponse.Content.Headers.ContentType?.ToString() ?? "");

        // 2. Authenticate endpoint with empty request
        var authResponse = await _client.PostAsJsonAsync("/api/auth/qr/authenticate", new { });
        Assert.Contains("application/json", authResponse.Content.Headers.ContentType?.ToString() ?? "");
    }

    [Fact]
    public async Task QrLoginEndpoints_ShouldHandleConcurrentRequests()
    {
        // Test that the API can handle multiple concurrent requests without issues
        var tasks = new List<Task<HttpResponseMessage>>();

        for (int i = 0; i < 10; i++)
        {
            var token = $"test-token-{i}";
            tasks.Add(_client.GetAsync($"/api/auth/qr/status/{token}"));
        }

        // Act
        var responses = await Task.WhenAll(tasks);

        // Assert
        Assert.All(responses, response =>
        {
            Assert.True(response.IsSuccessStatusCode);
        });

        // Cleanup
        foreach (var response in responses)
        {
            response.Dispose();
        }
    }
}