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
        // Identity-specific configuration can go here if needed
    }
}
