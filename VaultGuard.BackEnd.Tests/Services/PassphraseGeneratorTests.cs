using Allure.NUnit;
using NUnit.Framework;
using VaultGuard.Services.Interfaces;
using VaultGuard.Services.Services;

namespace VaultGuard.BackEnd.Tests.Services;

[TestFixture]
[AllureNUnit]
public class PassphraseGeneratorTests
{
    private PassphraseGenerator _gen = null!;

    [SetUp]
    public void Setup() => _gen = new PassphraseGenerator();

    [Test]
    public void Generate_DefaultOptions_HasFourWordsPlusNumber()
    {
        var phrase = _gen.Generate();
        // 4 words + 1 number segment = 5 parts joined by "-"
        var parts = phrase.Split('-');
        Assert.That(parts.Length, Is.EqualTo(5));
        Assert.That(parts[^1], Does.Match("^[0-9]+$"));
    }

    [Test]
    public void Generate_RespectsWordCountAndSeparator()
    {
        var phrase = _gen.Generate(new PassphraseOptions { WordCount = 3, Separator = ".", IncludeNumber = false });
        var parts = phrase.Split('.');
        Assert.That(parts.Length, Is.EqualTo(3));
    }

    [Test]
    public void Generate_Capitalize_CapitalisesEachWord()
    {
        var phrase = _gen.Generate(new PassphraseOptions { WordCount = 4, Capitalize = true, IncludeNumber = false });
        foreach (var word in phrase.Split('-'))
            Assert.That(char.IsUpper(word[0]), Is.True, $"'{word}' should start uppercase");
    }

    [Test]
    public void Generate_NoCapitalize_IsLowercase()
    {
        var phrase = _gen.Generate(new PassphraseOptions { WordCount = 4, Capitalize = false, IncludeNumber = false });
        foreach (var word in phrase.Split('-'))
            Assert.That(char.IsLower(word[0]), Is.True);
    }

    [Test]
    public void Generate_ProducesVariedOutput()
    {
        var a = _gen.Generate();
        var b = _gen.Generate();
        var c = _gen.Generate();
        Assert.That(new[] { a, b, c }.Distinct().Count(), Is.GreaterThan(1));
    }

    [Test]
    public void Generate_ClampsWordCountToSaneRange()
    {
        var tiny = _gen.Generate(new PassphraseOptions { WordCount = 0, IncludeNumber = false });
        Assert.That(tiny.Split('-').Length, Is.GreaterThanOrEqualTo(2));
    }

    [Test]
    public void WordListSize_IsReasonablyLarge()
    {
        Assert.That(_gen.WordListSize, Is.GreaterThan(100));
    }
}
