using Microsoft.EntityFrameworkCore;
using VaultGuard.DAL;
using VaultGuard.Models;

namespace VaultGuard.Services.Security;

/// <summary>
/// Re-encrypts items that were sealed with an older key for the same user, so they open with the current one.
/// The desktop app used to derive the vault key from a salt cached in Windows secure storage instead of the
/// account's salt in the database; whenever the two differed (a reset or re-seeded database, another database
/// file), the password still verified but items were sealed with a key nothing else could reproduce, and showed
/// "Couldn't decrypt this password". Items that open with neither key are left untouched.
/// </summary>
public static class VaultKeyRepair
{
    public sealed record Result(int Repaired, int StillUnreadable);

    public static async Task<Result> ReKeyAsync(VaultGuardDbContext db, string userId, VaultCipher current, VaultCipher previous,
        CancellationToken ct = default)
    {
        var items = await db.PasswordItems
            .Where(p => p.UserId == userId)
            .Include(p => p.LoginItem).Include(p => p.CreditCardItem).Include(p => p.WiFiItem)
            .Include(p => p.SecureNoteItem).Include(p => p.PasskeyItem).Include(p => p.CustomFields)
            .ToListAsync(ct);

        int repaired = 0, unreadable = 0;
        foreach (var item in items)
        {
            VaultItemSecrets.Forget(item);
            var failedNow = current.Open(item);
            VaultItemSecrets.Forget(item);
            if (failedNow.Count == 0) continue;            // already fine

            var failedBefore = previous.Open(item);
            if (failedBefore.Count > 0)                     // not the old key either: leave it alone
            {
                VaultItemSecrets.Forget(item);
                unreadable++;
                continue;
            }

            // Every secret is now in memory as plaintext: seal it again under the current key. Encrypted custom
            // fields keep their ciphertext unless restaged, so put the decrypted value back as the pending value.
            foreach (var cf in item.CustomFields ?? new List<CustomField>())
                if (cf.IsEncrypted && !string.IsNullOrEmpty(cf.Value)) cf.Value = cf.Value;
            current.Seal(item);
            VaultItemSecrets.Forget(item);
            item.LastModified = DateTime.UtcNow;
            repaired++;
        }

        if (repaired > 0) await db.SaveChangesAsync(ct);
        return new Result(repaired, unreadable);
    }
}
