using Allure.NUnit;
using Allure.NUnit.Attributes;
using NUnit.Framework;
using VaultGuard.Services.Services;

namespace VaultGuard.BackEnd.Tests.Services;

[TestFixture]
[AllureNUnit]
[AllureEpic("Cryptography & Security")]
[AllureFeature("Two-Factor Authentication")]
public class TotpServiceTests
{
    private TotpService _service = null!;

    // RFC 6238 test secret "12345678901234567890" (ASCII) in Base32.
    private const string Rfc6238Secret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";

    [SetUp]
    public void Setup() => _service = new TotpService();

    [Test]
    public void GenerateCode_Rfc6238Vector_T59_Produces6DigitCode()
    {
        // RFC 6238 Appendix B: T=59 → 8-digit code 94287082 → last 6 digits = 287082.
        var time = DateTimeOffset.FromUnixTimeSeconds(59);
        var code = _service.GenerateCode(Rfc6238Secret, time);

        Assert.That(code, Is.EqualTo("287082"));
    }

    [Test]
    public void GenerateCode_Rfc6238Vector_LaterTime_Produces6DigitCode()
    {
        // RFC 6238: T=1111111109 → 8-digit 07081804 → last 6 = 081804.
        var time = DateTimeOffset.FromUnixTimeSeconds(1111111109);
        var code = _service.GenerateCode(Rfc6238Secret, time);

        Assert.That(code, Is.EqualTo("081804"));
    }

    [Test]
    public void GenerateCode_OtpAuthUri_WithDigits8_ProducesFullCode()
    {
        var uri = $"otpauth://totp/Example:alice@example.com?secret={Rfc6238Secret}&issuer=Example&digits=8&period=30";
        var time = DateTimeOffset.FromUnixTimeSeconds(59);

        var code = _service.GenerateCode(uri, time);

        Assert.That(code, Is.EqualTo("94287082"));
    }

    [Test]
    public void GenerateCode_SecretWithSpaces_IsNormalised()
    {
        var spaced = "GEZD GNBV GY3T QOJQ GEZD GNBV GY3T QOJQ";
        var time = DateTimeOffset.FromUnixTimeSeconds(59);

        var code = _service.GenerateCode(spaced, time);

        Assert.That(code, Is.EqualTo("287082"));
    }

    [Test]
    public void GenerateCode_IsSixDigitsByDefault()
    {
        var code = _service.GenerateCode(Rfc6238Secret);
        Assert.That(code, Is.Not.Null);
        Assert.That(code!.Length, Is.EqualTo(6));
        Assert.That(code, Does.Match("^[0-9]{6}$"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("!!!!----")]            // strips to empty
    [TestCase("11110000 99008811")]   // digits 0/1/8/9 are not in the Base32 alphabet
    public void GenerateCode_InvalidSecret_ReturnsNull(string? secret)
    {
        Assert.That(_service.GenerateCode(secret), Is.Null);
    }

    [Test]
    public void TryParse_RawBase32_Succeeds()
    {
        var ok = _service.TryParse(Rfc6238Secret, out var secret, out var digits, out var period);

        Assert.That(ok, Is.True);
        Assert.That(secret, Is.EqualTo(Rfc6238Secret));
        Assert.That(digits, Is.EqualTo(6));
        Assert.That(period, Is.EqualTo(30));
    }

    [Test]
    public void TryParse_OtpAuthUri_ExtractsParameters()
    {
        var uri = $"otpauth://totp/Acme:bob?secret={Rfc6238Secret}&digits=8&period=60";

        var ok = _service.TryParse(uri, out var secret, out var digits, out var period);

        Assert.That(ok, Is.True);
        Assert.That(secret, Is.EqualTo(Rfc6238Secret));
        Assert.That(digits, Is.EqualTo(8));
        Assert.That(period, Is.EqualTo(60));
    }

    [Test]
    public void GetRemainingSeconds_AtWindowStart_ReturnsFullPeriod()
    {
        var atStart = DateTimeOffset.FromUnixTimeSeconds(0);
        Assert.That(_service.GetRemainingSeconds(30, atStart), Is.EqualTo(30));
    }

    [Test]
    public void GetRemainingSeconds_MidWindow_CountsDown()
    {
        var mid = DateTimeOffset.FromUnixTimeSeconds(25);
        Assert.That(_service.GetRemainingSeconds(30, mid), Is.EqualTo(5));
    }

    [Test]
    public void GenerateCode_ChangesAcrossWindows()
    {
        var w1 = _service.GenerateCode(Rfc6238Secret, DateTimeOffset.FromUnixTimeSeconds(0));
        var w2 = _service.GenerateCode(Rfc6238Secret, DateTimeOffset.FromUnixTimeSeconds(30));
        Assert.That(w1, Is.Not.EqualTo(w2));
    }
}
