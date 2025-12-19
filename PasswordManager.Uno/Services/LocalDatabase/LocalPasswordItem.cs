using SQLite;

namespace PasswordManager.Uno.Services.LocalDatabase;

/// <summary>
/// Local SQLite representation of a Password Item
/// </summary>
[Table("PasswordItems")]
public class LocalPasswordItem
{
    [PrimaryKey, AutoIncrement]
    public int LocalId { get; set; }
    
    /// <summary>
    /// Server ID from API
    /// </summary>
    public int? ServerId { get; set; }
    
    public string Title { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public string Type { get; set; } = "Login";
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime LastModified { get; set; }
    
    public bool IsFavorite { get; set; }
    
    public bool IsArchived { get; set; }
    
    public bool IsDeleted { get; set; }
    
    public string? UserId { get; set; }
    
    public int? CategoryId { get; set; }
    
    public int? CollectionId { get; set; }
    
    public string? Website { get; set; }
    
    // Login specific fields (serialized if needed)
    public string? Username { get; set; }
    
    public string? EncryptedPassword { get; set; }
    
    public string? Notes { get; set; }
    
    public DateTime? LastAccessedAt { get; set; }
    
    /// <summary>
    /// Sync status tracking
    /// </summary>
    public bool NeedsSyncToServer { get; set; }
    
    public DateTime? LastSyncedAt { get; set; }
}
