using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PasswordManager.DAL;
using PasswordManager.Models;
using PasswordManager.Models.DTOs.Audit;
using PasswordManager.Services.Interfaces;
using System.Text.Json;

namespace PasswordManager.Services.Services;

public class AuditLogService : IAuditLogService
{
    private readonly PasswordManagerDbContext _dbContext;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(
        PasswordManagerDbContext dbContext,
        ILogger<AuditLogService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<AuditLog> CreateAuditLogAsync(
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
        string? deviceName = null)
    {
        try
        {
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                EntityName = entityName,
                Changes = changes,
                Timestamp = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                DeviceId = deviceId,
                DeviceName = deviceName,
                Success = success,
                ErrorMessage = errorMessage
            };

            _dbContext.AuditLogs.Add(auditLog);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Audit log created: {Action} on {EntityType} by user {UserId}", 
                action, entityType, userId);

            return auditLog;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating audit log for user {UserId}", userId);
            throw;
        }
    }

    public async Task<ListAuditLogsResponseDto> GetAuditLogsAsync(
        string userId, 
        ListAuditLogsRequestDto request)
    {
        try
        {
            var query = _dbContext.AuditLogs
                .Where(a => a.UserId == userId);

            // Apply filters
            if (!string.IsNullOrEmpty(request.EntityType))
            {
                query = query.Where(a => a.EntityType == request.EntityType);
            }

            if (!string.IsNullOrEmpty(request.EntityId))
            {
                query = query.Where(a => a.EntityId == request.EntityId);
            }

            if (!string.IsNullOrEmpty(request.Action))
            {
                query = query.Where(a => a.Action == request.Action);
            }

            if (request.StartDate.HasValue)
            {
                query = query.Where(a => a.Timestamp >= request.StartDate.Value);
            }

            if (request.EndDate.HasValue)
            {
                query = query.Where(a => a.Timestamp <= request.EndDate.Value);
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var logs = await query
                .OrderByDescending(a => a.Timestamp)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new ListAuditLogsResponseDto
            {
                Logs = logs.Select(MapToDto).ToList(),
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit logs for user {UserId}", userId);
            return new ListAuditLogsResponseDto
            {
                Logs = new List<AuditLogDto>(),
                TotalCount = 0,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }
    }

    public async Task<List<AuditLogDto>> GetEntityAuditLogsAsync(
        string userId, 
        string entityType, 
        string entityId)
    {
        try
        {
            var logs = await _dbContext.AuditLogs
                .Where(a => a.UserId == userId && 
                           a.EntityType == entityType && 
                           a.EntityId == entityId)
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();

            return logs.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting entity audit logs for {EntityType}:{EntityId}", 
                entityType, entityId);
            return new List<AuditLogDto>();
        }
    }

    public async Task DeleteOldAuditLogsAsync(int daysToKeep = 90)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
            
            var oldLogs = await _dbContext.AuditLogs
                .Where(a => a.Timestamp < cutoffDate)
                .ToListAsync();

            _dbContext.AuditLogs.RemoveRange(oldLogs);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Deleted {Count} old audit logs older than {Date}", 
                oldLogs.Count, cutoffDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting old audit logs");
        }
    }

    private static AuditLogDto MapToDto(AuditLog log)
    {
        return new AuditLogDto
        {
            Id = log.Id,
            UserId = log.UserId,
            Action = log.Action,
            EntityType = log.EntityType,
            EntityId = log.EntityId,
            EntityName = log.EntityName,
            Changes = log.Changes,
            Timestamp = log.Timestamp,
            IpAddress = log.IpAddress,
            UserAgent = log.UserAgent,
            DeviceId = log.DeviceId,
            DeviceName = log.DeviceName,
            Success = log.Success,
            ErrorMessage = log.ErrorMessage
        };
    }
}
