using System.IO.Compression;
using System.Text;
using Allure.NUnit;
using Allure.NUnit.Attributes;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Microsoft.Extensions.DependencyInjection;
using VaultGuard.Crypto.Extensions;
using VaultGuard.Crypto.Interfaces;
using VaultGuard.DAL;
using VaultGuard.Imports.Interfaces;
using VaultGuard.Imports.Services;
using VaultGuard.Models;
using VaultGuard.Services;
using VaultGuard.Services.Interfaces;
using VaultGuard.Services.Security;
using VaultGuard.Services.Services;
using VaultGuard.Services.Utilities;
using VaultGuardImports.OnePassword.Providers;

namespace VaultGuard.BackEnd.Tests.Services;

/// <summary>
/// Imports realistic 1Password exports (CSV in both header layouts, and the extended 1PUX archive) through the
/// real ImportService into a real SQLite vault with an unlocked session, then reads every item back from a fresh
/// context, decrypts it and checks each value survived — and that no secret is left readable in the database file.
/// </summary>
[TestFixture]
[AllureNUnit]
[AllureEpic("Vault Data Management")]
[AllureFeature("Import")]
[AllureParentSuite("Vault Data Management")]
[AllureSuite("1Password import (end to end)")]
public class OnePasswordImportEndToEndTests
{
    private const string UserId = "user-1";
    private string _dbFile = null!;
    private DbContextOptions<VaultGuardDbContext> _options = null!;
    private VaultSessionService _sessions = null!;
    private string _sid = null!;

    [SetUp]
    public void SetUp()
    {
        _dbFile = Path.Combine(Path.GetTempPath(), $"op-import-{Guid.NewGuid():N}.db");
        _options = new DbContextOptionsBuilder<VaultGuardDbContext>().UseSqlite($"Data Source={_dbFile};Pooling=False").Options;
        using (var db = new VaultGuardDbContext(_options))
        {
            db.Database.EnsureCreated();
            db.Users.Add(new ApplicationUser { Id = UserId, UserName = "me", Email = "me@example.com" });
            db.SaveChanges();
        }
        var crypto = new ServiceCollection().AddLogging().AddCryptographyServices().BuildServiceProvider().GetRequiredService<IPasswordCryptoService>();
        _sessions = new VaultSessionService(crypto, NullLogger<VaultSessionService>.Instance);
        _sid = _sessions.InitializeSession(UserId, crypto.DeriveMasterKey("Master-Pw-1!", crypto.GenerateUserSalt()));
    }

    [TearDown]
    public void TearDown()
    {
        SqliteConnection.ClearAllPools();
        try { File.Delete(_dbFile); } catch { /* temp file */ }
    }

    private async Task<ImportResult> ImportAsync(byte[] file, string fileName)
    {
        using var db = new VaultGuardDbContext(_options);
        var service = new ImportService(new PasswordItemService(db), new CollectionService(db),
            new CategoryService(db, Mock.Of<IAuthService>()), new TagService(db), new PluginDiscoveryService(), _sessions);
        service.RegisterProvider(new OnePasswordImportProvider());
        return await service.ImportPasswordsAsync("1Password", new MemoryStream(file), fileName, UserId, null, _sid);
    }

    /// <summary>Every imported item, re-read from a fresh context and decrypted with the vault key.</summary>
    private async Task<Dictionary<string, PasswordItem>> ReadBackAsync()
    {
        using var db = new VaultGuardDbContext(_options);
        var service = new PasswordItemService(db);
        var cipher = VaultCipher.ForSession(_sessions, _sid)!;
        var items = new Dictionary<string, PasswordItem>();
        foreach (var summary in await service.GetAllAsync())
        {
            var item = (await service.GetByIdAsync(summary.Id))!;
            cipher.Open(item);
            items[item.Title] = item;
        }
        return items;
    }

    private void AssertNotInDatabaseFile(params string[] secrets)
    {
        SqliteConnection.ClearAllPools();
        var raw = File.ReadAllBytes(_dbFile);
        foreach (var s in secrets)
        {
            Assert.That(IndexOf(raw, Encoding.UTF8.GetBytes(s)), Is.EqualTo(-1), $"'{s}' is stored readable (UTF-8) in the database file");
            Assert.That(IndexOf(raw, Encoding.Unicode.GetBytes(s)), Is.EqualTo(-1), $"'{s}' is stored readable (UTF-16) in the database file");
        }
    }

    private static int IndexOf(byte[] hay, byte[] needle)
    {
        for (var i = 0; i <= hay.Length - needle.Length; i++)
        {
            var j = 0;
            while (j < needle.Length && hay[i + j] == needle[j]) j++;
            if (j == needle.Length) return i;
        }
        return -1;
    }

    private static string Custom(PasswordItem i, string name) =>
        i.CustomFields.FirstOrDefault(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase))?.Value ?? "<missing>";

    // ---------------------------------------------------------------- CSV

    [Test]
    [AllureName("1Password CSV (Title,Url,Username,Password,OTPAuth,Favorite,Archived,Tags,Notes) stores every value")]
    public async Task Csv_ClassicLayout_StoresEveryValueEncrypted()
    {
        // BOM, CRLF line endings, quoted commas, escaped quotes, multi-line notes, leading/trailing spaces in a password
        var csv = "﻿\"Title\",\"Url\",\"Username\",\"Password\",\"OTPAuth\",\"Favorite\",\"Archived\",\"Tags\",\"Notes\"\r\n" +
                  "\"GitHub, Inc.\",\"https://github.com/login\",\"octo@example.com\",\"Gh\"\"Quote\"\"Pw-1\",\"otpauth://totp/GitHub:octo?secret=JBSWY3DPEHPK3PXP&issuer=GitHub\",\"true\",\"false\",\"dev;work\",\"Recovery codes:\r\nabc-123\r\ndef-456\"\r\n" +
                  "\"Spacey\",\"spacey.example\",\"sp\",\"  space-pw-2  \",\"\",\"false\",\"true\",\"\",\"\"\r\n" +
                  "\"Ünïcødé Bank\",\"https://bank.example.co.uk\",\"jürgen\",\"pässwörd-€-3\",\"\",\"\",\"\",\"\",\"Notes with ünïcødé SECRETNOTE-CSV\"\r\n";

        var result = await ImportAsync(Encoding.UTF8.GetBytes(csv), "1PasswordExport.csv");

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assert.That(result.Warnings, Is.Empty, string.Join("\n", result.Warnings));
        Assert.That(result.SuccessfulImports, Is.EqualTo(3));

        var items = await ReadBackAsync();
        var gh = items["GitHub, Inc."];
        Assert.Multiple(() =>
        {
            Assert.That(gh.Type, Is.EqualTo(ItemType.Login));
            Assert.That(gh.UserId, Is.EqualTo(UserId));
            Assert.That(gh.LoginItem!.WebsiteUrl, Is.EqualTo("https://github.com/login"));
            Assert.That(gh.LoginItem.Username, Is.EqualTo("octo@example.com"));
            Assert.That(gh.LoginItem.Password, Is.EqualTo("Gh\"Quote\"Pw-1"));
            Assert.That(gh.LoginItem.Notes, Is.EqualTo("Recovery codes:\r\nabc-123\r\ndef-456").Or.EqualTo("Recovery codes:\nabc-123\ndef-456"));
            Assert.That(TotpHelper.GetSecret(gh), Does.Contain("JBSWY3DPEHPK3PXP"));
            Assert.That(new TotpService().TryParse(TotpHelper.GetSecret(gh)!, out _, out _, out _), Is.True, "TOTP must produce codes");
            Assert.That(gh.Tags.Select(t => t.Name), Is.SupersetOf(new[] { "Imported", "Favorite", "dev", "work" }));
        });

        Assert.That(items["Spacey"].LoginItem!.Password, Is.EqualTo("  space-pw-2  "), "passwords must be stored exactly, spaces included");
        Assert.That(items["Spacey"].Tags.Select(t => t.Name), Does.Contain("Archived"));
        Assert.That(items["Ünïcødé Bank"].LoginItem!.Password, Is.EqualTo("pässwörd-€-3"));
        Assert.That(items["Ünïcødé Bank"].LoginItem!.Username, Is.EqualTo("jürgen"));

        AssertNotInDatabaseFile("Gh\"Quote\"Pw-1", "space-pw-2", "pässwörd-€-3", "JBSWY3DPEHPK3PXP", "SECRETNOTE-CSV", "abc-123");
    }

    [Test]
    [AllureName("Import report lists every item: complete, missing fields, and nothing secret")]
    public async Task ImportReport_ListsEveryItem_WithMissingFields_AndNoSecrets()
    {
        var csv = "Title,URL,Username,Password,Notes,Type\n" +
                  "Complete,https://complete.example,me@example.com,Report-Pw-1,,Login\n" +
                  "Activision,https://activision.com,,,,Login\n";

        var result = await ImportAsync(Encoding.UTF8.GetBytes(csv), "export.csv");

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assert.That(result.Report, Has.Count.EqualTo(2));
        var complete = result.Report.Single(r => r.Title == "Complete");
        var gaps = result.Report.Single(r => r.Title == "Activision");
        Assert.Multiple(() =>
        {
            Assert.That(complete.Outcome, Is.EqualTo(ImportItemOutcome.Imported));
            Assert.That(complete.ItemType, Is.EqualTo("Login"));
            Assert.That(gaps.Outcome, Is.EqualTo(ImportItemOutcome.ImportedWithGaps));
            Assert.That(gaps.MissingFields, Is.EquivalentTo(new[] { "username", "password" }));
            Assert.That(gaps.Summary, Is.EqualTo("Imported. Not in the file: username, password"));
        });

        var text = ImportReport.ToText(result, "export.csv", "1Password");
        Assert.That(text, Does.Contain("Imported: 2 of 2 item(s)"));
        Assert.That(text, Does.Contain("Activision (Login): no username, password in the file"));
        Assert.That(text, Does.Not.Contain("Report-Pw-1"), "the report must never contain secrets");
    }

    [Test]
    [AllureName("1Password CSV (Title,URL,Username,Password,Notes,Type) stores every value")]
    public async Task Csv_NewLayout_StoresEveryValueEncrypted()
    {
        var csv = "Title,URL,Username,Password,Notes,Type\n" +
                  "Netflix,https://netflix.com,me@example.com,Nf-Pw-9,\"Shared with family, SECRETNOTE-NEW\",Login\n" +
                  "Gmail,https://mail.google.com,me@gmail.com,Gm-Pw-8,,Login\n";

        var result = await ImportAsync(Encoding.UTF8.GetBytes(csv), "export.csv");

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assert.That(result.SuccessfulImports, Is.EqualTo(2), string.Join("\n", result.Warnings));
        var items = await ReadBackAsync();
        Assert.That(items["Netflix"].LoginItem!.Password, Is.EqualTo("Nf-Pw-9"));
        Assert.That(items["Netflix"].LoginItem!.Notes, Is.EqualTo("Shared with family, SECRETNOTE-NEW"));
        Assert.That(items["Gmail"].LoginItem!.Username, Is.EqualTo("me@gmail.com"));
        Assert.That(items["Gmail"].LoginItem!.Password, Is.EqualTo("Gm-Pw-8"));
        AssertNotInDatabaseFile("Nf-Pw-9", "Gm-Pw-8", "SECRETNOTE-NEW");
    }

    [Test]
    [AllureName("1Password CSV with columns in a different order is read by header name")]
    public async Task Csv_ReorderedColumns_AreMappedByHeaderName()
    {
        var csv = "Notes,Password,Title,Username,OTPAuth,Url\n" +
                  "my note,Ro-Pw-7,Reordered,ro-user,,https://ro.example\n";

        var result = await ImportAsync(Encoding.UTF8.GetBytes(csv), "export.csv");

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        var item = (await ReadBackAsync())["Reordered"];
        Assert.That(item.LoginItem!.Username, Is.EqualTo("ro-user"));
        Assert.That(item.LoginItem.Password, Is.EqualTo("Ro-Pw-7"));
        Assert.That(item.LoginItem.WebsiteUrl, Is.EqualTo("https://ro.example"));
        Assert.That(item.LoginItem.Notes, Is.EqualTo("my note"));
    }

    // ---------------------------------------------------------------- 1PUX (extended export)

    private static byte[] Pux(string itemsJson)
    {
        var data = "{\"accounts\":[{\"attrs\":{\"accountName\":\"Me\",\"name\":\"Me\",\"email\":\"me@example.com\",\"uuid\":\"A1\",\"domain\":\"https://my.1password.com/\"}," +
                   "\"vaults\":[{\"attrs\":{\"uuid\":\"V1\",\"name\":\"Personal\",\"type\":\"P\"},\"items\":[" + itemsJson + "]}]}]}";
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            using (var w = new StreamWriter(zip.CreateEntry("export.attributes").Open())) w.Write("{\"version\":3,\"description\":\"1Password Unencrypted Export\",\"createdAt\":1700000000}");
            using (var w = new StreamWriter(zip.CreateEntry("export.data").Open())) w.Write(data);
        }
        return ms.ToArray();
    }

    private const string PuxItems = """
    {"uuid":"i1","favIndex":1,"createdAt":1600000000,"updatedAt":1700000000,"state":"active","categoryUuid":"001",
     "overview":{"title":"GitHub","url":"","urls":[{"label":"website","url":"https://github.com"}],"tags":["dev","work"]},
     "details":{"loginFields":[
        {"value":"octo@example.com","name":"username","fieldType":"E","designation":"username"},
        {"value":" Pux-Gh-Pw-1 ","name":"password","fieldType":"P","designation":"password"}],
      "notesPlain":"line one\nline two SECRETNOTE-PUX",
      "sections":[{"title":"","name":"add more","fields":[
        {"title":"one-time password","id":"TOTP_abc","value":{"totp":"otpauth://totp/GitHub:octo?secret=JBSWY3DPEHPK3PXP&issuer=GitHub"}},
        {"title":"Recovery code","id":"rc","value":{"concealed":"RECOVERY-777"}},
        {"title":"Backup email","id":"be","value":{"email":{"email_address":"backup@example.com","provider":null}}},
        {"title":"Member since","id":"ms","value":{"date":1577836800}}]}],
      "passwordHistory":[]}},
    {"uuid":"i2","favIndex":0,"createdAt":1600000000,"updatedAt":1700000000,"state":"active","categoryUuid":"002",
     "overview":{"title":"Visa Travel","url":"","tags":[]},
     "details":{"loginFields":[],"notesPlain":"card note","sections":[{"title":"","name":"","fields":[
        {"title":"cardholder name","id":"cardholder","value":{"string":"Alice Smith"}},
        {"title":"type","id":"type","value":{"creditCardType":"visa"}},
        {"title":"number","id":"ccnum","value":{"creditCardNumber":"4111111111111111"}},
        {"title":"verification number","id":"cvv","value":{"concealed":"987"}},
        {"title":"expiry date","id":"expiry","value":{"monthYear":202712}},
        {"title":"PIN","id":"pin","value":{"concealed":"4321"}},
        {"title":"issuing bank","id":"bank","value":{"string":"Big Bank"}}]}],"passwordHistory":[]}},
    {"uuid":"i3","favIndex":0,"createdAt":1600000000,"updatedAt":1700000000,"state":"active","categoryUuid":"003",
     "overview":{"title":"Safe combination","url":"","tags":[]},
     "details":{"loginFields":[],"notesPlain":"Combination is SECRET-NOTE-42","sections":[],"passwordHistory":[]}},
    {"uuid":"i4","favIndex":0,"createdAt":1600000000,"updatedAt":1700000000,"state":"active","categoryUuid":"005",
     "overview":{"title":"Standalone password","url":"","tags":[]},
     "details":{"loginFields":[],"notesPlain":"","password":"Standalone-Pw-5","sections":[],"passwordHistory":[]}},
    {"uuid":"i5","favIndex":0,"createdAt":1600000000,"updatedAt":1700000000,"state":"active","categoryUuid":"109",
     "overview":{"title":"Home router","url":"","tags":[]},
     "details":{"loginFields":[],"notesPlain":"","sections":[{"title":"","name":"","fields":[
        {"title":"base station name","id":"name","value":{"string":"HomeBase"}},
        {"title":"base station password","id":"password","value":{"concealed":"Router-Admin-Pw-6"}},
        {"title":"server / IP address","id":"server","value":{"string":"192.168.1.1"}},
        {"title":"network name","id":"network_name","value":{"string":"HomeNet"}},
        {"title":"wireless security","id":"wireless_security","value":{"menu":"wpa2p"}},
        {"title":"wireless network password","id":"wireless_password","value":{"concealed":"Wifi-Pw-7"}}]}],"passwordHistory":[]}},
    {"uuid":"i6","favIndex":0,"createdAt":1600000000,"updatedAt":1700000000,"state":"active","categoryUuid":"004",
     "overview":{"title":"Me (identity)","url":"","tags":[]},
     "details":{"loginFields":[],"notesPlain":"","sections":[{"title":"Identification","name":"name","fields":[
        {"title":"first name","id":"firstname","value":{"string":"Alice"}},
        {"title":"last name","id":"lastname","value":{"string":"Smith"}},
        {"title":"email","id":"email","value":{"email":{"email_address":"alice@example.com"}}}]}],"passwordHistory":[]}},
    {"uuid":"i7","favIndex":0,"createdAt":1600000000,"updatedAt":1700000000,"state":"archived","categoryUuid":"001",
     "overview":{"title":"Old forum","url":"https://forum.example","tags":[]},
     "details":{"loginFields":[{"value":"olduser","designation":"username"},{"value":"Old-Pw-8","designation":"password"}],"notesPlain":"","sections":[],"passwordHistory":[]}},
    {"uuid":"i8","favIndex":0,"createdAt":1600000000,"updatedAt":1700000000,"state":"active","categoryUuid":"001",
     "overview":{"title":null,"url":null,"tags":null},
     "details":{"loginFields":[{"value":"nobody","designation":"username"},{"value":"Untitled-Pw-9","designation":"password"}],"notesPlain":null,"sections":[],"passwordHistory":[]}}
    """;

    [Test]
    [AllureName("1Password 1PUX export stores logins, TOTP, cards, notes, passwords, Wi-Fi and identities")]
    public async Task Pux_EveryItemType_StoresEveryValueEncrypted()
    {
        var result = await ImportAsync(Pux(PuxItems), "export.1pux");

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assert.That(result.Warnings, Is.Empty, string.Join("\n", result.Warnings));
        Assert.That(result.SuccessfulImports, Is.EqualTo(8));

        var items = await ReadBackAsync();

        var gh = items["GitHub"];
        Assert.Multiple(() =>
        {
            Assert.That(gh.Type, Is.EqualTo(ItemType.Login));
            Assert.That(gh.LoginItem!.WebsiteUrl, Is.EqualTo("https://github.com"));
            Assert.That(gh.LoginItem.Username, Is.EqualTo("octo@example.com"));
            Assert.That(gh.LoginItem.Password, Is.EqualTo(" Pux-Gh-Pw-1 "), "passwords must be stored exactly");
            Assert.That(gh.LoginItem.Notes, Is.EqualTo("line one\nline two SECRETNOTE-PUX"));
            Assert.That(TotpHelper.GetSecret(gh), Does.Contain("JBSWY3DPEHPK3PXP"));
            Assert.That(new TotpService().TryParse(TotpHelper.GetSecret(gh)!, out _, out _, out _), Is.True);
            Assert.That(Custom(gh, "Recovery code"), Is.EqualTo("RECOVERY-777"));
            Assert.That(gh.CustomFields.Single(f => f.Name == "Recovery code").IsSecret, Is.True);
            Assert.That(Custom(gh, "Backup email"), Is.EqualTo("backup@example.com"));
            Assert.That(Custom(gh, "Member since"), Is.EqualTo("2020-01-01"));
            Assert.That(gh.CustomFields.Count(f => f.Value.Contains("JBSWY3DPEHPK3PXP")), Is.LessThanOrEqualTo(1), "TOTP must not be duplicated");
            Assert.That(gh.Tags.Select(t => t.Name), Is.SupersetOf(new[] { "Imported", "Favorite", "dev", "work" }));
            Assert.That(gh.CreatedAt, Is.EqualTo(DateTimeOffset.FromUnixTimeSeconds(1600000000).UtcDateTime).Within(TimeSpan.FromDays(1)).Or.GreaterThan(DateTime.UtcNow.AddMinutes(-5)));
        });

        var card = items["Visa Travel"];
        Assert.Multiple(() =>
        {
            Assert.That(card.Type, Is.EqualTo(ItemType.CreditCard));
            Assert.That(card.CreditCardItem!.CardholderName, Is.EqualTo("Alice Smith"));
            Assert.That(card.CreditCardItem.CardNumber, Is.EqualTo("4111111111111111"));
            Assert.That(card.CreditCardItem.CVV, Is.EqualTo("987"));
            Assert.That(card.CreditCardItem.PIN, Is.EqualTo("4321"));
            Assert.That(card.CreditCardItem.ExpiryDate, Is.EqualTo("12/2027"));
            Assert.That(card.CreditCardItem.CardType, Is.EqualTo(CardType.Visa));
            Assert.That(card.CreditCardItem.IssuingBank, Is.EqualTo("Big Bank"));
            Assert.That(card.CreditCardItem.Notes, Is.EqualTo("card note"));
        });

        Assert.That(items["Safe combination"].Type, Is.EqualTo(ItemType.SecureNote));
        Assert.That(items["Safe combination"].SecureNoteItem!.Content, Is.EqualTo("Combination is SECRET-NOTE-42"));

        Assert.That(items["Standalone password"].Type, Is.EqualTo(ItemType.Password));
        Assert.That(items["Standalone password"].LoginItem!.Password, Is.EqualTo("Standalone-Pw-5"), "1Password 'Password' items keep the password in details.password");

        var wifi = items["Home router"];
        Assert.Multiple(() =>
        {
            Assert.That(wifi.Type, Is.EqualTo(ItemType.WiFi));
            Assert.That(wifi.WiFiItem, Is.Not.Null, "Wi-Fi items need a Wi-Fi body for the apps to show them");
            Assert.That(wifi.WiFiItem?.NetworkName, Is.EqualTo("HomeNet"));
            Assert.That(wifi.WiFiItem?.Password, Is.EqualTo("Wifi-Pw-7"));
            Assert.That(wifi.WiFiItem?.RouterPassword, Is.EqualTo("Router-Admin-Pw-6"));
            Assert.That(wifi.WiFiItem?.RouterIP, Is.EqualTo("192.168.1.1"));
        });

        var id = items["Me (identity)"];
        Assert.That(Custom(id, "first name"), Is.EqualTo("Alice"));
        Assert.That(Custom(id, "last name"), Is.EqualTo("Smith"));
        Assert.That(Custom(id, "email"), Is.EqualTo("alice@example.com"));

        Assert.That(items["Old forum"].Tags.Select(t => t.Name), Does.Contain("Archived"));
        Assert.That(items["Old forum"].LoginItem!.Password, Is.EqualTo("Old-Pw-8"));

        Assert.That(items.Values.Any(i => i.LoginItem?.Password == "Untitled-Pw-9"), Is.True, "an item with no title must still be imported");

        AssertNotInDatabaseFile("Pux-Gh-Pw-1", "SECRETNOTE-PUX", "JBSWY3DPEHPK3PXP", "RECOVERY-777", "4111111111111111",
            "SECRET-NOTE-42", "Standalone-Pw-5", "Wifi-Pw-7", "Router-Admin-Pw-6", "Old-Pw-8", "Untitled-Pw-9");
    }

    [Test]
    [AllureName("Reveal buttons: the reveal service decrypts every imported secret the apps show")]
    public async Task Reveal_EveryImportedSecret_DecryptsThroughTheRevealService()
    {
        Assert.That((await ImportAsync(Pux(PuxItems), "export.1pux")).SuccessfulImports, Is.EqualTo(8));
        Assert.That((await ImportAsync(Encoding.UTF8.GetBytes("Title,Url,Username,Password\nCsv Login,https://c.example,cu,\" Csv-Pw \"\n"), "e.csv")).SuccessfulImports, Is.EqualTo(1));

        // Exactly what the WPF details page / MAUI vault page do: load the item (still encrypted), then either
        // reveal one field on demand (login password, TOTP) or open the whole item (cards, Wi-Fi, notes, custom fields).
        var reveal = new PasswordRevealService(_sessions, NullLogger<PasswordRevealService>.Instance);
        using var db = new VaultGuardDbContext(_options);
        var svc = new PasswordItemService(db);
        async Task<PasswordItem> Load(string title) => (await svc.GetByIdAsync((await svc.GetAllAsync()).Single(i => i.Title == title).Id))!;

        var gh = await Load("GitHub");
        Assert.That(gh.LoginItem!.Password, Is.Null.Or.Empty, "secrets must not be readable before reveal");
        Assert.That(await reveal.RevealPasswordAsync(gh.LoginItem, _sid), Is.EqualTo(" Pux-Gh-Pw-1 "));
        Assert.That(await reveal.RevealTotpSecretAsync(gh.LoginItem, _sid), Does.Contain("JBSWY3DPEHPK3PXP"));
        Assert.That(gh.CustomFields.Single(f => f.Name == "Recovery code").Value, Does.Not.Contain("RECOVERY-777"));
        Assert.That(reveal.OpenItem(gh, _sid), Is.True);
        Assert.That(gh.CustomFields.Single(f => f.Name == "Recovery code").Value, Is.EqualTo("RECOVERY-777"));

        var card = await Load("Visa Travel");
        Assert.That(await reveal.RevealCardNumberAsync(card.CreditCardItem!, _sid), Is.EqualTo("4111111111111111"));
        Assert.That(await reveal.RevealCvvAsync(card.CreditCardItem!, _sid), Is.EqualTo("987"));
        Assert.That(reveal.OpenItem(card, _sid), Is.True);
        Assert.That(card.CreditCardItem!.PIN, Is.EqualTo("4321"));

        var wifi = await Load("Home router");
        Assert.That(await reveal.RevealPasswordAsync(wifi.WiFiItem!, _sid), Is.EqualTo("Wifi-Pw-7"));
        Assert.That(reveal.OpenItem(wifi, _sid), Is.True);
        Assert.That(wifi.WiFiItem!.RouterPassword, Is.EqualTo("Router-Admin-Pw-6"));

        var note = await Load("Safe combination");
        Assert.That(await reveal.RevealContentAsync(note.SecureNoteItem!, _sid), Is.EqualTo("Combination is SECRET-NOTE-42"));

        Assert.That(await reveal.RevealPasswordAsync((await Load("Standalone password")).LoginItem!, _sid), Is.EqualTo("Standalone-Pw-5"));
        Assert.That(await reveal.RevealPasswordAsync((await Load("Csv Login")).LoginItem!, _sid), Is.EqualTo(" Csv-Pw "));

        // Locked vault: reveal gives nothing (the apps then say "unlock"), never ciphertext.
        _sessions.ClearSession(_sid);
        var locked = await Load("Old forum");
        Assert.That(await reveal.RevealPasswordAsync(locked.LoginItem!, _sid), Is.Null.Or.Empty);
    }

    [Test]
    [AllureName("A second import in the same session (one DbContext, as in a Blazor circuit) succeeds")]
    public async Task Import_TwiceInOneContext_Succeeds()
    {
        using var db = new VaultGuardDbContext(_options);
        var service = new ImportService(new PasswordItemService(db), new CollectionService(db),
            new CategoryService(db, Mock.Of<IAuthService>()), new TagService(db), new PluginDiscoveryService(), _sessions);
        service.RegisterProvider(new OnePasswordImportProvider());

        var first = await service.ImportPasswordsAsync("1Password", new MemoryStream(Pux(PuxItems)), "export.1pux", UserId, null, _sid);
        var csv = "Title,Url,Username,Password\nSecond Import,https://second.example,s,Second-Pw\nInsurance login,https://geico.com,g,Geico-Pw\n";
        var second = await service.ImportPasswordsAsync("1Password", new MemoryStream(Encoding.UTF8.GetBytes(csv)), "e.csv", UserId, null, _sid);

        Assert.That(first.SuccessfulImports, Is.EqualTo(8), first.ErrorMessage + string.Join("\n", first.Warnings));
        Assert.That(second.Success, Is.True, second.ErrorMessage);
        Assert.That(second.SuccessfulImports, Is.EqualTo(2), string.Join("\n", second.Warnings));
        var items = await ReadBackAsync();
        Assert.That(items["Second Import"].LoginItem!.Password, Is.EqualTo("Second-Pw"));
        Assert.That(items["Second Import"].CategoryId, Is.Not.Null);
    }

    [Test]
    [AllureName("Importing while the vault is locked refuses items instead of storing secrets unencrypted")]
    public async Task Pux_LockedVault_StoresNoPlaintext()
    {
        _sessions.ClearSession(_sid);
        var result = await ImportAsync(Pux(PuxItems), "export.1pux");

        // Only the identity (names and an email, nothing secret) can be stored without the vault key.
        Assert.That(result.SuccessfulImports, Is.EqualTo(1));
        Assert.That(result.FailedImports, Is.EqualTo(7));
        Assert.That(result.Warnings, Has.All.Contains("locked"));
        AssertNotInDatabaseFile("Pux-Gh-Pw-1", "4111111111111111", "Wifi-Pw-7");
    }
}
