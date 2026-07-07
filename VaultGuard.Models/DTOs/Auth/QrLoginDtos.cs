using System.ComponentModel.DataAnnotations;

namespace VaultGuard.Models.DTOs.Auth;

public class QrLoginGenerateRequestDto
{
    // No required fields - uses current authenticated user session
}

public class QrLoginGenerateResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string QrCodeData { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public int ExpiresInSeconds { get; set; }
}

public class QrLoginAuthenticateRequestDto
{
    [Required]
    public string Token { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string Password { get; set; } = string.Empty;
    
    // Device information for linking
    [MaxLength(200)]
    public string? DeviceName { get; set; }
    
    [MaxLength(50)]
    public string? DeviceType { get; set; }
    
    [MaxLength(100)]
    public string? Platform { get; set; }
}

public class QrLoginAuthenticateResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public AuthResponseDto? AuthData { get; set; }
    public string? DeviceId { get; set; }
    public string? DeviceName { get; set; }
}

public class QrLoginStatusResponseDto
{
    public string Token { get; set; } = string.Empty;
    public QrLoginStatus Status { get; set; }
    public bool IsExpired { get; set; }
    public AuthResponseDto? AuthData { get; set; }
    public string Message { get; set; } = string.Empty;
}

public enum QrLoginStatus
{
    Pending,
    Authenticated,
    Expired,
    Used
}

/// <summary>
/// Sent by the scanning device (phone) to relay an end-to-end encrypted master-key hand-off to the
/// device that displayed the QR. The server stores the ciphertext and never decrypts it.
/// </summary>
public class QrHandoffSubmitRequestDto
{
    [Required]
    public string Token { get; set; } = string.Empty;

    /// <summary>Scanning device's ephemeral public key (base64 SubjectPublicKeyInfo).</summary>
    [Required]
    public string EphemeralPublicKey { get; set; } = string.Empty;

    /// <summary>AES-GCM nonce (base64).</summary>
    [Required]
    public string Nonce { get; set; } = string.Empty;

    /// <summary>AES-GCM ciphertext + tag (base64) of the master password.</summary>
    [Required]
    public string Ciphertext { get; set; } = string.Empty;

    /// <summary>Account email so the displaying device knows which local profile to unlock.</summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Polled by the displaying device. Once a phone has submitted, carries the encrypted blob for the
/// displaying device to decrypt with its private key and unlock its local vault.
/// </summary>
public class QrHandoffStatusResponseDto
{
    public string Token { get; set; } = string.Empty;
    public QrLoginStatus Status { get; set; }
    public bool IsExpired { get; set; }
    public string? EphemeralPublicKey { get; set; }
    public string? Nonce { get; set; }
    public string? Ciphertext { get; set; }
    public string? Email { get; set; }
    public string Message { get; set; } = string.Empty;
}