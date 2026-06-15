using NUnit.Framework;
using PasswordManager.Services.Services;

namespace PasswordManager.BackEnd.Tests.Services;

[TestFixture]
public class QrCodeServiceTests
{
    private QrCodeService _service = null!;

    [SetUp]
    public void Setup() => _service = new QrCodeService();

    [Test]
    public void GeneratePng_ReturnsValidPngSignature()
    {
        var bytes = _service.GeneratePng("otpauth://totp/Vault:alice?secret=GEZDGNBVGY3TQOJQ&issuer=Vault");

        Assert.That(bytes, Is.Not.Empty);
        // PNG magic number: 89 50 4E 47 0D 0A 1A 0A
        Assert.That(bytes[0], Is.EqualTo(0x89));
        Assert.That(bytes[1], Is.EqualTo(0x50));
        Assert.That(bytes[2], Is.EqualTo(0x4E));
        Assert.That(bytes[3], Is.EqualTo(0x47));
    }

    [Test]
    public void GeneratePngDataUrl_HasDataUrlPrefix()
    {
        var url = _service.GeneratePngDataUrl("hello world");
        Assert.That(url, Does.StartWith("data:image/png;base64,"));
        Assert.That(url.Length, Is.GreaterThan("data:image/png;base64,".Length + 10));
    }

    [TestCase("")]
    [TestCase(null)]
    public void GeneratePng_EmptyContent_ReturnsEmpty(string? content)
    {
        Assert.That(_service.GeneratePng(content!), Is.Empty);
        Assert.That(_service.GeneratePngDataUrl(content!), Is.Empty);
    }

    [Test]
    public void GeneratePng_LargerModuleSize_ProducesLargerImage()
    {
        var small = _service.GeneratePng("same content", 4);
        var large = _service.GeneratePng("same content", 12);
        Assert.That(large.Length, Is.GreaterThan(small.Length));
    }
}
