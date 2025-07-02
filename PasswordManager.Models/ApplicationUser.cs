using Microsoft.AspNetCore.Identity;

namespace PasswordManager.Models;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation properties for user's data
    public virtual ICollection<PasswordItem> PasswordItems { get; set; } = new List<PasswordItem>();
    public virtual ICollection<Collection> Collections { get; set; } = new List<Collection>();
}
