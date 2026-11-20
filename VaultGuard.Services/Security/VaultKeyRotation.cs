using Microsoft.EntityFrameworkCore;
using VaultGuard.DAL;
using VaultGuard.Models;

namespace VaultGuard.Services.Security;

/// <summary>
/// Re-encrypts all of a user's vault secrets from the old vault key to a new one when the master password
/// (and with it the salt and derived key) changes. Previously the key was rotated without this step, which
/// left every stored secret encrypted under a key nobody could derive any more — i.e. permanent data loss.
///
/// It only stages the changes on <paramref name="db"/>; the caller saves them in the same SaveChanges /
/// transaction as the new salt and hash, so either both happen or neither does.
/// </summary>
public static class VaultKeyRotation
{
    public sealed record Result(int ItemsReEncrypted, IReadOnlyList<string> Unreadable)
    {
        public bool Ok => Unreadable.Count == 0;
        public string Describe() => Ok
            ? $"{ItemsReEncrypted} item(s) re-encrypted."
            : $"{Unreadable.Count} item(s) can't be decrypted with the current master password " +
              $"({string.Join(", ", Unreadable.Take(5))}{(Unreadable.Count > 5 ? ", …" : "")}), so the password was not changed — " +
              "changing it would make them permanently unreadable.";
    }

    /// <summary>
    /// Stages re-encryption of every item owned by <paramref name="userId"/>. If any secret can't be
    /// decrypted with <paramref name="oldKey"/>, nothing is staged and <see cref="Result.Ok"/> is false.
    /// </summary>
    public static async Task<Result> StageAsync(VaultGuardDbContext db, string userId, VaultCipher oldKey, VaultCipher newKey,
        CancellationToken ct = default)
    {
        // Bring legacy plaintext/old layouts up to date first (encrypted with the old key, then rotated below).
        await VaultSecretsMigrator.MigrateAsync(db, userId, oldKey, ct);

        var items = await db.PasswordItems
            .Where(p => p.UserId == userId)
            .Include(p => p.LoginItem).Include(p => p.CreditCardItem).Include(p => p.WiFiItem)
            .Include(p => p.SecureNoteItem).Include(p => p.PasskeyItem)
            .ToListAsync(ct);

        var unreadable = new List<string>();
        foreach (var item in items)
        {
            VaultItemSecrets.Forget(item); // decrypt from what's stored, not from anything in memory
            var failed = oldKey.Open(item);
            if (failed.Count > 0) unreadable.Add($"\"{item.Title}\" ({string.Join("/", failed)})");
        }

        if (unreadable.Count > 0)
        {
            foreach (var item in items) VaultItemSecrets.Forget(item);
            return new Result(0, unreadable);
        }

        int count = 0;
        foreach (var item in items)
        {
            if (newKey.Seal(item) > 0) count++;
            VaultItemSecrets.Forget(item);
        }
        return new Result(count, Array.Empty<string>());
    }
}
