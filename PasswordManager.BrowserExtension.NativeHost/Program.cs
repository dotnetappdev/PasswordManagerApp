using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace PasswordManager.BrowserExtension.NativeHost;

public class Program
{
    private static PasswordManagerDbContext? _dbContext;
    private static readonly Dictionary<string, (string userId, byte[] masterKey)> _sessions = new();

    public static async Task Main(string[] args)
    {
        try
        {
            // Initialize services
            InitializeServices();

            // Start native messaging loop
            await ProcessNativeMessages();
        }
        catch (Exception ex)
        {
            // Log error to stderr (not visible to browser extension)
            await Console.Error.WriteLineAsync($"Native Host Error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    private static void InitializeServices()
    {
        // Initialize database context
        var connectionString = GetDatabasePath();
        var options = new DbContextOptionsBuilder<PasswordManagerDbContext>()
            .UseSqlite(connectionString)
            .Options;
        
        _dbContext = new PasswordManagerDbContext(options);
    }

    private static string GetDatabasePath()
    {
        // Try to find the database in common locations
        var possiblePaths = new[]
        {
            "passwordmanager_dev.db",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PasswordManager", "passwordmanager.db"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PasswordManager", "passwordmanager.db"),
            // For development - look for the API's database
            Path.Combine(Directory.GetCurrentDirectory(), "..", "PasswordManager.API", "passwordmanager_dev.db"),
            Path.Combine(Directory.GetCurrentDirectory(), "passwordmanager_dev.db")
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                return $"Data Source={path}";
            }
        }

        // If not found, use default path (will be created if needed)
        return $"Data Source={Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PasswordManager", "passwordmanager.db")}";
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

        try
        {
            return action switch
            {
                "login" => await HandleLogin(message),
                "getCredentials" => await HandleGetCredentials(message),
                "generatePassword" => HandleGeneratePassword(message),
                "testConnection" => await HandleTestConnection(),
                _ => new { success = false, error = "Unknown action" }
            };
        }
        catch (Exception ex)
        {
            return new { success = false, error = ex.Message };
        }
    }

    private static async Task<object> HandleLogin(Dictionary<string, object> message)
    {
        if (!message.TryGetValue("email", out var emailObj) || emailObj is not JsonElement emailElement ||
            !message.TryGetValue("password", out var passwordObj) || passwordObj is not JsonElement passwordElement)
        {
            return new { success = false, error = "Email and password are required" };
        }

        var email = emailElement.GetString();
        var password = passwordElement.GetString();

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            return new { success = false, error = "Email and password cannot be empty" };
        }

        try
        {
            // Find user by email
            var user = await _dbContext!.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                return new { success = false, error = "Invalid email or password" };
            }

            // Verify password
            var userSalt = Convert.FromBase64String(user.UserSalt);
            var isValid = VerifyMasterPassword(password, user.MasterPasswordHash, userSalt, user.MasterPasswordIterations);
            
            if (!isValid)
            {
                return new { success = false, error = "Invalid email or password" };
            }

            // Derive master key and create session
            var masterKey = DeriveMasterKey(password, userSalt);
            var sessionId = Guid.NewGuid().ToString();
            _sessions[sessionId] = (user.Id, masterKey);

            return new { 
                success = true, 
                token = sessionId,
                message = "Login successful" 
            };
        }
        catch (Exception ex)
        {
            return new { success = false, error = $"Login failed: {ex.Message}" };
        }
    }

    private static async Task<object> HandleGetCredentials(Dictionary<string, object> message)
    {
        if (!message.TryGetValue("token", out var tokenObj) || tokenObj is not JsonElement tokenElement)
        {
            return new { success = false, error = "Authentication token required" };
        }

        var token = tokenElement.GetString();
        if (string.IsNullOrEmpty(token) || !_sessions.TryGetValue(token, out var session))
        {
            return new { success = false, error = "Invalid or expired session" };
        }

        var domain = "";
        if (message.TryGetValue("domain", out var domainObj) && domainObj is JsonElement domainElement)
        {
            domain = domainElement.GetString() ?? "";
        }

        try
        {
            var query = _dbContext!.PasswordItems
                .Include(p => p.LoginItem)
                .Where(p => p.UserId == session.userId && p.Type == ItemType.Login && !p.IsDeleted);

            var passwordItems = await query.ToListAsync();

            var credentials = new List<object>();

            foreach (var item in passwordItems)
            {
                if (item.LoginItem == null) continue;

                // Filter by domain if specified
                var websiteUrl = item.LoginItem.WebsiteUrl ?? item.LoginItem.Website ?? "";
                if (!string.IsNullOrEmpty(domain) && !DomainMatches(websiteUrl, domain))
                    continue;

                // Decrypt password if available
                string decryptedPassword = "";
                if (!string.IsNullOrEmpty(item.LoginItem.EncryptedPassword) &&
                    !string.IsNullOrEmpty(item.LoginItem.PasswordNonce) &&
                    !string.IsNullOrEmpty(item.LoginItem.PasswordAuthTag))
                {
                    try
                    {
                        var encryptedData = new EncryptedPasswordData
                        {
                            EncryptedPassword = item.LoginItem.EncryptedPassword,
                            Nonce = item.LoginItem.PasswordNonce,
                            AuthenticationTag = item.LoginItem.PasswordAuthTag
                        };
                        decryptedPassword = DecryptPasswordWithKey(encryptedData, session.masterKey);
                    }
                    catch
                    {
                        // If decryption fails, leave password empty
                        decryptedPassword = "";
                    }
                }

                credentials.Add(new
                {
                    id = item.Id,
                    title = item.Title,
                    username = item.LoginItem.Username ?? "",
                    password = decryptedPassword,
                    websiteUrl = websiteUrl
                });
            }

            return new { 
                success = true, 
                credentials = credentials 
            };
        }
        catch (Exception ex)
        {
            return new { success = false, error = $"Failed to get credentials: {ex.Message}" };
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
                message = "Database connection successful" 
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

    // Crypto utility methods
    private static byte[] DeriveMasterKey(string masterPassword, byte[] userSalt)
    {
        const int masterKeyIterations = 600000;
        const int masterKeyLength = 32;
        
        using var pbkdf2 = new Rfc2898DeriveBytes(masterPassword, userSalt, masterKeyIterations, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(masterKeyLength);
    }

    private static bool VerifyMasterPassword(string password, string storedHash, byte[] userSalt, int iterations)
    {
        // Derive master key from password and salt
        var masterKey = DeriveMasterKey(password, userSalt);
        
        try 
        {
            // Create auth hash using same method as stored hash
            var authHash = CreateAuthHash(masterKey, password);
            return authHash == storedHash;
        }
        finally
        {
            Array.Clear(masterKey, 0, masterKey.Length);
        }
    }

    private static string CreateAuthHash(byte[] masterKey, string masterPassword)
    {
        const int authHashIterations = 600000;
        
        // Combine master key and master password as input
        var masterKeyAndPassword = masterKey.Concat(Encoding.UTF8.GetBytes(masterPassword)).ToArray();
        
        try
        {
            // Create salt for auth hash (single iteration with master key as salt)
            using var pbkdf2 = new Rfc2898DeriveBytes(masterKeyAndPassword, masterKey, authHashIterations, HashAlgorithmName.SHA256);
            var authHash = pbkdf2.GetBytes(32); // 256 bits
            return Convert.ToBase64String(authHash);
        }
        finally
        {
            Array.Clear(masterKeyAndPassword, 0, masterKeyAndPassword.Length);
        }
    }

    private static string DecryptPasswordWithKey(EncryptedPasswordData encryptedPasswordData, byte[] masterKey)
    {
        // Reconstruct encrypted data
        var ciphertext = Convert.FromBase64String(encryptedPasswordData.EncryptedPassword);
        var nonce = Convert.FromBase64String(encryptedPasswordData.Nonce);
        var tag = Convert.FromBase64String(encryptedPasswordData.AuthenticationTag);

        // Decrypt using AES-256-GCM
        using var aes = new AesGcm(masterKey);
        var plaintextBytes = new byte[ciphertext.Length];
        aes.Decrypt(nonce, ciphertext, tag, plaintextBytes);
        
        return Encoding.UTF8.GetString(plaintextBytes);
    }

    private static bool DomainMatches(string websiteUrl, string currentDomain)
    {
        if (string.IsNullOrEmpty(websiteUrl) || string.IsNullOrEmpty(currentDomain)) 
            return false;
        
        try
        {
            // Clean up the website URL
            var cleanUrl = websiteUrl.ToLower();
            if (!cleanUrl.StartsWith("http://") && !cleanUrl.StartsWith("https://"))
            {
                cleanUrl = "https://" + cleanUrl;
            }
            
            var urlDomain = new Uri(cleanUrl).Host.Replace("www.", "");
            var currentCleanDomain = currentDomain.Replace("www.", "");
            
            return urlDomain == currentCleanDomain || 
                   urlDomain.EndsWith("." + currentCleanDomain) ||
                   currentCleanDomain.EndsWith("." + urlDomain);
        }
        catch
        {
            // Fallback to simple string matching
            return websiteUrl.ToLower().Contains(currentDomain.ToLower());
        }
    }

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

public class PasswordManagerDbContext : IdentityDbContext<ApplicationUser>
{
    public PasswordManagerDbContext(DbContextOptions<PasswordManagerDbContext> options) : base(options)
    {
    }

    public DbSet<PasswordItem> PasswordItems { get; set; } = null!;
    public DbSet<LoginItem> LoginItems { get; set; } = null!;

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

        // Configure ApplicationUser
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.Id).IsRequired();
            entity.Property(e => e.UserName).HasMaxLength(256);
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.LastModified).IsRequired();
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