using System;
using System.ComponentModel.DataAnnotations;

namespace PasswordManager.Models
{
    /// <summary>
    /// Represents the result of an API test execution
    /// </summary>
    public class ApiTestResult
    {
        public int Id { get; set; }
        
        public int ApiRequestId { get; set; }
        public ApiRequest ApiRequest { get; set; } = null!;
        
        /// <summary>
        /// HTTP status code of the response
        /// </summary>
        public int StatusCode { get; set; }
        
        /// <summary>
        /// Status text (e.g., "OK", "Not Found", etc.)
        /// </summary>
        public string? StatusText { get; set; }
        
        /// <summary>
        /// Response body content
        /// </summary>
        public string? ResponseBody { get; set; }
        
        /// <summary>
        /// Response headers (JSON format)
        /// </summary>
        public string? ResponseHeaders { get; set; }
        
        /// <summary>
        /// Time taken for the request in milliseconds
        /// </summary>
        public long ResponseTimeMs { get; set; }
        
        /// <summary>
        /// Size of the response in bytes
        /// </summary>
        public long ResponseSizeBytes { get; set; }
        
        /// <summary>
        /// Error message if the request failed
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// Whether the request was successful
        /// </summary>
        public bool IsSuccess { get; set; }
        
        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
        
        // User relationship
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }
    }
}