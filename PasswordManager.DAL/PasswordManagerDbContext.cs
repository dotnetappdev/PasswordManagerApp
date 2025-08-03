using Microsoft.EntityFrameworkCore;
using PasswordManager.Models;
using PasswordManager.DAL.Interfaces;

namespace PasswordManager.DAL;

public class PasswordManagerDbContext : DbContext, IPasswordManagerDbContext
{
    public PasswordManagerDbContext(DbContextOptions<PasswordManagerDbContext> options) : base(options)
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
    public DbSet<ApplicationUser> Users { get; set; } = null!;
    public DbSet<QrLoginToken> QrLoginTokens { get; set; } = null!;
    public DbSet<OtpCode> OtpCodes { get; set; } = null!;
    public DbSet<SmsSettings> SmsSettings { get; set; } = null!;

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

            // Configure Tags (many-to-many)
            entity.HasMany(e => e.Tags)
                  .WithMany(t => t.PasswordItems)
                  .UsingEntity(j => j.ToTable("PasswordItemTags"));

            // Configure User relationship
            entity.Property(e => e.UserId).IsRequired();
            entity.HasOne(e => e.User)
                  .WithMany(u => u.PasswordItems)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure LoginItem
        modelBuilder.Entity<LoginItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Password).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.WebsiteUrl).HasMaxLength(500);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.LastModified).IsRequired();

            // Configure User relationship
            entity.Property(e => e.UserId).IsRequired();
            entity.HasOne(e => e.User)
                  .WithMany(u => u.LoginItems)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure CreditCardItem
        modelBuilder.Entity<CreditCardItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CardholderName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CardNumber).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ExpiryDate).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CVV).IsRequired().HasMaxLength(100);
            entity.Property(e => e.BankWebsite).HasMaxLength(500);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.LastModified).IsRequired();

            // Configure User relationship
            entity.Property(e => e.UserId).IsRequired();
            entity.HasOne(e => e.User)
                  .WithMany(u => u.CreditCardItems)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure SecureNoteItem
        modelBuilder.Entity<SecureNoteItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Content).IsRequired().HasMaxLength(5000);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.LastModified).IsRequired();

            // Configure User relationship
            entity.Property(e => e.UserId).IsRequired();
            entity.HasOne(e => e.User)
                  .WithMany(u => u.SecureNoteItems)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure WiFiItem
        modelBuilder.Entity<WiFiItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NetworkName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Password).IsRequired().HasMaxLength(500);
            entity.Property(e => e.SecurityType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.LastModified).IsRequired();

            // Configure User relationship
            entity.Property(e => e.UserId).IsRequired();
            entity.HasOne(e => e.User)
                  .WithMany(u => u.WiFiItems)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Tag
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Color).IsRequired().HasMaxLength(7);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.LastModified).IsRequired();

            // Configure User relationship
            entity.Property(e => e.UserId).IsRequired();
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Tags)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Category
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Icon).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Color).IsRequired().HasMaxLength(7);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.LastModified).IsRequired();

            // Configure User relationship
            entity.Property(e => e.UserId).IsRequired();
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Categories)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

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

        // Configure ApplicationUser
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.Id).IsRequired();
            entity.Property(e => e.UserName).HasMaxLength(256);
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.LastModified).IsRequired();
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

        // Configure OtpCode
        modelBuilder.Entity<OtpCode>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.CodeHash).IsRequired().HasMaxLength(500);
            entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(20);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.ExpiresAt).IsRequired();
            entity.Property(e => e.AttemptCount).IsRequired();
            entity.Property(e => e.IsUsed).IsRequired();
            entity.Property(e => e.RequestIpAddress).HasMaxLength(45);
            entity.Property(e => e.RequestUserAgent).HasMaxLength(500);

            // Configure User relationship
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Add indexes for performance
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.ExpiresAt });
            entity.HasIndex(e => e.CreatedAt);
        });

        // Configure SmsSettings
        modelBuilder.Entity<SmsSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Provider).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DefaultCountryCode).HasMaxLength(5);
            entity.Property(e => e.MessageTemplate).HasMaxLength(500);
            entity.Property(e => e.TwilioAccountSid).HasMaxLength(1000);
            entity.Property(e => e.TwilioAuthToken).HasMaxLength(1000);
            entity.Property(e => e.TwilioFromPhoneNumber).HasMaxLength(20);
            entity.Property(e => e.AwsAccessKeyId).HasMaxLength(1000);
            entity.Property(e => e.AwsSecretAccessKey).HasMaxLength(1000);
            entity.Property(e => e.AwsRegion).HasMaxLength(50);
            entity.Property(e => e.AwsSenderName).HasMaxLength(100);
            entity.Property(e => e.AzureConnectionString).HasMaxLength(1000);
            entity.Property(e => e.AzureFromPhoneNumber).HasMaxLength(20);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.LastModified).IsRequired();

            // Configure User relationship
            entity.HasOne(e => e.User)
                  .WithMany(u => u.SmsSettings)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Add indexes for performance
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.IsActive });
            entity.HasIndex(e => e.IsActive);
        });
    }
}
