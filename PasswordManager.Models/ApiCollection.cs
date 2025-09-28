using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PasswordManager.Models
{
    /// <summary>
    /// Represents a collection of API requests for organization
    /// </summary>
    public class ApiCollection
    {
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        /// <summary>
        /// Color for UI display (hex color code)
        /// </summary>
        public string? Color { get; set; } = "#007bff";
        
        /// <summary>
        /// Icon for UI display (emoji or icon name)
        /// </summary>
        public string? Icon { get; set; } = "📁";
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // User relationship
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }
        
        // Parent-child relationship for nested collections
        public int? ParentApiCollectionId { get; set; }
        public ApiCollection? ParentApiCollection { get; set; }
        public List<ApiCollection> Children { get; set; } = new();
        
        // API requests in this collection
        public List<ApiRequest> ApiRequests { get; set; } = new();
        
        /// <summary>
        /// Base URL for all requests in this collection (optional)
        /// </summary>
        public string? BaseUrl { get; set; }
        
        /// <summary>
        /// Default headers for all requests in this collection (JSON format)
        /// </summary>
        public string? DefaultHeaders { get; set; }
    }
}