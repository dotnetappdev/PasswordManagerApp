using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using PasswordManager.Models;

namespace PasswordManager.DAL.SupaBase;

public interface ISupabaseDbContext : IDisposable
{
    DbSet<PasswordItem> PasswordItems { get; set; }
    DbSet<LoginItem> LoginItems { get; set; }
    DbSet<CreditCardItem> CreditCardItems { get; set; }
    DbSet<SecureNoteItem> SecureNoteItems { get; set; }
    DbSet<WiFiItem> WiFiItems { get; set; }
    DbSet<Tag> Tags { get; set; }
    DbSet<Category> Categories { get; set; }
    DbSet<Collection> Collections { get; set; }
    DbSet<ApplicationUser> Users { get; set; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    int SaveChanges();
    EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;
    DbSet<TEntity> Set<TEntity>() where TEntity : class;
    EntityEntry<TEntity> Attach<TEntity>(TEntity entity) where TEntity : class;
    EntityEntry<TEntity> Add<TEntity>(TEntity entity) where TEntity : class;
    EntityEntry<TEntity> Update<TEntity>(TEntity entity) where TEntity : class;
    EntityEntry<TEntity> Remove<TEntity>(TEntity entity) where TEntity : class;
}
