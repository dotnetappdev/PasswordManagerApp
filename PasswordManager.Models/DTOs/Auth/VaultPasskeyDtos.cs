namespace PasswordManager.Models.DTOs.Auth;

/// <summary>
/// Request to create (register) a software passkey for a third-party site, stored encrypted
/// in the user's vault. The master password is required because the private key is encrypted
/// under the master-key (zero-knowledge) and never persisted in the clear.
/// </summary>
public class VaultPasskeyCreateRequestDto
{
    public string MasterPassword { get; set; } = string.Empty;
    public string RpId { get; set; } = string.Empty;
    public string RpName { get; set; } = string.Empty;
    public string UserHandle { get; set; } = string.Empty;   // base64url
    public string UserName { get; set; } = string.Empty;
    public string UserDisplayName { get; set; } = string.Empty;
}

public class VaultPasskeyCreateResponseDto
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string CredentialId { get; set; } = string.Empty;     // base64url
    public string AttestationObject { get; set; } = string.Empty; // base64 (standard)
    public string PublicKeyCose { get; set; } = string.Empty;     // base64 (standard)
}

/// <summary>
/// Request to produce an assertion (sign a challenge) using a stored vault passkey.
/// </summary>
public class VaultPasskeyAssertRequestDto
{
    public string MasterPassword { get; set; } = string.Empty;
    public string RpId { get; set; } = string.Empty;
    public string ClientDataJSON { get; set; } = string.Empty;          // base64url of the bytes the page will send the RP
    public List<string> AllowCredentialIds { get; set; } = new();        // base64url credential ids (may be empty)
}

public class VaultPasskeyAssertResponseDto
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string CredentialId { get; set; } = string.Empty;       // base64url
    public string AuthenticatorData { get; set; } = string.Empty;  // base64 (standard)
    public string Signature { get; set; } = string.Empty;          // base64 (standard)
    public string UserHandle { get; set; } = string.Empty;         // base64url
}
