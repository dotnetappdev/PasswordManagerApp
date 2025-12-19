using SQLite;

namespace PasswordManager.Uno.Services.LocalDatabase;

/// <summary>
/// Local SQLite representation of a Category
/// </summary>
[Table("Categories")]
public class LocalCategory
{
    [PrimaryKey, AutoIncrement]
    public int LocalId { get; set; }
    
    /// <summary>
    /// Server ID from API
    /// </summary>
    public int? ServerId { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public string? IconName { get; set; }
    
    public string? UserId { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime LastModified { get; set; }
    
    /// <summary>
    /// Sync status tracking
    /// </summary>
    public bool NeedsSyncToServer { get; set; }
    
    public DateTime? LastSyncedAt { get; set; }
}
