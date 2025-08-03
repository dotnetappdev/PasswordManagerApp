using PasswordManager.Models.Configuration;
using System.ComponentModel.DataAnnotations;

namespace PasswordManager.Models.DTOs
{
    public class ApiKeyRequestDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = "";
        
        /// <summary>
        /// Database provider this API key will be associated with
        /// </summary>
        public DatabaseProvider? Provider { get; set; }
        
        /// <summary>
        /// Whether to use existing database configuration for this provider
        /// </summary>
        public bool UseExistingConfig { get; set; } = true;
    }
    
    public class ApiKeyResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string ApiKey { get; set; } = "";
        public DatabaseProvider? Provider { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ProviderDisplayName { get; set; }
        public string? ApiUrl { get; set; }
    }
}