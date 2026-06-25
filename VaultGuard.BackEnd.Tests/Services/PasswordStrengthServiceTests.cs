using NUnit.Framework;
using VaultGuard.Services.Interfaces;
using VaultGuard.Services.Services;

namespace VaultGuard.BackEnd.Tests.Services;

[TestFixture]
public class PasswordStrengthServiceTests
{
    private PasswordStrengthService _service = null!;

    [SetUp]
    public void Setup() => _service = new PasswordStrengthService();

    [Test]
    public void Evaluate_Null_ReturnsVeryWeakWithSuggestion()
    {
        var result = _service.Evaluate(null);

        Assert.That(result.Score, Is.EqualTo(0));
        Assert.That(result.Level, Is.EqualTo(PasswordStrengthLevel.VeryWeak));
        Assert.That(result.Suggestions, Is.Not.Empty);
    }

    [Test]
    public void Evaluate_Empty_ReturnsVeryWeak()
    {
        var result = _service.Evaluate(string.Empty);
        Assert.That(result.Score, Is.EqualTo(0));
        Assert.That(result.Level, Is.EqualTo(PasswordStrengthLevel.VeryWeak));
    }

    [TestCase("password")]
    [TestCase("123456")]
    [TestCase("qwerty")]
    [TestCase("letmein")]
    public void Evaluate_CommonPassword_IsVeryWeak(string pwd)
    {
        var result = _service.Evaluate(pwd);
        Assert.That(result.Score, Is.LessThanOrEqualTo(1));
        Assert.That(result.Suggestions, Is.Not.Empty);
    }

    [Test]
    public void Evaluate_RepeatedCharacters_IsPenalised()
    {
        var repeated = _service.Evaluate("aaaaaaaaaaaa");
        Assert.That(repeated.Score, Is.LessThanOrEqualTo(2));
        Assert.That(repeated.Suggestions, Has.Some.Contains("repeated"));
    }

    [Test]
    public void Evaluate_Sequence_IsPenalised()
    {
        var seq = _service.Evaluate("abcdefgh");
        Assert.That(seq.Suggestions, Has.Some.Contains("sequence").IgnoreCase);
    }

    [Test]
    public void Evaluate_ShortPassword_CannotBeStrong()
    {
        // Even with all character classes, < 8 chars is capped to "weak".
        var result = _service.Evaluate("Ab1!");
        Assert.That(result.Score, Is.LessThanOrEqualTo(1));
    }

    [Test]
    public void Evaluate_LongComplexPassword_IsStrongOrBetter()
    {
        var result = _service.Evaluate("7h!sIs@V3ryStr0ng&UniquePhrase92");
        Assert.That(result.Score, Is.GreaterThanOrEqualTo(3));
        Assert.That(result.Level, Is.GreaterThanOrEqualTo(PasswordStrengthLevel.Strong));
    }

    [Test]
    public void Evaluate_MissingCharacterClasses_SuggestsAddingThem()
    {
        var result = _service.Evaluate("alllowercaseletters");
        Assert.That(result.Suggestions, Has.Some.Contains("uppercase").IgnoreCase);
        Assert.That(result.Suggestions, Has.Some.Contains("number").IgnoreCase);
        Assert.That(result.Suggestions, Has.Some.Contains("symbol").IgnoreCase);
    }

    [Test]
    public void Evaluate_AlwaysReturnsColourAndCrackTime()
    {
        var result = _service.Evaluate("SomePassword123!");
        Assert.That(result.ColorHex, Does.StartWith("#"));
        Assert.That(result.CrackTimeDisplay, Is.Not.Empty);
        Assert.That(result.EntropyBits, Is.GreaterThan(0));
    }

    [Test]
    public void Evaluate_StrongerPassword_HasHigherOrEqualScore()
    {
        var weak = _service.Evaluate("aaaa1111");
        var strong = _service.Evaluate("G7#kLm92!qXz&5Rt");
        Assert.That(strong.Score, Is.GreaterThan(weak.Score));
    }
}
