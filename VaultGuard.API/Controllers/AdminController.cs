using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VaultGuard.DAL;
using VaultGuard.Models;
using VaultGuard.Models.Authorization;
using VaultGuard.Models.Licensing;

namespace VaultGuard.API.Controllers;

/// <summary>
/// Backs the VaultGuard.Admin control panel: "who am I / am I a super admin" identity check, role
/// assignment, and the dashboard's summary counters. User CRUD itself lives in
/// <see cref="UserProfileController"/> (now shared by both the in-app Admin role and SuperAdmin).
/// </summary>
[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly VaultGuardDbContext _dbContext;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        VaultGuardDbContext dbContext,
        ILogger<AdminController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>Any authenticated user can call this — it's how VaultGuard.Admin decides, right after
    /// login, whether the account is actually allowed into the control panel (must have SuperAdmin).</summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<AdminWhoAmIResponse>> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return Unauthorized();

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new AdminWhoAmIResponse
        {
            Id = user.Id,
            Email = user.Email ?? "",
            DisplayName = string.Join(' ', new[] { user.FirstName, user.LastName }.Where(s => !string.IsNullOrWhiteSpace(s))),
            Roles = roles.ToList(),
            IsSuperAdmin = roles.Contains(ApplicationRoles.SuperAdmin)
        });
    }

    [RequireSuperAdmin]
    [HttpGet("roles")]
    public ActionResult<List<string>> GetRoles() => Ok(ApplicationRoles.AllRoles.ToList());

    [RequireSuperAdmin]
    [HttpGet("users/{id}/roles")]
    public async Task<ActionResult<List<string>>> GetUserRoles(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        return Ok((await _userManager.GetRolesAsync(user)).ToList());
    }

    [RequireSuperAdmin]
    [HttpPost("users/{id}/roles/{role}")]
    public async Task<ActionResult> AddUserToRole(string id, string role)
    {
        if (!ApplicationRoles.AllRoles.Contains(role)) return BadRequest("Unknown role.");

        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        if (await _userManager.IsInRoleAsync(user, role)) return NoContent();

        var result = await _userManager.AddToRoleAsync(user, role);
        if (!result.Succeeded)
            return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });

        _logger.LogInformation("SuperAdmin {Admin} granted role {Role} to user {User}",
            User.FindFirstValue(ClaimTypes.NameIdentifier), role, id);
        return NoContent();
    }

    [RequireSuperAdmin]
    [HttpDelete("users/{id}/roles/{role}")]
    public async Task<ActionResult> RemoveUserFromRole(string id, string role)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        if (!await _userManager.IsInRoleAsync(user, role)) return NoContent();

        var result = await _userManager.RemoveFromRoleAsync(user, role);
        if (!result.Succeeded)
            return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });

        _logger.LogInformation("SuperAdmin {Admin} revoked role {Role} from user {User}",
            User.FindFirstValue(ClaimTypes.NameIdentifier), role, id);
        return NoContent();
    }

    /// <summary>Assigns (or clears, with tenantId = null) which tenant a user belongs to.</summary>
    [RequireSuperAdmin]
    [HttpPut("users/{id}/tenant")]
    public async Task<ActionResult> SetUserTenant(string id, [FromBody] SetUserTenantRequest request)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();

        if (request.TenantId.HasValue && !await _dbContext.Tenants.AnyAsync(t => t.Id == request.TenantId.Value))
            return BadRequest("Tenant not found.");

        user.TenantId = request.TenantId;
        user.LastModified = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Cross-user API key oversight for the VaultGuard.Admin dashboard — the per-user
    /// <see cref="ApiKeysController"/> only ever sees the calling user's own keys.</summary>
    [RequireSuperAdmin]
    [HttpGet("api-keys")]
    public async Task<ActionResult<List<AdminApiKeyResponse>>> GetAllApiKeys()
    {
        var keys = await _dbContext.ApiKeys
            .Include(k => k.User)
            .OrderByDescending(k => k.CreatedAt)
            .Select(k => new AdminApiKeyResponse
            {
                Id = k.Id,
                Name = k.Name,
                UserId = k.UserId,
                UserEmail = k.User.Email ?? "",
                CreatedAt = k.CreatedAt,
                LastUsedAt = k.LastUsedAt,
                IsActive = k.IsActive
            })
            .ToListAsync();
        return Ok(keys);
    }

    [RequireSuperAdmin]
    [HttpGet("dashboard-stats")]
    public async Task<ActionResult<AdminDashboardStats>> GetDashboardStats()
    {
        var now = DateTime.UtcNow;
        var stats = new AdminDashboardStats
        {
            TotalUsers = await _dbContext.Users.CountAsync(),
            ActiveUsers = await _dbContext.Users.CountAsync(u => u.IsActive),
            TotalTenants = await _dbContext.Tenants.CountAsync(),
            ActiveTenants = await _dbContext.Tenants.CountAsync(t => t.Status == Models.Tenancy.TenantStatus.Active),
            TotalLicenseKeys = await _dbContext.LicenseKeys.CountAsync(),
            ActiveLicenseKeys = await _dbContext.LicenseKeys.CountAsync(k => !k.IsRevoked && (k.ExpiresAt == null || k.ExpiresAt > now)),
            TotalActivations = await _dbContext.LicenseActivations.CountAsync(a => a.IsActive),
            ActiveSubscriptions = await _dbContext.Subscriptions.CountAsync(s =>
                s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trialing),
        };
        return Ok(stats);
    }
}

public class AdminWhoAmIResponse
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public List<string> Roles { get; set; } = new();
    public bool IsSuperAdmin { get; set; }
}

public class SetUserTenantRequest
{
    public Guid? TenantId { get; set; }
}

public class AdminApiKeyResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string UserId { get; set; } = "";
    public string UserEmail { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public bool IsActive { get; set; }
}

public class AdminDashboardStats
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalTenants { get; set; }
    public int ActiveTenants { get; set; }
    public int TotalLicenseKeys { get; set; }
    public int ActiveLicenseKeys { get; set; }
    public int TotalActivations { get; set; }
    public int ActiveSubscriptions { get; set; }
}
