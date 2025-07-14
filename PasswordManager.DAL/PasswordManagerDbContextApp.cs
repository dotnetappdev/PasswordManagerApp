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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure PasswordItem
        modelBuilder.Entity<PasswordItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Type).HasConversion<int>();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.LastModified).IsRequired();

            // Configure Category relationship
            entity.Property(e => e.CategoryId).IsRequired();
            entity.HasOne(e => e.Category)
                  .WithMany(c => c.PasswordItems)
                  .HasForeignKey(e => e.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
                  
            // Configure Collection relationship
            entity.Property(e => e.CollectionId).IsRequired();
            entity.HasOne(e => e.Collection)
                  .WithMany(c => c.PasswordItems)
                  .HasForeignKey(e => e.CollectionId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Configure one-to-one relationships
            entity.HasOne(e => e.LoginItem)
                  .WithOne(l => l.PasswordItem)
                  .HasForeignKey<LoginItem>(l => l.PasswordItemId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CreditCardItem)
                  .WithOne(c => c.PasswordItem)
                  .HasForeignKey<CreditCardItem>(c => c.PasswordItemId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.SecureNoteItem)
                  .WithOne(s => s.PasswordItem)
                  .HasForeignKey<SecureNoteItem>(s => s.PasswordItemId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.WiFiItem)
                  .WithOne(w => w.PasswordItem)
                  .HasForeignKey<WiFiItem>(w => w.PasswordItemId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Configure many-to-many relationship with Tags
            entity.HasMany(e => e.Tags)
                  .WithMany(t => t.PasswordItems)
                  .UsingEntity(j => j.ToTable("PasswordItemTags"));
                  
            // Configure User relationship
            entity.HasOne(e => e.User)
                  .WithMany(u => u.PasswordItems)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure LoginItem
        modelBuilder.Entity<LoginItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Encrypted password fields
            entity.Property(e => e.EncryptedPassword).HasMaxLength(1000);
            entity.Property(e => e.PasswordNonce).HasMaxLength(200);
            entity.Property(e => e.PasswordAuthTag).HasMaxLength(200);
            
            // Encrypted TOTP secret fields
            entity.Property(e => e.EncryptedTotpSecret).HasMaxLength(1000);
            entity.Property(e => e.TotpNonce).HasMaxLength(200);
            entity.Property(e => e.TotpAuthTag).HasMaxLength(200);
            
            // Encrypted security answer fields
            entity.Property(e => e.EncryptedSecurityAnswer1).HasMaxLength(1000);
            entity.Property(e => e.SecurityAnswer1Nonce).HasMaxLength(200);
            entity.Property(e => e.SecurityAnswer1AuthTag).HasMaxLength(200);
            
            entity.Property(e => e.EncryptedSecurityAnswer2).HasMaxLength(1000);
            entity.Property(e => e.SecurityAnswer2Nonce).HasMaxLength(200);
            entity.Property(e => e.SecurityAnswer2AuthTag).HasMaxLength(200);
            
            entity.Property(e => e.EncryptedSecurityAnswer3).HasMaxLength(1000);
            entity.Property(e => e.SecurityAnswer3Nonce).HasMaxLength(200);
            entity.Property(e => e.SecurityAnswer3AuthTag).HasMaxLength(200);
            
            // Encrypted notes fields
            entity.Property(e => e.EncryptedNotes).HasMaxLength(4000);
            entity.Property(e => e.NotesNonce).HasMaxLength(200);
            entity.Property(e => e.NotesAuthTag).HasMaxLength(200);
        });

        // Configure CreditCardItem
        modelBuilder.Entity<CreditCardItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CardNumber).HasMaxLength(19);
            entity.Property(e => e.CVV).HasMaxLength(4);
            entity.Property(e => e.PIN).HasMaxLength(6);
            entity.Property(e => e.CardType).HasConversion<int>();
            entity.Property(e => e.Notes).HasMaxLength(2000);
        });

        // Configure SecureNoteItem
        modelBuilder.Entity<SecureNoteItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).HasMaxLength(10000);
        });

        // Configure WiFiItem
        modelBuilder.Entity<WiFiItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Password).HasMaxLength(500);
            entity.Property(e => e.RouterPassword).HasMaxLength(500);
            entity.Property(e => e.SecurityType).HasConversion<int>();
            entity.Property(e => e.Frequency).HasConversion<int>();
            entity.Property(e => e.Notes).HasMaxLength(2000);
        });

        // Configure Tag
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Color).HasMaxLength(7);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Configure Category
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Icon).HasMaxLength(10);
            entity.Property(e => e.Color).HasMaxLength(10);
            entity.HasIndex(e => e.Name).IsUnique();
            
            // Configure Collection relationship
            entity.HasOne(e => e.Collection)
                  .WithMany(c => c.Categories)
                  .HasForeignKey(e => e.CollectionId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
        
        // Configure Collection
        modelBuilder.Entity<Collection>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Icon).HasMaxLength(10);
            entity.Property(e => e.Color).HasMaxLength(10);
            entity.HasIndex(e => e.Name).IsUnique();
            // Parent-child relationship
            entity.HasOne(e => e.ParentCollection)
                  .WithMany(e => e.Children)
                  .HasForeignKey(e => e.ParentCollectionId)
                  .OnDelete(DeleteBehavior.Restrict);
                  
            // Configure User relationship
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Collections)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure ApplicationUser (extends IdentityUser)
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            // Configure encryption-related properties
            entity.Property(e => e.UserSalt).HasMaxLength(200);
            entity.Property(e => e.MasterPasswordHash).HasMaxLength(500);
            entity.Property(e => e.MasterPasswordIterations).HasDefaultValue(100000);
        });
  
        // Seed data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Use static dates for seeding
        var staticDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        
        // Seed default collections
        modelBuilder.Entity<Collection>().HasData(
            new Collection { 
                Id = 1, 
                Name = "Personal", 
                Icon = "👤", 
                Color = "#3B82F6", 
                CreatedAt = staticDate,
                IsDefault = true
            },
            new Collection { 
                Id = 2, 
                Name = "Work", 
                Icon = "💼", 
                Color = "#10B981", 
                CreatedAt = staticDate,
                IsDefault = false
            },
            new Collection { 
                Id = 3, 
                Name = "Family", 
                Icon = "👪", 
                Color = "#8B5CF6", 
                CreatedAt = staticDate,
                IsDefault = false
            }
        );
        
        // Seed default categories
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Personal", Icon = "👤", Color = "#3B82F6", CollectionId = 1, CreatedAt = staticDate },
            new Category { Id = 2, Name = "Work", Icon = "💼", Color = "#10B981", CollectionId = 2, CreatedAt = staticDate },
            new Category { Id = 3, Name = "Finance", Icon = "💰", Color = "#F59E0B", CollectionId = 1, CreatedAt = staticDate },
            new Category { Id = 4, Name = "Social", Icon = "🌐", Color = "#8B5CF6", CollectionId = 1, CreatedAt = staticDate },
            new Category { Id = 5, Name = "Shopping", Icon = "🛒", Color = "#EF4444", CollectionId = 1, CreatedAt = staticDate },
            new Category { Id = 6, Name = "Entertainment", Icon = "🎮", Color = "#EC4899", CollectionId = 3, CreatedAt = staticDate },
            new Category { Id = 7, Name = "Travel", Icon = "✈️", Color = "#06B6D4", CollectionId = 3, CreatedAt = staticDate },
            new Category { Id = 8, Name = "Health", Icon = "🏥", Color = "#84CC16", CollectionId = 1, CreatedAt = staticDate }
        );
        
        // Seed default tags
        modelBuilder.Entity<Tag>().HasData(
            new Tag { Id = 1, Name = "Personal", Color = "#3B82F6", IsSystemTag = true, CreatedAt = staticDate },
            new Tag { Id = 2, Name = "Work", Color = "#10B981", IsSystemTag = true, CreatedAt = staticDate },
            new Tag { Id = 3, Name = "Finance", Color = "#F59E0B", IsSystemTag = true, CreatedAt = staticDate },
            new Tag { Id = 4, Name = "Social", Color = "#8B5CF6", IsSystemTag = true, CreatedAt = staticDate },
            new Tag { Id = 5, Name = "Shopping", Color = "#EF4444", IsSystemTag = true, CreatedAt = staticDate }
        );
    }
}
