using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace VaultGuard.BrowserExtension.NativeHost;

public class Program
{
    private static VaultGuardDbContext? _dbContext;
    private static string _currentDbPath = "";

    // The browser passes the calling extension's origin (Chrome: "chrome-extension://<id>/"; Firefox: the
    // add-on id as the second argument). Forwarded to the app so its approval prompt can say who is asking.
    private static string? _callerOrigin;

    // Requests answered by the running desktop app when it's open (see AppLinkClient).
    private static readonly HashSet<string> AppLinkActions = new() { "hello", "pair", "getCredentials", "getCreditCards", "saveTotpSecret", "getTotpCode", "saveLogin", "checkPasswordReuse", "passkeyCreate", "passkeyGet" };

    public static async Task Main(string[] args)
    {
        _callerOrigin = args.FirstOrDefault(a => a.Contains("-extension://")) ?? (args.Length > 1 ? args[1] : null);
        try
        {
            // Initialize services
            InitializeServices();

            // Start native messaging loop
            await ProcessNativeMessages();
        }
        catch (Exception ex)
        {
            // Durable log + stderr (stdout is reserved for the native-messaging protocol).
            NativeHostLogger.Error("Fatal native host error", ex);
            await Console.Error.WriteLineAsync($"Native Host Error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    private static void InitializeServices()
    {
        // Initialize database context
        _currentDbPath = ResolveDatabasePath();
        var options = new DbContextOptionsBuilder<VaultGuardDbContext>()
            .UseSqlite($"Data Source={_currentDbPath}")
            .Options;

        _dbContext = new VaultGuardDbContext(options);
    }


    /// <summary>Returns the raw path to the SQLite vault file the host will use.</summary>
    private static string ResolveDatabasePath()
    {
        // Try to find the database in common locations
        var possiblePaths = new[]
        {
            "passwordmanager_dev.db",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VaultGuard", "passwordmanager.db"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VaultGuard", "passwordmanager.db"),
            // For development - look for the API's database
            Path.Combine(Directory.GetCurrentDirectory(), "..", "VaultGuard.API", "passwordmanager_dev.db"),
            Path.Combine(Directory.GetCurrentDirectory(), "passwordmanager_dev.db")
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                return Path.GetFullPath(path);
            }
        }

        // If not found, use default path (will be created if needed)
        return Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VaultGuard", "passwordmanager.db"));
    }

    private static async Task ProcessNativeMessages()
    {
        while (true)
        {
            try
            {
                var message = await ReadNativeMessage();
                if (message == null) break;

                var response = await ProcessMessage(message);
                await WriteNativeMessage(response);
            }
            catch (Exception ex)
            {
                NativeHostLogger.Error("Error processing native message", ex);
                var errorResponse = new { success = false, error = ex.Message };
                await WriteNativeMessage(errorResponse);
            }
        }
    }

    private static async Task<Dictionary<string, object>?> ReadNativeMessage()
    {
        var stdin = Console.OpenStandardInput();
        
        // Read message length (4 bytes, little-endian)
        var lengthBytes = new byte[4];
        var bytesRead = await stdin.ReadAsync(lengthBytes, 0, 4);
        if (bytesRead != 4) return null;

        var length = BitConverter.ToInt32(lengthBytes, 0);
        if (length <= 0 || length > 1024 * 1024) return null; // Max 1MB message

        // Read message content
        var messageBytes = new byte[length];
        var totalRead = 0;
        while (totalRead < length)
        {
            var read = await stdin.ReadAsync(messageBytes, totalRead, length - totalRead);
            if (read == 0) return null;
            totalRead += read;
        }

        var messageText = System.Text.Encoding.UTF8.GetString(messageBytes);
        return JsonSerializer.Deserialize<Dictionary<string, object>>(messageText);
    }

    private static async Task WriteNativeMessage(object response)
    {
        var json = JsonSerializer.Serialize(response);
        var messageBytes = System.Text.Encoding.UTF8.GetBytes(json);
        var lengthBytes = BitConverter.GetBytes(messageBytes.Length);

        var stdout = Console.OpenStandardOutput();
        await stdout.WriteAsync(lengthBytes, 0, 4);
        await stdout.WriteAsync(messageBytes, 0, messageBytes.Length);
        await stdout.FlushAsync();
    }

    private static async Task<object> ProcessMessage(Dictionary<string, object> message)
    {
        if (!message.TryGetValue("action", out var actionObj) || actionObj is not JsonElement actionElement)
        {
            return new { success = false, error = "No action specified" };
        }

        var action = actionElement.GetString();
        
        // Check if a custom database path was provided
        if (message.TryGetValue("databasePath", out var dbPathObj) && dbPathObj is JsonElement dbPathElement)
        {
            var customDbPath = dbPathElement.GetString();
            if (!string.IsNullOrEmpty(customDbPath) && File.Exists(customDbPath))
            {
                // Reinitialize database with custom path
                var fullPath = Path.GetFullPath(customDbPath);
                if (!string.Equals(fullPath, _currentDbPath, StringComparison.OrdinalIgnoreCase))
                {
                    var options = new DbContextOptionsBuilder<VaultGuardDbContext>()
                        .UseSqlite($"Data Source={fullPath}")
                        .Options;

                    _dbContext = new VaultGuardDbContext(options);
                    _currentDbPath = fullPath;
                    _passkeySchemaEnsured = false;
                    _customFieldSchemaEnsured = false; // re-check the schema on the new DB
                }
            }
        }

        // Browser link: when the Vault Guard app is running, it answers from its unlocked vault (pairing and
        // lock state are enforced there). Only when it isn't running do we fall back to the local handlers below.
        if (action != null && AppLinkActions.Contains(action))
        {
            var timeout = action == "pair" ? TimeSpan.FromMinutes(3) : TimeSpan.FromSeconds(20);
            var fromApp = await AppLinkClient.TryForwardAsync(message, _callerOrigin, timeout);
            if (fromApp is { } reply) return reply;
            if (action == "hello")
                return new { success = true, linked = false, protocol = AppLinkClient.ProtocolVersion, error = "Vault Guard isn't running. Open it to use the extension." };
            if (action == "pair")
                return new { success = false, code = "APP_NOT_RUNNING", error = "Open Vault Guard to connect this browser." };
            // Everything else needs the running app. The old fallback (this helper reading the database itself
            // after the extension sent it the master password) is gone: it sent the master password over the
            // local channel unencrypted, and it never kept a session between requests anyway.
            return new { success = false, code = "APP_NOT_RUNNING", error = "Open Vault Guard on this computer to use the extension." };
        }

        try
        {
            return action switch
            {
                // Never accept the master password here (see above): sign in to the Vault Guard app instead.
                "login" => new { success = false, code = "USE_APP", error = "Sign in to the Vault Guard app on this computer, then click Connect in the extension." },
                "generatePassword" => HandleGeneratePassword(message),
                "testConnection" => await HandleTestConnection(),
                _ => new { success = false, error = "Unknown action" }
            };
        }
        catch (Exception ex)
        {
            NativeHostLogger.Error($"Error dispatching action '{action}'", ex);
            return new { success = false, error = ex.Message };
        }
    }




    private static object HandleGeneratePassword(Dictionary<string, object> message)
    {
        // Extract options
        var length = 16;
        var includeUppercase = true;
        var includeLowercase = true;
        var includeNumbers = true;
        var includeSymbols = true;

        if (message.TryGetValue("options", out var optionsObj) && optionsObj is JsonElement optionsElement)
        {
            if (optionsElement.TryGetProperty("length", out var lengthProp))
                length = lengthProp.GetInt32();
            if (optionsElement.TryGetProperty("includeUppercase", out var upperProp))
                includeUppercase = upperProp.GetBoolean();
            if (optionsElement.TryGetProperty("includeLowercase", out var lowerProp))
                includeLowercase = lowerProp.GetBoolean();
            if (optionsElement.TryGetProperty("includeNumbers", out var numbersProp))
                includeNumbers = numbersProp.GetBoolean();
            if (optionsElement.TryGetProperty("includeSymbols", out var symbolsProp))
                includeSymbols = symbolsProp.GetBoolean();
        }

        var password = GeneratePassword(length, includeUppercase, includeLowercase, includeNumbers, includeSymbols);
        
        return new { 
            success = true, 
            password = password 
        };
    }

    private static async Task<object> HandleTestConnection()
    {
        try
        {
            // Test database connection
            await _dbContext!.Database.OpenConnectionAsync();
            await _dbContext.Database.CloseConnectionAsync();

            return new {
                success = true,
                message = "Database connection successful",
                databasePath = _currentDbPath
            };
        }
        catch (Exception ex)
        {
            return new { 
                success = false, 
                error = $"Database connection failed: {ex.Message}" 
            };
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Passkey (WebAuthn virtual authenticator) — software keys stored in the vault.
    //
    // 1Password model: the host generates a P-256 key pair on registration, encrypts
    // the private key under the user's master key (AES-GCM, zero-knowledge) and stores
    // it in the UserPasskeys table. On authentication it decrypts the private key and
    // signs the challenge. The browser never sees the private key.
    // ─────────────────────────────────────────────────────────────────────────────

    private static readonly byte[] Aaguid = new byte[16]; // all-zero AAGUID (privacy-preserving)

    private static bool _passkeySchemaEnsured;
    private static bool _customFieldSchemaEnsured;









    // --- minimal CBOR writers (definite-length, values < 24..65535) -----------------












    private static string GeneratePassword(int length, bool includeUppercase, bool includeLowercase, bool includeNumbers, bool includeSymbols)
    {
        var charset = "";
        
        if (includeLowercase) charset += "abcdefghijklmnopqrstuvwxyz";
        if (includeUppercase) charset += "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        if (includeNumbers) charset += "0123456789";
        if (includeSymbols) charset += "!@#$%^&*()_+-=[]{}|;:,.<>?";
        
        if (string.IsNullOrEmpty(charset))
        {
            charset = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        }
        
        var password = "";
        var random = new Random();
        
        for (int i = 0; i < length; i++)
        {
            password += charset[random.Next(charset.Length)];
        }
        
        return password;
    }
}

// Models and DTOs needed for the application
public enum CardType
{
    Visa,
    MasterCard,
    AmericanExpress,
    Discover,
    DinersClub,
    JCB,
    Other
}

public enum ItemType
{
    Login = 0,
    CreditCard = 1,
    SecureNote = 2,
    WiFi = 3
}

public class ApplicationUser : IdentityUser
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    public string UserSalt { get; set; } = "";
    public string MasterPasswordHash { get; set; } = "";
    public int MasterPasswordIterations { get; set; } = 600000;
    public List<PasswordItem> PasswordItems { get; set; } = new();
    public List<LoginItem> LoginItems { get; set; } = new();
    public List<CreditCardItem> CreditCardItems { get; set; } = new();
}

public class PasswordItem
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public ItemType Type { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    
    public bool IsFavorite { get; set; }
    
    public bool IsArchived { get; set; }
    
    public bool IsDeleted { get; set; }
    
    // User relationship
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }
    
    public int? CategoryId { get; set; }
    public int? CollectionId { get; set; }

    // Navigation properties
    public LoginItem? LoginItem { get; set; }
    public CreditCardItem? CreditCardItem { get; set; }
}

public class LoginItem
{
    public int Id { get; set; }
    public int PasswordItemId { get; set; }

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    // User relationship
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    // WebsiteUrl
    [MaxLength(200)]
    public string? WebsiteUrl { get; set; }
    
    [MaxLength(200)]
    public string? Website { get; set; }
    
    [MaxLength(100)]
    public string? Username { get; set; }
    
    // Encrypted password storage (Base64 encoded ciphertext)
    [MaxLength(1000)]
    public string? EncryptedPassword { get; set; }
    
    // Nonce for AES-GCM encryption (Base64 encoded)
    [MaxLength(200)]
    public string? PasswordNonce { get; set; }
    
    // Authentication tag for AES-GCM (Base64 encoded)
    [MaxLength(200)]
    public string? PasswordAuthTag { get; set; }

    // Navigation property
    public PasswordItem PasswordItem { get; set; } = null!;
}

public class CreditCardItem
{
    public int Id { get; set; }
    public int PasswordItemId { get; set; }

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    // User relationship
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }
    
    // Card Details
    [MaxLength(100)]
    public string? CardholderName { get; set; }
    
    [MaxLength(19)] // Maximum for credit card numbers with spaces
    public string? CardNumber { get; set; }
    
    [MaxLength(7)] // MM/YYYY format
    public string? ExpiryDate { get; set; }
    
    [MaxLength(4)]
    public string? CVV { get; set; }
    
    public CardType CardType { get; set; }
    
    // Billing Address
    [MaxLength(200)]
    public string? BillingAddressLine1 { get; set; }
    
    [MaxLength(200)]
    public string? BillingAddressLine2 { get; set; }
    
    [MaxLength(100)]
    public string? BillingCity { get; set; }
    
    [MaxLength(50)]
    public string? BillingState { get; set; }
    
    [MaxLength(20)]
    public string? BillingZipCode { get; set; }
    
    [MaxLength(50)]
    public string? BillingCountry { get; set; }
    
    // Encrypted card data (Base64 encoded ciphertext)
    [MaxLength(1000)]
    public string? EncryptedCardNumber { get; set; }
    [MaxLength(200)]
    public string? CardNumberNonce { get; set; }
    [MaxLength(200)]
    public string? CardNumberAuthTag { get; set; }
    [MaxLength(1000)]
    public string? EncryptedCvv { get; set; }
    [MaxLength(200)]
    public string? CvvNonce { get; set; }
    [MaxLength(200)]
    public string? CvvAuthTag { get; set; }

    // Navigation property
    public PasswordItem PasswordItem { get; set; } = null!;
}

/// <summary>
/// WebAuthn passkey row. Mirrors the main app's UserPasskey table. The private key is held
/// (AES-GCM encrypted under the master key) inside <see cref="EncryptedVaultData"/>; RpId is an
/// extra plaintext column added at runtime by the host so credentials can be looked up per site.
/// </summary>
public class UserPasskey
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string CredentialId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public uint SignatureCounter { get; set; }
    public string? DeviceType { get; set; }
    public bool IsBackedUp { get; set; }
    public bool RequiresUserVerification { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public bool StoreInVault { get; set; } = true;
    public string? EncryptedVaultData { get; set; }
    public string? RpId { get; set; }
}

public class CustomField
{
    public int Id { get; set; }
    public int PasswordItemId { get; set; }
    [MaxLength(100)]
    public string? Name { get; set; }
    public string? Value { get; set; }
    public int Type { get; set; }
    public bool IsProtected { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    public PasswordItem PasswordItem { get; set; } = null!;
}

public class VaultGuardDbContext : IdentityDbContext<ApplicationUser>
{
    public VaultGuardDbContext(DbContextOptions<VaultGuardDbContext> options) : base(options)
    {
    }

    public DbSet<PasswordItem> PasswordItems { get; set; } = null!;
    public DbSet<LoginItem> LoginItems { get; set; } = null!;
    public DbSet<CreditCardItem> CreditCardItems { get; set; } = null!;
    public DbSet<UserPasskey> UserPasskeys { get; set; } = null!;
    public DbSet<CustomField> CustomFields { get; set; } = null!;

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
            entity.Property(e => e.Username).HasMaxLength(100);
            entity.Property(e => e.EncryptedPassword).HasMaxLength(1000);
            entity.Property(e => e.PasswordNonce).HasMaxLength(200);
            entity.Property(e => e.PasswordAuthTag).HasMaxLength(200);
            entity.Property(e => e.WebsiteUrl).HasMaxLength(500);
            entity.Property(e => e.Website).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.LastModified).IsRequired();

            // Configure User relationship
            entity.Property(e => e.UserId).IsRequired();
            entity.HasOne(e => e.User)
                  .WithMany(u => u.LoginItems)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Configure PasswordItem relationship
            entity.HasOne(e => e.PasswordItem)
                  .WithOne(p => p.LoginItem)
                  .HasForeignKey<LoginItem>(e => e.PasswordItemId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure CreditCardItem
        modelBuilder.Entity<CreditCardItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CardholderName).HasMaxLength(100);
            entity.Property(e => e.CardNumber).HasMaxLength(19);
            entity.Property(e => e.ExpiryDate).HasMaxLength(7);
            entity.Property(e => e.CVV).HasMaxLength(4);
            entity.Property(e => e.CardType).HasConversion<int>();
            entity.Property(e => e.BillingAddressLine1).HasMaxLength(200);
            entity.Property(e => e.BillingAddressLine2).HasMaxLength(200);
            entity.Property(e => e.BillingCity).HasMaxLength(100);
            entity.Property(e => e.BillingState).HasMaxLength(50);
            entity.Property(e => e.BillingZipCode).HasMaxLength(20);
            entity.Property(e => e.BillingCountry).HasMaxLength(50);
            entity.Property(e => e.EncryptedCardNumber).HasMaxLength(1000);
            entity.Property(e => e.CardNumberNonce).HasMaxLength(200);
            entity.Property(e => e.CardNumberAuthTag).HasMaxLength(200);
            entity.Property(e => e.EncryptedCvv).HasMaxLength(1000);
            entity.Property(e => e.CvvNonce).HasMaxLength(200);
            entity.Property(e => e.CvvAuthTag).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.LastModified).IsRequired();

            // Configure User relationship
            entity.Property(e => e.UserId).IsRequired();
            entity.HasOne(e => e.User)
                  .WithMany(u => u.CreditCardItems)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Configure PasswordItem relationship
            entity.HasOne(e => e.PasswordItem)
                  .WithOne(p => p.CreditCardItem)
                  .HasForeignKey<CreditCardItem>(e => e.PasswordItemId)
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

        // Configure UserPasskey (maps to the existing UserPasskeys table).
        modelBuilder.Entity<UserPasskey>(entity =>
        {
            entity.ToTable("UserPasskeys");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.CredentialId).IsRequired().HasMaxLength(1024);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PublicKey).IsRequired().HasMaxLength(2048);
            entity.Property(e => e.DeviceType).HasMaxLength(50);
            entity.Property(e => e.EncryptedVaultData).HasMaxLength(4096);
            // RpId is an extra column the host adds via ALTER TABLE (EnsurePasskeySchema).
            entity.Property(e => e.RpId).HasMaxLength(256);
        });

        // Configure CustomField
        modelBuilder.Entity<CustomField>(entity =>
        {
            entity.ToTable("CustomFields");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.LastModified).IsRequired();
            entity.HasOne(e => e.PasswordItem)
                  .WithMany()
                  .HasForeignKey(e => e.PasswordItemId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

// DTO classes
public class EncryptedPasswordData
{
    public string EncryptedPassword { get; set; } = "";
    public string Nonce { get; set; } = "";
    public string AuthenticationTag { get; set; } = "";
}