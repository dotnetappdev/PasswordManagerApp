using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PasswordManager.Models;
using PasswordManager.DAL.Interfaces;

namespace PasswordManager.DAL;

public class PasswordManagerDbContextApp : IdentityDbContext<ApplicationUser>, IPasswordManagerDbContextApp
{
    public PasswordManagerDbContextApp(DbContextOptions<PasswordManagerDbContextApp> options) : base(options)
    {
    }

    public DbSet<PasswordItem> PasswordItems { get; set; } = null!;
    public DbSet<LoginItem> LoginItems { get; set; } = null!;
    public DbSet<CreditCardItem> CreditCardItems { get; set; } = null!;
    public DbSet<SecureNoteItem> SecureNoteItems { get; set; } = null!;
    public DbSet<WiFiItem> WiFiItems { get; set; } = null!;
    public DbSet<Tag> Tags { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Collection> Collections { get; set; } = null!;
    public DbSet<ApiKey> ApiKeys { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure Collection
        modelBuilder.Entity<Collection>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.LastModified).IsRequired();

            // Configure User relationship
            entity.Property(e => e.UserId).IsRequired();
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Collections)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            // Ignore the duplicate navigation properties that cause the mapping conflict
            entity.Ignore(e => e.Parent);
            entity.Ignore(e => e.ParentId);
            
            // Configure self-referencing relationship
            entity.HasOne(e => e.ParentCollection)
                  .WithMany(e => e.Children)
                  .HasForeignKey(e => e.ParentCollectionId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
        
        // Configure ApiKey
        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.KeyHash).IsRequired().HasMaxLength(500);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UserId).IsRequired();

            // Configure User relationship
            entity.HasOne(e => e.User)
                  .WithMany(u => u.ApiKeys)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Identity-specific configuration can go here if needed
    }
}
