using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using PasswordManager.Models;

namespace PasswordManager.DAL.Interfaces;

public interface IPasswordManagerDbContext : IDisposable
{
    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    DbSet<PasswordItem> PasswordItems { get; set; }
    DbSet<Category> Categories { get; set; }
    DbSet<Collection> Collections { get; set; }
    DbSet<Tag> Tags { get; set; }
    DbSet<ApplicationUser> Users { get; set; }
    DbSet<LoginItem> LoginItems { get; set; }
    DbSet<CreditCardItem> CreditCardItems { get; set; }
    DbSet<SecureNoteItem> SecureNoteItems { get; set; }
    DbSet<WiFiItem> WiFiItems { get; set; }
    DbSet<ApiKey> ApiKeys { get; set; }
    DbSet<QrLoginToken> QrLoginTokens { get; set; }

    DbSet<OtpCode> OtpCodes { get; set; }

    DbSet<UserPasskey> UserPasskeys { get; set; }
    DbSet<UserTwoFactorBackupCode> UserTwoFactorBackupCodes { get; set; }

}
