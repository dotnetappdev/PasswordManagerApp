using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PasswordManager.Models
{
    /// <summary>
    /// Represents an API request for testing purposes
    /// </summary>
    public class ApiRequest
    {
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        public string Url { get; set; } = string.Empty;
        
        [Required]
        public HttpMethod Method { get; set; } = HttpMethod.GET;
        
        public string? Description { get; set; }
        
        /// <summary>
        /// JSON representation of headers (key-value pairs)
        /// </summary>
        public string? Headers { get; set; }
        
        /// <summary>
        /// JSON representation of query parameters (key-value pairs)
        /// </summary>
        public string? QueryParameters { get; set; }
        
        /// <summary>
        /// Request body content
        /// </summary>
        public string? Body { get; set; }
        
        /// <summary>
        /// Content type for the request body
        /// </summary>
        public string? ContentType { get; set; } = "application/json";
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // User relationship
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }
        
        // Collection relationship
        public int? ApiCollectionId { get; set; }
        public ApiCollection? ApiCollection { get; set; }
        
        // Test results
        public List<ApiTestResult> TestResults { get; set; } = new();
    }
    
    /// <summary>
    /// HTTP methods for API requests
    /// </summary>
    public enum HttpMethod
    {
        GET = 1,
        POST = 2,
        PUT = 3,
        DELETE = 4,
        PATCH = 5,
        HEAD = 6,
        OPTIONS = 7
    }
}