using Allure.NUnit;
using Allure.NUnit.Attributes;
using NUnit.Framework;
using VaultGuard.Services.Interfaces;
using VaultGuard.Services.Services;

namespace VaultGuard.BackEnd.Tests.Services;

/// <summary>
/// Boundary and formatting coverage for PassphraseGenerator - the only backend, DI-friendly password
/// generator in the codebase (traditional random-character generation is duplicated as private,
/// untestable methods inline in several client UIs - see docs/TESTING.md). Complements
/// PassphraseGeneratorTests, which covers the happy path; every case here targets something that suite
/// doesn't: explicit null options, both clamp directions, an empty separator, a multi-character
/// separator, and the exact shape of the word/number segments.
/// </summary>
[TestFixture]
[AllureNUnit]
[AllureEpic("Vault Data Management")]
[AllureFeature("Password Strength & Generation")]
[AllureParentSuite("Vault Data Management")]
[AllureSuite("Password Strength & Generation")]
public class PassphraseGeneratorEdgeCaseTests
{
    private PassphraseGenerator _gen = null!;

    [AllureBefore("Construct a real PassphraseGenerator instance")]
    [SetUp]
    public void SetUp() => _gen = new PassphraseGenerator();

    [Test]
    public void Generate_NullOptions_BehavesLikeDefaultOptions()
    {
        var phrase = _gen.Generate(null);
        // Same shape as the documented default: 4 words + 1 number segment, "-"-separated.
        var parts = phrase.Split('-');
        Assert.That(parts.Length, Is.EqualTo(5));
        Assert.That(parts[^1], Does.Match("^[0-9]+$"));
    }

    [Test]
    public void Generate_WordCountAboveMaximum_ClampsToTwelve()
    {
        var phrase = _gen.Generate(new PassphraseOptions { WordCount = 999, IncludeNumber = false });
        Assert.That(phrase.Split('-').Length, Is.EqualTo(12));
    }

    [Test]
    public void Generate_NegativeWordCount_ClampsToTwo()
    {
        var phrase = _gen.Generate(new PassphraseOptions { WordCount = -5, IncludeNumber = false });
        Assert.That(phrase.Split('-').Length, Is.EqualTo(2));
    }

    [Test]
    public void Generate_EmptySeparator_ConcatenatesWordsWithNoDelimiter()
    {
        // Separator = "" is not null, so options.Separator ?? "-" keeps it as "" - words run together.
        // Each capitalized word contributes exactly one uppercase letter, so the count of uppercase
        // letters in the result is a reliable way to confirm exactly 3 words were joined with nothing
        // between them, without depending on Split (which needs a real delimiter to work with).
        var phrase = _gen.Generate(new PassphraseOptions { WordCount = 3, Separator = "", Capitalize = true, IncludeNumber = false });
        Assert.That(phrase.Count(char.IsUpper), Is.EqualTo(3));
        Assert.That(phrase.Contains('-'), Is.False);
    }

    [Test]
    public void Generate_MultiCharacterSeparator_SplitsIntoExpectedWordCount()
    {
        var phrase = _gen.Generate(new PassphraseOptions { WordCount = 3, Separator = "::", IncludeNumber = false });
        var parts = phrase.Split(new[] { "::" }, StringSplitOptions.None);
        Assert.That(parts.Length, Is.EqualTo(3));
    }

    [Test]
    public void Generate_IncludeNumberFalse_LastCharacterIsNeverADigit()
    {
        // Words are alphabetic-only, so with no number segment the phrase can never end in a digit.
        var phrase = _gen.Generate(new PassphraseOptions { WordCount = 4, IncludeNumber = false });
        Assert.That(char.IsDigit(phrase[^1]), Is.False);
    }

    [Test]
    public void Generate_IncludeNumberTrue_AppendsATwoDigitNumberInRange()
    {
        var phrase = _gen.Generate(new PassphraseOptions { WordCount = 2, IncludeNumber = true });
        var lastPart = phrase.Split('-')[^1];
        var number = int.Parse(lastPart);
        Assert.That(number, Is.InRange(10, 99), "RandomNumberGenerator.GetInt32(10, 100) is [10, 100), i.e. 10-99 inclusive.");
    }

    [Test]
    public void Generate_WordSegments_ContainOnlyLetters()
    {
        var phrase = _gen.Generate(new PassphraseOptions { WordCount = 5, IncludeNumber = false });
        foreach (var word in phrase.Split('-'))
            Assert.That(word, Does.Match("^[A-Za-z]+$"), $"'{word}' should be alphabetic-only.");
    }
}
