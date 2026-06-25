using PasswordManager.Models;
using PasswordManager.Models.DTOs.Audit;

namespace PasswordManager.Services.Interfaces;

public interface IAuditLogService
{
    /// <summary>
    /// Create an audit log entry
    /// </summary>
    Task<AuditLog> CreateAuditLogAsync(
        string userId, 
        string action, 
        string entityType, 
        string? entityId = null,
        string? entityName = null,
        string? changes = null,
        bool success = true,
        string? errorMessage = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? deviceId = null,
        string? deviceName = null);
    
    /// <summary>
    /// Get audit logs for a user with filtering
    /// </summary>
    Task<ListAuditLogsResponseDto> GetAuditLogsAsync(string userId, ListAuditLogsRequestDto request);
    
    /// <summary>
    /// Get audit logs for a specific entity
    /// </summary>
    Task<List<AuditLogDto>> GetEntityAuditLogsAsync(string userId, string entityType, string entityId);
    
    /// <summary>
    /// Delete old audit logs (for cleanup)
    /// </summary>
    Task DeleteOldAuditLogsAsync(int daysToKeep = 90);
}
