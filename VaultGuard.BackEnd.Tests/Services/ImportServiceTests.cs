using Moq;
using NUnit.Framework;
using VaultGuard.Imports.Interfaces;
using VaultGuard.Imports.Services;
using VaultGuard.Models;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.BackEnd.Tests.Services;

[TestFixture]
public class ImportServiceTests
{
    private Mock<IPasswordItemService> _itemMock = null!;
    private Mock<ICollectionService> _collectionMock = null!;
    private Mock<ICategoryInterface> _categoryMock = null!;
    private Mock<ITagService> _tagMock = null!;
    private ImportService _service = null!;

    [SetUp]
    public void Setup()
    {
        _itemMock = new Mock<IPasswordItemService>();
        _collectionMock = new Mock<ICollectionService>();
        _categoryMock = new Mock<ICategoryInterface>();
        _tagMock = new Mock<ITagService>();

        _service = new ImportService(
            _itemMock.Object,
            _collectionMock.Object,
            _categoryMock.Object,
            _tagMock.Object,
            new PluginDiscoveryService());
    }

    [Test]
    public async Task ImportPasswordsAsync_UnknownProvider_FailsGracefully()
    {
        var result = await _service.ImportPasswordsAsync("does-not-exist", Stream.Null, "x.csv");

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("not found"));
    }

    [Test]
    public async Task RegisterProvider_ThenImport_DispatchesToProviderAndPersistsItems()
    {
        var provider = new FakeProvider
        {
            Result = new ImportResult
            {
                Success = true,
                ImportedItems =
                {
                    new PasswordItem { Title = "GitHub" },
                    new PasswordItem { Title = "Gmail" }
                }
            }
        };

        _itemMock
            .Setup(s => s.CreateAsync(It.IsAny<PasswordItem>()))
            .ReturnsAsync((PasswordItem p) => p);

        _service.RegisterProvider(provider);

        var result = await _service.ImportPasswordsAsync("fake", Stream.Null, "export.csv", "user-1");

        Assert.That(provider.WasCalled, Is.True, "The provider's parser should be invoked.");
        Assert.That(result.SuccessfulImports, Is.EqualTo(2));
        _itemMock.Verify(s => s.CreateAsync(It.IsAny<PasswordItem>()), Times.Exactly(2));
    }

    [Test]
    public async Task ImportPasswordsAsync_AssignsUserIdToImportedItems()
    {
        PasswordItem? captured = null;
        _itemMock
            .Setup(s => s.CreateAsync(It.IsAny<PasswordItem>()))
            .Callback<PasswordItem>(p => captured = p)
            .ReturnsAsync((PasswordItem p) => p);

        var provider = new FakeProvider
        {
            Result = new ImportResult
            {
                Success = true,
                ImportedItems = { new PasswordItem { Title = "Item" } }
            }
        };
        _service.RegisterProvider(provider);

        await _service.ImportPasswordsAsync("fake", Stream.Null, "export.csv", "tenant-42");

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.UserId, Is.EqualTo("tenant-42"));
    }

    [Test]
    public async Task ImportPasswordsAsync_WhenProviderReportsFailure_DoesNotPersist()
    {
        var provider = new FakeProvider
        {
            Result = new ImportResult { Success = false, ErrorMessage = "bad file" }
        };
        _service.RegisterProvider(provider);

        var result = await _service.ImportPasswordsAsync("fake", Stream.Null, "bad.csv");

        Assert.That(result.Success, Is.False);
        _itemMock.Verify(s => s.CreateAsync(It.IsAny<PasswordItem>()), Times.Never);
    }

    private sealed class FakeProvider : IPasswordImportProvider
    {
        public string ProviderName => "fake";
        public string DisplayName => "Fake Provider";
        public string Version => "1.0";
        public string[] SupportedFileExtensions => new[] { ".csv" };
        public ImportResult Result { get; set; } = new();
        public bool WasCalled { get; private set; }

        public Task<ImportResult> ImportFromFileAsync(Stream fileStream, string fileName)
        {
            WasCalled = true;
            return Task.FromResult(Result);
        }
    }
}
