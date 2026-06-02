using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PasswordManager.Components.Shared.Components.Database;
using PasswordManager.Models.Configuration;
using PasswordManager.Services.Interfaces;
using PasswordManager.Web.Components.Pages;

namespace PasswordManager.Web.Tests.Components;

[TestFixture]
public class SetupFlowTests
{
    [Test]
    public void DatabaseStartupWrapper_WhenFirstRun_RedirectsToSetup()
    {
        using var ctx = new Bunit.TestContext();
        ctx.Services.AddLogging();

        var configService = new Mock<IDatabaseConfigurationService>();
        configService.Setup(x => x.ShouldShowDatabaseSelection()).Returns(true);
        configService.Setup(x => x.IsFirstRunAsync()).ReturnsAsync(true);

        ctx.Services.AddSingleton(configService.Object);

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();

        var cut = ctx.RenderComponent<DatabaseStartupWrapper>(parameters => parameters
            .AddChildContent("<div id='child-content'>App</div>"));

        cut.WaitForAssertion(() => Assert.That(navigationManager.Uri, Does.EndWith("/setup")));
    }

    [Test]
    public void SetupPage_ShowsProviderSelectorAndConfigurationTogether()
    {
        using var ctx = new Bunit.TestContext();
        ctx.Services.AddLogging();

        var configService = new Mock<IDatabaseConfigurationService>();
        configService.Setup(x => x.GetConfigurationAsync()).ReturnsAsync(new DatabaseConfiguration
        {
            Provider = DatabaseProvider.Sqlite,
            IsFirstRun = true,
            Sqlite = new SqliteConfig()
        });

        ctx.Services.AddSingleton(configService.Object);

        var cut = ctx.RenderComponent<Setup>();

        Assert.That(cut.Markup, Does.Contain("Database Type"));
        Assert.That(cut.Markup, Does.Contain("Connection Settings"));
        Assert.That(cut.FindAll("button").Any(button => button.TextContent.Trim() == "Continue"), Is.False);

        var sqlitePathInput = cut.Find("input[placeholder='passwordmanager.db']");
        Assert.That(sqlitePathInput, Is.Not.Null);
    }

    [Test]
    public void SetupPage_WhenProviderChanges_UpdatesConfigurationPanelWithoutStepper()
    {
        using var ctx = new Bunit.TestContext();
        ctx.Services.AddLogging();

        var configService = new Mock<IDatabaseConfigurationService>();
        configService.Setup(x => x.GetConfigurationAsync()).ReturnsAsync(new DatabaseConfiguration
        {
            Provider = DatabaseProvider.Sqlite,
            IsFirstRun = true,
            Sqlite = new SqliteConfig()
        });

        ctx.Services.AddSingleton(configService.Object);

        var cut = ctx.RenderComponent<Setup>();

        cut.FindAll("button")
            .Single(button => button.TextContent.Contains("Supabase"))
            .Click();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Project URL"));
            Assert.That(cut.FindAll("button").Any(button => button.TextContent.Trim() == "Continue"), Is.False);
        });
    }
}
