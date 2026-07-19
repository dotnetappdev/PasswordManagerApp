using Allure.NUnit;
using Allure.NUnit.Attributes;
using Microsoft.Data.SqlClient;
using Moq;
using NUnit.Framework;
using VaultGuard.API.Configuration;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.BackEnd.Tests.Configuration;

[TestFixture]
[AllureNUnit]
[AllureEpic("Platform & Infrastructure")]
[AllureFeature("Configuration")]
public class SharedMachineDatabaseSettingsTests
{
    [Test]
    public void NormalizeProvider_StripsSpacesAndNormalizesCase()
    {
        var result = SharedMachineDatabaseSettings.NormalizeProvider("SQL Server");

        Assert.That(result, Is.EqualTo("sqlserver"));
    }

    [Test]
    public void TryBuildSqlServerConnectionString_WithSqlAuth_BuildsExpectedValues()
    {
        var settings = CreateSettings(new Dictionary<string, string>
        {
            ["dbserver"] = "sql.example.com",
            ["dbname"] = "VaultGuard",
            ["dbusername"] = "vaultguard",
            ["dbpassword"] = "secret"
        });

        var success = SharedMachineDatabaseSettings.TryBuildSqlServerConnectionString(settings.Object, out var connectionString);

        Assert.That(success, Is.True);
        Assert.That(connectionString, Is.Not.Null.And.Not.Empty);

        var builder = new SqlConnectionStringBuilder(connectionString);
        Assert.Multiple(() =>
        {
            Assert.That(builder.DataSource, Is.EqualTo("sql.example.com"));
            Assert.That(builder.InitialCatalog, Is.EqualTo("VaultGuard"));
            Assert.That(builder.UserID, Is.EqualTo("vaultguard"));
            Assert.That(builder.Password, Is.EqualTo("secret"));
            Assert.That(builder.TrustServerCertificate, Is.True);
            Assert.That(connectionString, Does.Contain("Encrypt=True"));
        });
    }

    [Test]
    public void TryBuildSqlServerConnectionString_WithoutServerOrDatabase_ReturnsFalse()
    {
        var settings = CreateSettings(new Dictionary<string, string>());

        var success = SharedMachineDatabaseSettings.TryBuildSqlServerConnectionString(settings.Object, out var connectionString);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.False);
            Assert.That(connectionString, Is.Null);
        });
    }

    private static Mock<IAppSettingsService> CreateSettings(Dictionary<string, string> values)
    {
        var settings = new Mock<IAppSettingsService>();
        settings.Setup(x => x.Get(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string key, string fallback) => values.TryGetValue(key, out var value) ? value : fallback);
        return settings;
    }
}
