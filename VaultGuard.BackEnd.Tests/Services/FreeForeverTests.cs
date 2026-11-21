using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using VaultGuard.Models.Licensing;
using VaultGuard.Services.Interfaces;
using VaultGuard.Services.Services;

namespace VaultGuard.BackEnd.Tests.Services;

/// <summary>Vault Guard is free forever: no feature may ever depend on a paid licence.</summary>
[TestFixture]
public class FreeForeverTests
{
    [Test]
    public void EveryFeature_IsAvailable_WithNoLicence()
    {
        var client = Create();
        foreach (var feature in Enum.GetValues<LicenseFeature>())
            Assert.That(client.HasFeature(feature), Is.True, $"{feature} must be free");
        Assert.That(client.CurrentFeatures, Is.EqualTo(LicenseFeature.All));
        Assert.That(client.IsProUnlocked, Is.True);
    }

    [Test]
    public void ClearingTheLicence_ChangesNothing()
    {
        var client = Create();
        client.ClearCached();
        Assert.That(client.HasFeature(LicenseFeature.ApiAccess | LicenseFeature.CloudBackup | LicenseFeature.UnlimitedDevices), Is.True);
    }

    private static LicenseClientService Create()
    {
        var ctor = typeof(LicenseClientService).GetConstructors().Single();
        var args = ctor.GetParameters().Select(p =>
            p.ParameterType == typeof(IAppSettingsService) ? Mock.Of<IAppSettingsService>()
            : p.ParameterType.IsGenericType && p.ParameterType.GetGenericTypeDefinition() == typeof(IOptions<>)
                ? Activator.CreateInstance(typeof(OptionsWrapper<>).MakeGenericType(p.ParameterType.GetGenericArguments()[0]),
                    Activator.CreateInstance(p.ParameterType.GetGenericArguments()[0]))
            : p.ParameterType.IsGenericType && p.ParameterType.GetGenericTypeDefinition() == typeof(Microsoft.Extensions.Logging.ILogger<>)
                ? Activator.CreateInstance(typeof(NullLogger<>).MakeGenericType(p.ParameterType.GetGenericArguments()[0]))
            : p.ParameterType.IsInterface ? typeof(Mock).GetMethod(nameof(Mock.Of), Type.EmptyTypes)!.MakeGenericMethod(p.ParameterType).Invoke(null, null)
            : null).ToArray();
        return (LicenseClientService)ctor.Invoke(args);
    }
}
