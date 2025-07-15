using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PasswordManager.Models
{
    public class ApiKey
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = "";
        
        [Required]
        [MaxLength(500)]
        public string KeyHash { get; set; } = "";
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? LastUsedAt { get; set; }
        
        [Required]
        public string UserId { get; set; } = "";
        
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;
        
        public bool IsActive { get; set; } = true;
    }
}
