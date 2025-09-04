using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PasswordManager.Models;
using PasswordManager.DAL.Interfaces;

namespace PasswordManager.DAL;

public class PasswordManagerDbContextApp : IdentityDbContext<ApplicationUser, ApplicationRole, string>, IPasswordManagerDbContextApp
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
    public DbSet<QrLoginToken> QrLoginTokens { get; set; } = null!;
    public DbSet<UserPasskey> UserPasskeys { get; set; } = null!;
    public DbSet<UserTwoFactorBackupCode> UserTwoFactorBackupCodes { get; set; } = null!;
    public DbSet<UserRelationship> UserRelationships { get; set; } = null!;
    public DbSet<ChildPermissionConfig> ChildPermissionConfigs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure Collection
        modelBuilder.Entity<Collection>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Icon).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Color).IsRequired().HasMaxLength(7);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.LastModified).IsRequired();

            // Configure User relationship
            entity.Property(e => e.UserId).IsRequired();
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Collections)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
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

        // Configure QrLoginToken
        modelBuilder.Entity<QrLoginToken>(entity =>
        {
            entity.HasKey(e => e.Token);
            entity.Property(e => e.Token).IsRequired().HasMaxLength(32);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.ExpiresAt).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.IsUsed).IsRequired();
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.Status).HasConversion<int>();
        });

        // Configure UserRelationship
        modelBuilder.Entity<UserRelationship>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ParentUserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.ChildUserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.RelationshipType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.LastModified).IsRequired();
            entity.Property(e => e.CreatedBy).HasMaxLength(450);
            entity.Property(e => e.Notes).HasMaxLength(500);

            // Configure unique constraint to prevent duplicate relationships
            entity.HasIndex(e => new { e.ParentUserId, e.ChildUserId, e.RelationshipType })
                  .IsUnique()
                  .HasDatabaseName("IX_UserRelationship_Unique");

            // Configure foreign key relationships
            entity.HasOne(e => e.ParentUser)
                  .WithMany(u => u.ChildRelationships)
                  .HasForeignKey(e => e.ParentUserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ChildUser)
                  .WithMany(u => u.ParentRelationships)
                  .HasForeignKey(e => e.ChildUserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure ChildPermissionConfig
        modelBuilder.Entity<ChildPermissionConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ChildUserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.ParentUserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.LastModified).IsRequired();
            entity.Property(e => e.AccessStartTime).HasMaxLength(5);
            entity.Property(e => e.AccessEndTime).HasMaxLength(5);
            entity.Property(e => e.AllowedDaysOfWeek).HasMaxLength(20);

            // Configure unique constraint - one config per child-parent pair
            entity.HasIndex(e => new { e.ChildUserId, e.ParentUserId })
                  .IsUnique()
                  .HasDatabaseName("IX_ChildPermissionConfig_Unique");

            // Configure foreign key relationships
            entity.HasOne(e => e.ChildUser)
                  .WithMany(u => u.ChildPermissionConfigs)
                  .HasForeignKey(e => e.ChildUserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ParentUser)
                  .WithMany(u => u.ManagedChildPermissions)
                  .HasForeignKey(e => e.ParentUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
