using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace VaultGuard.DAL.Security;

/// <summary>
/// Secrets (card number, CVV, PIN, Wi-Fi passwords, note content…) used to be stored in plaintext
/// columns. Their CLR properties are now [NotMapped] and the values live in Encrypted* columns.
///
/// The old columns are kept in the schema (dropping them would need table rebuilds on SQLite and would
/// destroy data before it is migrated) and mapped to shadow properties named "Legacy{Name}", so that
/// <c>VaultSecretsMigrator</c> can read old plaintext once, encrypt it, and wipe it. Application code never
/// writes a secret into them: on insert, required ones get "" (some existing databases declare them
/// NOT NULL), optional ones stay NULL.
/// </summary>
public static class LegacySecretColumns
{
    public static string ShadowName(string column) => "Legacy" + column;

    public static void Map<T>(EntityTypeBuilder<T> entity, string column, int maxLength, bool required) where T : class
    {
        var p = entity.Property<string>(ShadowName(column)).HasColumnName(column).HasMaxLength(maxLength);
        if (required)
            p.IsRequired().HasValueGenerator<EmptyStringGenerator>();
    }

    /// <summary>Gives new rows "" for legacy NOT NULL columns — never real data.</summary>
    public sealed class EmptyStringGenerator : ValueGenerator<string>
    {
        public override bool GeneratesTemporaryValues => false;
        public override string Next(EntityEntry entry) => string.Empty;
    }

    /// <summary>
    /// For code that has to write secrets without a vault key (the demo-data seeder runs at install time,
    /// before anyone has unlocked): park them in the retired plaintext columns so that
    /// <c>VaultSecretsMigrator</c> encrypts them — and wipes the plaintext — the first time the owner unlocks.
    /// </summary>
    public static void StashUntilUnlock(DbContext db, VaultGuard.Models.PasswordItem item)
    {
        void Put(object? entity, string column, string? value)
        {
            if (entity == null || string.IsNullOrEmpty(value)) return;
            db.Entry(entity).Property(ShadowName(column)).CurrentValue = value;
        }
        if (item.CreditCardItem is { } cc)
        {
            Put(cc, "CardNumber", cc.CardNumber); Put(cc, "CVV", cc.CVV); Put(cc, "PIN", cc.PIN);
            Put(cc, "OnlineBankingPassword", cc.OnlineBankingPassword);
        }
        if (item.WiFiItem is { } w)
        {
            Put(w, "Password", w.Password); Put(w, "RouterPassword", w.RouterPassword);
            Put(w, "GuestNetworkPassword", w.GuestNetworkPassword);
        }
        if (item.SecureNoteItem is { } n) Put(n, "Content", n.Content);
    }
}
