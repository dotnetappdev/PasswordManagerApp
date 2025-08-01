using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using PasswordManager.Models;

namespace PasswordManager.DAL.Interfaces;

/// <summary>
/// Interface for the Password Manager Database Context
/// </summary>
public interface IPasswordManagerDbContextApp : IDisposable
{
    /// <summary>
    /// Gets or sets the password items
    /// </summary>
    DbSet<PasswordItem> PasswordItems { get; set; }
    
    /// <summary>
    /// Gets or sets the login items
    /// </summary>
    DbSet<LoginItem> LoginItems { get; set; }
    
    /// <summary>
    /// Gets or sets the credit card items
    /// </summary>
    DbSet<CreditCardItem> CreditCardItems { get; set; }
    
    /// <summary>
    /// Gets or sets the secure note items
    /// </summary>
    DbSet<SecureNoteItem> SecureNoteItems { get; set; }
    
    /// <summary>
    /// Gets or sets the WiFi items
    /// </summary>
    DbSet<WiFiItem> WiFiItems { get; set; }
    
    /// <summary>
    /// Gets or sets the tags
    /// </summary>
    DbSet<Tag> Tags { get; set; }
    
    /// <summary>
    /// Gets or sets the categories
    /// </summary>
    DbSet<Category> Categories { get; set; }
    
    /// <summary>
    /// Gets or sets the collections
    /// </summary>
    DbSet<Collection> Collections { get; set; }
    
    /// <summary>
    /// Gets or sets the API keys
    /// </summary>
    DbSet<ApiKey> ApiKeys { get; set; }
    
    /// <summary>
    /// Gets or sets the QR login tokens
    /// </summary>
    DbSet<QrLoginToken> QrLoginTokens { get; set; }
    
    /// <summary>
    /// Gets or sets the application users
    /// </summary>
    DbSet<ApplicationUser> Users { get; set; }
    
    /// <summary>
    /// Saves all changes made in this context to the database
    /// </summary>
    /// <returns>The number of state entries written to the database</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Saves all changes made in this context to the database
    /// </summary>
    /// <returns>The number of state entries written to the database</returns>
    int SaveChanges();
    
    /// <summary>
    /// Gets an EntityEntry for the given entity
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity</typeparam>
    /// <param name="entity">The entity</param>
    /// <returns>The EntityEntry for the entity</returns>
    EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;
    
    /// <summary>
    /// Creates a DbSet that can be used to query and save instances of TEntity
    /// </summary>
    /// <typeparam name="TEntity">The type of entity for which a set should be returned</typeparam>
    /// <returns>A set for the given entity type</returns>
    DbSet<TEntity> Set<TEntity>() where TEntity : class;
    
    /// <summary>
    /// Begins tracking the given entity and entries reachable from the given entity using the Unchanged state
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity</typeparam>
    /// <param name="entity">The entity to attach</param>
    /// <returns>The EntityEntry for the entity</returns>
    EntityEntry<TEntity> Attach<TEntity>(TEntity entity) where TEntity : class;
    
    /// <summary>
    /// Begins tracking the given entity in the Added state
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity</typeparam>
    /// <param name="entity">The entity to add</param>
    /// <returns>The EntityEntry for the entity</returns>
    EntityEntry<TEntity> Add<TEntity>(TEntity entity) where TEntity : class;
    
    /// <summary>
    /// Begins tracking the given entity in the Modified state
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity</typeparam>
    /// <param name="entity">The entity to update</param>
    /// <returns>The EntityEntry for the entity</returns>
    EntityEntry<TEntity> Update<TEntity>(TEntity entity) where TEntity : class;
    
    /// <summary>
    /// Begins tracking the given entity in the Deleted state
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity</typeparam>
    /// <param name="entity">The entity to remove</param>
    /// <returns>The EntityEntry for the entity</returns>
    EntityEntry<TEntity> Remove<TEntity>(TEntity entity) where TEntity : class;
}
