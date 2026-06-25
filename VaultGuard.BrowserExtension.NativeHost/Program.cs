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
    private static string _currentDbPath = "";
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
        _currentDbPath = ResolveDatabasePath();
        var options = new DbContextOptionsBuilder<PasswordManagerDbContext>()
            .UseSqlite($"Data Source={_currentDbPath}")
            .Options;

        _dbContext = new PasswordManagerDbContext(options);
    }

    private static string GetDatabasePath() => $"Data Source={ResolveDatabasePath()}";

    /// <summary>Returns the raw path to the SQLite vault file the host will use.</summary>
    private static string ResolveDatabasePath()
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
                return Path.GetFullPath(path);
            }
        }

        // If not found, use default path (will be created if needed)
        return Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PasswordManager", "passwordmanager.db"));
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
                    var options = new DbContextOptionsBuilder<PasswordManagerDbContext>()
                        .UseSqlite($"Data Source={fullPath}")
                        .Options;

                    _dbContext = new PasswordManagerDbContext(options);
                    _currentDbPath = fullPath;
                    _passkeySchemaEnsured = false;
                    _customFieldSchemaEnsured = false; // re-check the schema on the new DB
                }
            }
        }

        try
        {
            return action switch
            {
                "login" => await HandleLogin(message),
                "getCredentials" => await HandleGetCredentials(message),
                "getCreditCards" => await HandleGetCreditCards(message),
                "generatePassword" => HandleGeneratePassword(message),
                "testConnection" => await HandleTestConnection(),
                "passkeyCreate" => await HandlePasskeyCreate(message),
                "passkeyGet" => await HandlePasskeyGet(message),
                "saveTotpSecret" => await HandleSaveTotpSecret(message),
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

        var email = emailElement.GetString()?.Trim();
        var password = passwordElement.GetString()?.Trim();

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

    private static async Task<object> HandleGetCreditCards(Dictionary<string, object> message)
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
                .Include(p => p.CreditCardItem)
                .Where(p => p.UserId == session.userId && p.Type == ItemType.CreditCard && !p.IsDeleted);

            var passwordItems = await query.ToListAsync();

            var creditCards = new List<object>();

            foreach (var item in passwordItems)
            {
                if (item.CreditCardItem == null) continue;

                // Decrypt credit card data if available
                string decryptedCardNumber = "";
                string decryptedCvv = "";

                // Decrypt card number
                if (!string.IsNullOrEmpty(item.CreditCardItem.EncryptedCardNumber) &&
                    !string.IsNullOrEmpty(item.CreditCardItem.CardNumberNonce) &&
                    !string.IsNullOrEmpty(item.CreditCardItem.CardNumberAuthTag))
                {
                    try
                    {
                        var encryptedData = new EncryptedPasswordData
                        {
                            EncryptedPassword = item.CreditCardItem.EncryptedCardNumber,
                            Nonce = item.CreditCardItem.CardNumberNonce,
                            AuthenticationTag = item.CreditCardItem.CardNumberAuthTag
                        };
                        decryptedCardNumber = DecryptPasswordWithKey(encryptedData, session.masterKey);
                    }
                    catch
                    {
                        // If decryption fails, use unencrypted version if available
                        decryptedCardNumber = item.CreditCardItem.CardNumber ?? "";
                    }
                }
                else
                {
                    decryptedCardNumber = item.CreditCardItem.CardNumber ?? "";
                }

                // Decrypt CVV
                if (!string.IsNullOrEmpty(item.CreditCardItem.EncryptedCvv) &&
                    !string.IsNullOrEmpty(item.CreditCardItem.CvvNonce) &&
                    !string.IsNullOrEmpty(item.CreditCardItem.CvvAuthTag))
                {
                    try
                    {
                        var encryptedData = new EncryptedPasswordData
                        {
                            EncryptedPassword = item.CreditCardItem.EncryptedCvv,
                            Nonce = item.CreditCardItem.CvvNonce,
                            AuthenticationTag = item.CreditCardItem.CvvAuthTag
                        };
                        decryptedCvv = DecryptPasswordWithKey(encryptedData, session.masterKey);
                    }
                    catch
                    {
                        // If decryption fails, use unencrypted version if available
                        decryptedCvv = item.CreditCardItem.CVV ?? "";
                    }
                }
                else
                {
                    decryptedCvv = item.CreditCardItem.CVV ?? "";
                }

                creditCards.Add(new
                {
                    id = item.Id,
                    title = item.Title,
                    cardholderName = item.CreditCardItem.CardholderName ?? "",
                    cardNumber = decryptedCardNumber,
                    expiryDate = item.CreditCardItem.ExpiryDate ?? "",
                    cvv = decryptedCvv,
                    cardType = item.CreditCardItem.CardType.ToString(),
                    billingAddressLine1 = item.CreditCardItem.BillingAddressLine1 ?? "",
                    billingAddressLine2 = item.CreditCardItem.BillingAddressLine2 ?? "",
                    billingCity = item.CreditCardItem.BillingCity ?? "",
                    billingState = item.CreditCardItem.BillingState ?? "",
                    billingZipCode = item.CreditCardItem.BillingZipCode ?? "",
                    billingCountry = item.CreditCardItem.BillingCountry ?? ""
                });
            }

            return new { 
                success = true, 
                creditCards = creditCards 
            };
        }
        catch (Exception ex)
        {
            return new { success = false, error = $"Failed to get credit cards: {ex.Message}" };
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

    /// <summary>
    /// Adds the extra "RpId" column to the UserPasskeys table if it isn't there yet.
    /// SQLite ignores the statement target if the column already exists (we swallow the error),
    /// so this is a safe, migration-free schema top-up that the main app tolerates too.
    /// </summary>
    private static async Task EnsurePasskeySchemaAsync()
    {
        if (_passkeySchemaEnsured) return;
        try
        {
            await _dbContext!.Database.ExecuteSqlRawAsync("ALTER TABLE \"UserPasskeys\" ADD COLUMN \"RpId\" TEXT NULL");
        }
        catch
        {
            // Column already exists (or table is being created elsewhere) — fine.
        }
        _passkeySchemaEnsured = true;
    }

    private static async Task EnsureCustomFieldSchemaAsync()
    {
        if (_customFieldSchemaEnsured) return;
        try
        {
            // Create the CustomFields table if it doesn't exist yet (safe — EF may have already done it).
            await _dbContext!.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""CustomFields"" (
                    ""Id""             INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    ""PasswordItemId"" INTEGER NOT NULL REFERENCES ""PasswordItems""(""Id"") ON DELETE CASCADE,
                    ""Name""           TEXT NULL,
                    ""Value""          TEXT NULL,
                    ""Type""           INTEGER NOT NULL DEFAULT 0,
                    ""IsProtected""    INTEGER NOT NULL DEFAULT 0,
                    ""DisplayOrder""   INTEGER NOT NULL DEFAULT 0,
                    ""CreatedAt""      TEXT NOT NULL,
                    ""LastModified""   TEXT NOT NULL
                )");
        }
        catch
        {
            // Table already exists — fine.
        }
        _customFieldSchemaEnsured = true;
    }

    private static async Task<object> HandleSaveTotpSecret(Dictionary<string, object> message)
    {
        var session = ResolveSession(message);
        if (session == null) return new { success = false, error = "Not authenticated" };

        var otpauthUri = Str(message, "otpauthUri");
        var hostname   = Str(message, "hostname");
        var issuer     = Str(message, "issuer");
        var account    = Str(message, "account");

        if (string.IsNullOrEmpty(otpauthUri))
            return new { success = false, error = "otpauthUri is required" };

        await EnsureCustomFieldSchemaAsync();

        const string totpFieldName = "TOTP Secret";
        var now = DateTime.UtcNow.ToString("o");

        // Find an existing login item by hostname
        PasswordItem? item = null;
        if (!string.IsNullOrEmpty(hostname))
        {
            item = await _dbContext!.PasswordItems
                .Include(p => p.LoginItem)
                .Where(p => !p.IsDeleted && p.UserId == session.Value.userId &&
                            p.LoginItem != null && p.LoginItem.WebsiteUrl != null &&
                            p.LoginItem.WebsiteUrl.Contains(hostname))
                .FirstOrDefaultAsync();
        }

        if (item != null)
        {
            // Upsert the TOTP custom field via raw SQL (avoids EF migration issues)
            var existingField = await _dbContext!.CustomFields
                .FirstOrDefaultAsync(f => f.PasswordItemId == item.Id &&
                    f.Name != null && f.Name.ToLower() == totpFieldName.ToLower());

            if (existingField != null)
            {
                existingField.Value = otpauthUri;
                existingField.LastModified = DateTime.UtcNow;
            }
            else
            {
                _dbContext.CustomFields.Add(new CustomField
                {
                    PasswordItemId = item.Id,
                    Name = totpFieldName,
                    Value = otpauthUri,
                    Type = 2, // CustomFieldType.Password
                    IsProtected = true,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                });
            }
            item.LastModified = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            return new { success = true, updated = true, itemId = item.Id, title = item.Title };
        }
        else
        {
            // Create a new login item for this TOTP entry
            var title = issuer ?? account ?? hostname ?? "TOTP Entry";
            var newItem = new PasswordItem
            {
                Title = title,
                UserId = session.Value.userId,
                Type = 0, // ItemType.Login
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
            };
            _dbContext!.PasswordItems.Add(newItem);
            await _dbContext.SaveChangesAsync();

            _dbContext.CustomFields.Add(new CustomField
            {
                PasswordItemId = newItem.Id,
                Name = totpFieldName,
                Value = otpauthUri,
                Type = 2,
                IsProtected = true,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
            });
            await _dbContext.SaveChangesAsync();
            return new { success = true, created = true, itemId = newItem.Id, title = newItem.Title };
        }
    }

    private static (string userId, byte[] masterKey)? ResolveSession(Dictionary<string, object> message)
    {
        if (message.TryGetValue("token", out var tokenObj) && tokenObj is JsonElement tokenEl)
        {
            var token = tokenEl.GetString();
            if (!string.IsNullOrEmpty(token) && _sessions.TryGetValue(token, out var session))
                return session;
        }
        return null;
    }

    private static string? Str(Dictionary<string, object> m, string key) =>
        m.TryGetValue(key, out var o) && o is JsonElement e && e.ValueKind == JsonValueKind.String ? e.GetString() : null;

    private static async Task<object> HandlePasskeyCreate(Dictionary<string, object> message)
    {
        var session = ResolveSession(message);
        if (session == null)
            return new { success = false, error = "Vault is locked. Sign in to the extension first." };

        var rpId = Str(message, "rpId");
        var userName = Str(message, "userName") ?? "";
        var userHandleB64 = Str(message, "userHandle") ?? "";
        if (string.IsNullOrEmpty(rpId))
            return new { success = false, error = "rpId is required" };

        try
        {
            await EnsurePasskeySchemaAsync();

            // 1. Generate the P-256 key pair.
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var ecParams = ecdsa.ExportParameters(includePrivateParameters: true);
            var pkcs8 = ecdsa.ExportPkcs8PrivateKey();

            // 2. Random credential id.
            var credentialId = RandomNumberGenerator.GetBytes(32);
            var credentialIdB64Url = Base64Url(credentialId);

            // 3. COSE EC2 public key.
            var cosePublicKey = EncodeCoseEc2PublicKey(ecParams.Q.X!, ecParams.Q.Y!);

            // 4. authenticatorData with attested credential data (AT flag set).
            const byte flags = 0x01 | 0x04 | 0x40; // UP | UV | AT
            var authData = BuildAuthenticatorData(rpId, flags, signCount: 0,
                attestedCredentialData: BuildAttestedCredentialData(credentialId, cosePublicKey));

            // 5. attestationObject with fmt = "none".
            var attestationObject = BuildNoneAttestationObject(authData);

            // 6. Encrypt the private key (+ metadata) under the master key and persist.
            var vaultPayload = JsonSerializer.Serialize(new
            {
                rpId,
                userHandle = userHandleB64,
                userName,
                privateKeyPkcs8 = Convert.ToBase64String(pkcs8)
            });
            var enc = EncryptWithKey(vaultPayload, session.Value.masterKey);

            var passkey = new UserPasskey
            {
                UserId = session.Value.userId,
                CredentialId = credentialIdB64Url,
                Name = $"{rpId}{(string.IsNullOrEmpty(userName) ? "" : " · " + userName)}",
                PublicKey = Convert.ToBase64String(cosePublicKey),
                SignatureCounter = 0,
                DeviceType = "Extension",
                IsBackedUp = true,
                RequiresUserVerification = true,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                StoreInVault = true,
                RpId = rpId,
                EncryptedVaultData = JsonSerializer.Serialize(enc)
            };
            _dbContext!.UserPasskeys.Add(passkey);
            await _dbContext.SaveChangesAsync();

            Array.Clear(pkcs8, 0, pkcs8.Length);

            return new
            {
                success = true,
                credentialId = credentialIdB64Url,
                attestationObject = Convert.ToBase64String(attestationObject),
                publicKeyCose = Convert.ToBase64String(cosePublicKey)
            };
        }
        catch (Exception ex)
        {
            return new { success = false, error = $"Passkey creation failed: {ex.Message}" };
        }
    }

    private static async Task<object> HandlePasskeyGet(Dictionary<string, object> message)
    {
        var session = ResolveSession(message);
        if (session == null)
            return new { success = false, error = "Vault is locked. Sign in to the extension first." };

        var rpId = Str(message, "rpId");
        var clientDataJsonB64 = Str(message, "clientDataJSON");
        if (string.IsNullOrEmpty(rpId) || string.IsNullOrEmpty(clientDataJsonB64))
            return new { success = false, error = "rpId and clientDataJSON are required" };

        // Optional list of allowed credential ids (base64url).
        var allowIds = new List<string>();
        if (message.TryGetValue("allowCredentialIds", out var allowObj) && allowObj is JsonElement allowEl &&
            allowEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in allowEl.EnumerateArray())
                if (item.ValueKind == JsonValueKind.String) allowIds.Add(item.GetString()!);
        }

        try
        {
            await EnsurePasskeySchemaAsync();

            // Find candidate passkeys for this user + RP.
            var candidates = await _dbContext!.UserPasskeys
                .Where(p => p.UserId == session.Value.userId && p.IsActive && p.RpId == rpId)
                .ToListAsync();

            UserPasskey? passkey = allowIds.Count > 0
                ? candidates.FirstOrDefault(p => allowIds.Contains(p.CredentialId))
                : candidates.OrderByDescending(p => p.LastUsedAt ?? p.CreatedAt).FirstOrDefault();

            if (passkey == null)
                return new { success = false, error = "No matching passkey for this site" };

            // Decrypt the private key.
            var enc = JsonSerializer.Deserialize<EncryptedPasswordData>(passkey.EncryptedVaultData!)!;
            var payloadJson = DecryptPasswordWithKey(enc, session.Value.masterKey);
            using var payload = JsonDocument.Parse(payloadJson);
            var pkcs8 = Convert.FromBase64String(payload.RootElement.GetProperty("privateKeyPkcs8").GetString()!);
            var userHandle = payload.RootElement.TryGetProperty("userHandle", out var uh) ? uh.GetString() ?? "" : "";

            using var ecdsa = ECDsa.Create();
            ecdsa.ImportPkcs8PrivateKey(pkcs8, out _);
            Array.Clear(pkcs8, 0, pkcs8.Length);

            // authenticatorData (no attested credential data on assertion).
            var newCount = passkey.SignatureCounter + 1;
            const byte flags = 0x01 | 0x04; // UP | UV
            var authData = BuildAuthenticatorData(rpId, flags, newCount, attestedCredentialData: null);

            // signature over authData || SHA-256(clientDataJSON), ES256 (ASN.1 DER).
            var clientDataHash = SHA256.HashData(Base64UrlDecode(clientDataJsonB64));
            var signedData = authData.Concat(clientDataHash).ToArray();
            var signature = ecdsa.SignData(signedData, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);

            // Update counter + last-used.
            passkey.SignatureCounter = newCount;
            passkey.LastUsedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            return new
            {
                success = true,
                credentialId = passkey.CredentialId,
                authenticatorData = Convert.ToBase64String(authData),
                signature = Convert.ToBase64String(signature),
                userHandle
            };
        }
        catch (Exception ex)
        {
            return new { success = false, error = $"Passkey authentication failed: {ex.Message}" };
        }
    }

    // --- WebAuthn binary builders ---------------------------------------------------

    private static byte[] BuildAuthenticatorData(string rpId, byte flags, uint signCount, byte[]? attestedCredentialData)
    {
        var rpIdHash = SHA256.HashData(Encoding.UTF8.GetBytes(rpId));
        var countBytes = new byte[4];
        countBytes[0] = (byte)(signCount >> 24);
        countBytes[1] = (byte)(signCount >> 16);
        countBytes[2] = (byte)(signCount >> 8);
        countBytes[3] = (byte)signCount;

        using var ms = new MemoryStream();
        ms.Write(rpIdHash, 0, rpIdHash.Length);
        ms.WriteByte(flags);
        ms.Write(countBytes, 0, 4);
        if (attestedCredentialData != null)
            ms.Write(attestedCredentialData, 0, attestedCredentialData.Length);
        return ms.ToArray();
    }

    private static byte[] BuildAttestedCredentialData(byte[] credentialId, byte[] cosePublicKey)
    {
        using var ms = new MemoryStream();
        ms.Write(Aaguid, 0, Aaguid.Length);
        ms.WriteByte((byte)(credentialId.Length >> 8));
        ms.WriteByte((byte)(credentialId.Length & 0xFF));
        ms.Write(credentialId, 0, credentialId.Length);
        ms.Write(cosePublicKey, 0, cosePublicKey.Length);
        return ms.ToArray();
    }

    /// <summary>CBOR: { "fmt": "none", "attStmt": {}, "authData": authData }.</summary>
    private static byte[] BuildNoneAttestationObject(byte[] authData)
    {
        using var ms = new MemoryStream();
        ms.WriteByte(0xA3); // map(3)
        CborWriteTextKey(ms, "fmt");
        CborWriteTextString(ms, "none");
        CborWriteTextKey(ms, "attStmt");
        ms.WriteByte(0xA0); // map(0)
        CborWriteTextKey(ms, "authData");
        CborWriteByteString(ms, authData);
        return ms.ToArray();
    }

    /// <summary>COSE_Key for an EC2 P-256 public key: {1:2, 3:-7, -1:1, -2:x, -3:y}.</summary>
    private static byte[] EncodeCoseEc2PublicKey(byte[] x, byte[] y)
    {
        x = LeftPad(x, 32);
        y = LeftPad(y, 32);
        using var ms = new MemoryStream();
        ms.WriteByte(0xA5); // map(5)
        ms.WriteByte(0x01); ms.WriteByte(0x02);                 // 1 (kty)  : 2 (EC2)
        ms.WriteByte(0x03); ms.WriteByte(0x26);                 // 3 (alg)  : -7 (ES256)
        ms.WriteByte(0x20); ms.WriteByte(0x01);                 // -1 (crv) : 1 (P-256)
        ms.WriteByte(0x21); CborWriteByteString(ms, x);         // -2 (x)
        ms.WriteByte(0x22); CborWriteByteString(ms, y);         // -3 (y)
        return ms.ToArray();
    }

    // --- minimal CBOR writers (definite-length, values < 24..65535) -----------------

    private static void CborWriteTextKey(Stream s, string text) => CborWriteTextString(s, text);

    private static void CborWriteTextString(Stream s, string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        CborWriteTypeAndLength(s, majorType: 3, length: bytes.Length);
        s.Write(bytes, 0, bytes.Length);
    }

    private static void CborWriteByteString(Stream s, byte[] bytes)
    {
        CborWriteTypeAndLength(s, majorType: 2, length: bytes.Length);
        s.Write(bytes, 0, bytes.Length);
    }

    private static void CborWriteTypeAndLength(Stream s, int majorType, int length)
    {
        var mt = (byte)(majorType << 5);
        if (length < 24)
        {
            s.WriteByte((byte)(mt | length));
        }
        else if (length < 256)
        {
            s.WriteByte((byte)(mt | 24));
            s.WriteByte((byte)length);
        }
        else
        {
            s.WriteByte((byte)(mt | 25));
            s.WriteByte((byte)(length >> 8));
            s.WriteByte((byte)(length & 0xFF));
        }
    }

    private static byte[] LeftPad(byte[] value, int size)
    {
        if (value.Length == size) return value;
        if (value.Length > size) return value.Skip(value.Length - size).ToArray();
        var padded = new byte[size];
        Array.Copy(value, 0, padded, size - value.Length, value.Length);
        return padded;
    }

    private static string Base64Url(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string input)
    {
        var s = input.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4) { case 2: s += "=="; break; case 3: s += "="; break; }
        return Convert.FromBase64String(s);
    }

    private static EncryptedPasswordData EncryptWithKey(string plaintext, byte[] masterKey)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(masterKey, 16);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);
        return new EncryptedPasswordData
        {
            EncryptedPassword = Convert.ToBase64String(ciphertext),
            Nonce = Convert.ToBase64String(nonce),
            AuthenticationTag = Convert.ToBase64String(tag)
        };
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

public class PasswordManagerDbContext : IdentityDbContext<ApplicationUser>
{
    public PasswordManagerDbContext(DbContextOptions<PasswordManagerDbContext> options) : base(options)
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