using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using PasswordManager.Services.Extensions;
using PasswordManager.Services.Interfaces;
using PasswordManager.Services.Services;
using NUnit.Framework;

namespace PasswordManager.BackEnd.Tests.Services;

/// <summary>
/// Tests for the authentication service contextual registration
/// </summary>
[TestFixture]
public class AuthServiceRegistrationTests
{
    [Test]
    public void AddContextualAuthService_WithoutJSRuntime_ReturnsServerAuthService()
    {
        // Arrange
        var services = new ServiceCollection();
        // Do not register IJSRuntime - simulating non-Blazor context
        
        // Act
        services.AddContextualAuthService();
        var serviceProvider = services.BuildServiceProvider();
        
        // Assert
        var authService = serviceProvider.GetService<IAuthService>();
        Assert.That(authService, Is.Not.Null);
        Assert.That(authService, Is.TypeOf<ServerAuthService>());
    }

    [Test] 
    public void AddContextualAuthService_WithJSRuntime_ReturnsIdentityAuthService()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Add a mock IJSRuntime to simulate Blazor context
        services.AddSingleton<IJSRuntime>(provider => new MockJSRuntime());
        
        // Also add dependencies that IdentityAuthService needs
        services.AddLogging();
        
        // Act
        services.AddContextualAuthService();
        
        // Assert - Should select IdentityAuthService when IJSRuntime is available
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IAuthService));
        Assert.That(descriptor, Is.Not.Null);
        Assert.That(descriptor!.ImplementationType, Is.EqualTo(typeof(IdentityAuthService)));
    }

    [Test]
    public void AddBlazorAuthService_Always_ReturnsIdentityAuthService()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Act
        services.AddBlazorAuthService();
        
        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IAuthService));
        Assert.That(descriptor, Is.Not.Null);
        Assert.That(descriptor!.ImplementationType, Is.EqualTo(typeof(IdentityAuthService)));
    }

    [Test]
    public void AddServerAuthService_Always_ReturnsServerAuthService()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Act
        services.AddServerAuthService();
        
        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IAuthService));
        Assert.That(descriptor, Is.Not.Null);
        Assert.That(descriptor!.ImplementationType, Is.EqualTo(typeof(ServerAuthService)));
    }

    /// <summary>
    /// Mock JSRuntime for testing
    /// </summary>
    private class MockJSRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            return ValueTask.FromResult(default(TValue)!);
        }
    }
}