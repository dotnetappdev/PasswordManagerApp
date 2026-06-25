using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using VaultGuard.DAL;
using VaultGuard.Models;
using VaultGuard.Services;
using VaultGuard.Services.Utilities;

namespace VaultGuard.BackEnd.Tests.Services;

[TestFixture]
public class PasswordItemServiceTests
{
    private DbContextOptions<VaultGuardDbContext> _options = null!;
    private VaultGuardDbContext _context = null!;
    private PasswordItemService _service = null!;
    private const string TestUserId = "test-user-id";

    [SetUp]
    public void Setup()
    {
        _options = new DbContextOptionsBuilder<VaultGuardDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new VaultGuardDbContext(_options);
        _service = new PasswordItemService(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Test]
    public async Task GetByIdAsync_ShouldIncludeCustomFields()
    {
        var item = new PasswordItem
        {
            Title = "GitHub",
            Type = ItemType.Login,
            UserId = TestUserId,
            LoginItem = new LoginItem
            {
                UserId = TestUserId,
                Username = "octocat",
                Password = "password",
                WebsiteUrl = "https://github.com"
            },
            CustomFields =
            [
                new CustomField
                {
                    Name = BrandIconHelper.BrandIconCustomFieldName,
                    Value = "data:image/png;base64,AA==",
                    Type = CustomFieldType.File
                }
            ]
        };

        _context.PasswordItems.Add(item);
        await _context.SaveChangesAsync();

        var result = await _service.GetByIdAsync(item.Id);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.CustomFields, Has.Count.EqualTo(1));
        Assert.That(result.CustomFields[0].Name, Is.EqualTo(BrandIconHelper.BrandIconCustomFieldName));
    }

    [Test]
    public void WebsiteUrlSetter_ShouldCreateLoginItemAndKeepWebsiteSynchronized()
    {
        var item = new PasswordItem
        {
            Title = "Contoso",
            Type = ItemType.Login,
            UserId = TestUserId
        };

        item.WebsiteUrl = "https://contoso.com";

        Assert.That(item.LoginItem, Is.Not.Null);
        Assert.That(item.LoginItem!.WebsiteUrl, Is.EqualTo("https://contoso.com"));
        Assert.That(item.LoginItem.Website, Is.EqualTo("https://contoso.com"));
        Assert.That(item.Website, Is.EqualTo("https://contoso.com"));
    }
}
