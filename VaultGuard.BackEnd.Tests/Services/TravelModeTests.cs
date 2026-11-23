using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using VaultGuard.DAL;
using VaultGuard.Models;
using VaultGuard.Services;
using VaultGuard.Services.Interfaces;
using VaultGuard.Services.Security;

namespace VaultGuard.BackEnd.Tests.Services;

/// <summary>Travel mode: only vaults marked safe for travel are visible until it's turned off.</summary>
[TestFixture, NonParallelizable]
public class TravelModeTests
{
    private sealed class Settings : IAppSettingsService
    {
        private readonly Dictionary<string, string> _v = new();
        public string SettingsFilePath => "";
        public string Get(string key, string fallback = "") => _v.TryGetValue(key, out var x) ? x : fallback;
        public bool GetBool(string key, bool fallback = false) => bool.TryParse(Get(key), out var b) ? b : fallback;
        public int GetInt(string key, int fallback = 0) => fallback;
        public void Set(string key, string value) => _v[key] = value;
        public void Set(string key, bool value) => _v[key] = value.ToString();
        public void Set(string key, int value) => _v[key] = value.ToString();
        public bool Contains(string key) => _v.ContainsKey(key);
        public void Remove(string key) => _v.Remove(key);
        public void Reload() { }
        public bool Save() => true;
    }

    private string _file = null!;
    private DbContextOptions<VaultGuardDbContext> _options = null!;
    private int _safeVault, _otherVault;

    [SetUp]
    public void SetUp()
    {
        _file = Path.Combine(Path.GetTempPath(), $"travel-{Guid.NewGuid():N}.db");
        _options = new DbContextOptionsBuilder<VaultGuardDbContext>().UseSqlite($"Data Source={_file};Pooling=False").Options;
        using var db = new VaultGuardDbContext(_options);
        db.Database.EnsureCreated();
        db.Users.Add(new ApplicationUser { Id = "me", UserName = "me", Email = "me@x" });
        var personal = new Vault { Name = "Personal", UserId = "me", IsDefault = true };
        var work = new Vault { Name = "Work", UserId = "me" };
        db.Vaults.AddRange(personal, work);
        db.SaveChanges();
        var cp = new Collection { Name = "Personal", UserId = "me", VaultId = personal.Id };
        var cw = new Collection { Name = "Work", UserId = "me", VaultId = work.Id };
        var loose = new Collection { Name = "Loose", UserId = "me" };
        db.Collections.AddRange(cp, cw, loose);
        db.SaveChanges();
        db.PasswordItems.AddRange(
            new PasswordItem { Title = "Bank", Type = ItemType.SecureNote, UserId = "me", CollectionId = cp.Id },
            new PasswordItem { Title = "Work VPN", Type = ItemType.SecureNote, UserId = "me", CollectionId = cw.Id },
            new PasswordItem { Title = "Loose note", Type = ItemType.SecureNote, UserId = "me", CollectionId = loose.Id });
        db.SaveChanges();
        _safeVault = personal.Id; _otherVault = work.Id;
    }

    [TearDown]
    public void TearDown()
    {
        TravelMode.TurnOff(new Settings());
        SqliteConnection.ClearAllPools();
        try { File.Delete(_file); } catch { }
    }

    private async Task<List<string>> TitlesAsync()
    {
        using var db = new VaultGuardDbContext(_options);
        return (await new PasswordItemService(db).GetAllAsync()).Select(i => i.Title).OrderBy(t => t).ToList();
    }

    [Test]
    public async Task OnlySafeVaultsAreVisible_UntilTurnedOff()
    {
        Assert.That(await TitlesAsync(), Is.EqualTo(new[] { "Bank", "Loose note", "Work VPN" }));
        var settings = new Settings();
        TravelMode.TurnOn(settings, new[] { _safeVault });
        Assert.That(await TitlesAsync(), Is.EqualTo(new[] { "Bank" }), "other vaults and items outside any vault are hidden");

        // Survives a restart: loaded from settings.
        TravelMode.TurnOff(new Settings());
        TravelMode.Load(settings);
        Assert.That(TravelMode.IsOn, Is.True);
        Assert.That(await TitlesAsync(), Is.EqualTo(new[] { "Bank" }));

        TravelMode.TurnOff(settings);
        Assert.That(await TitlesAsync(), Has.Count.EqualTo(3));
    }

    [Test]
    public async Task HiddenItems_CantBeOpenedById()
    {
        using var db = new VaultGuardDbContext(_options);
        var workId = db.PasswordItems.Single(p => p.Title == "Work VPN").Id;
        TravelMode.TurnOn(new Settings(), new[] { _safeVault });
        Assert.That(await new PasswordItemService(db).GetByIdAsync(workId), Is.Null);
        Assert.That(TravelMode.Apply(db.PasswordItems, db).Count(), Is.EqualTo(1), "the browser link uses the same filter");
    }
}
