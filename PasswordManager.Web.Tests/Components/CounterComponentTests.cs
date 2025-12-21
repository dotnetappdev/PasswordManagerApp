using Bunit;
using NUnit.Framework;
using PasswordManager.Web.Components.Pages;

namespace PasswordManager.Web.Tests.Components;

[TestFixture]
public class CounterComponentTests : TestContext
{
    [Test]
    public void Counter_InitialState_ShouldBeZero()
    {
        // Act
        var cut = RenderComponent<Counter>();

        // Assert
        var paragraph = cut.Find("p[role='status']");
        Assert.That(paragraph.TextContent, Does.Contain("Current count: 0"));
    }

    [Test]
    public void Counter_WhenButtonClicked_ShouldIncrement()
    {
        // Arrange
        var cut = RenderComponent<Counter>();
        var button = cut.Find("button");

        // Act
        button.Click();

        // Assert
        var paragraph = cut.Find("p[role='status']");
        Assert.That(paragraph.TextContent, Does.Contain("Current count: 1"));
    }

    [Test]
    public void Counter_WhenButtonClickedMultipleTimes_ShouldIncrementCorrectly()
    {
        // Arrange
        var cut = RenderComponent<Counter>();
        var button = cut.Find("button");

        // Act
        button.Click();
        button.Click();
        button.Click();

        // Assert
        var paragraph = cut.Find("p[role='status']");
        Assert.That(paragraph.TextContent, Does.Contain("Current count: 3"));
    }

    [Test]
    public void Counter_ShouldHaveCorrectTitle()
    {
        // Act
        var cut = RenderComponent<Counter>();

        // Assert
        var heading = cut.Find("h1");
        Assert.That(heading.TextContent, Is.EqualTo("Counter"));
    }

    [Test]
    public void Counter_ButtonShouldHaveCorrectClass()
    {
        // Act
        var cut = RenderComponent<Counter>();

        // Assert
        var button = cut.Find("button");
        Assert.That(button.ClassList, Does.Contain("btn"));
        Assert.That(button.ClassList, Does.Contain("btn-primary"));
    }
}
