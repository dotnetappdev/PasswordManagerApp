namespace PasswordManager.API.DTOs;

/// <summary>
/// DTO for creating/updating password items with encryption
/// </summary>
public class CreatePasswordItemDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Models.ItemType Type { get; set; }
    public bool IsFavorite { get; set; }
    public bool IsArchived { get; set; }
    public int CategoryId { get; set; }
    public int CollectionId { get; set; }
    public CreateLoginItemDto? LoginItem { get; set; }
    public List<int> TagIds { get; set; } = new();
    
    // Master password for encryption (not stored)
    public string MasterPassword { get; set; } = string.Empty;
}

/// <summary>
/// DTO for creating/updating login items with plaintext data (will be encrypted)
/// </summary>
public class CreateLoginItemDto
{
    public string? Website { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? TotpSecret { get; set; }
    public string? TwoFactorType { get; set; }
    public string? SecurityQuestion1 { get; set; }
    public string? SecurityAnswer1 { get; set; }
    public string? SecurityQuestion2 { get; set; }
    public string? SecurityAnswer2 { get; set; }
    public string? SecurityQuestion3 { get; set; }
    public string? SecurityAnswer3 { get; set; }
    public string? RecoveryEmail { get; set; }
    public string? RecoveryPhone { get; set; }
    public string? LoginUrl { get; set; }
    public string? SupportUrl { get; set; }
    public string? AdminConsoleUrl { get; set; }
    public DateTime? PasswordLastChanged { get; set; }
    public bool RequiresPasswordChange { get; set; }
    public string? CompanyName { get; set; }
    public string? Department { get; set; }
    public string? JobTitle { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for password item responses with encrypted data
/// </summary>
public class PasswordItemResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Models.ItemType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastModified { get; set; }
    public bool IsFavorite { get; set; }
    public bool IsArchived { get; set; }
    public bool IsDeleted { get; set; }
    public string? UserId { get; set; }
    public int CategoryId { get; set; }
    public int CollectionId { get; set; }
    public EncryptedLoginItemDto? LoginItem { get; set; }
    public List<TagDto> Tags { get; set; } = new();
}

/// <summary>
/// DTO for encrypted login item data (as stored in database)
/// </summary>
public class EncryptedLoginItemDto
{
    public int Id { get; set; }
    public string? Website { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? TwoFactorType { get; set; }
    public string? SecurityQuestion1 { get; set; }
    public string? SecurityQuestion2 { get; set; }
    public string? SecurityQuestion3 { get; set; }
    public string? RecoveryEmail { get; set; }
    public string? RecoveryPhone { get; set; }
    public string? LoginUrl { get; set; }
    public string? SupportUrl { get; set; }
    public string? AdminConsoleUrl { get; set; }
    public DateTime? PasswordLastChanged { get; set; }
    public bool RequiresPasswordChange { get; set; }
    public DateTime? LastUsed { get; set; }
    public int UsageCount { get; set; }
    public string? CompanyName { get; set; }
    public string? Department { get; set; }
    public string? JobTitle { get; set; }
    
    // Encrypted fields are not included in responses by default for security
    // They can be decrypted on-demand using a separate endpoint
}

/// <summary>
/// DTO for decrypted password data (for UI display when user provides master password)
/// </summary>
public class DecryptedPasswordItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Models.ItemType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastModified { get; set; }
    public bool IsFavorite { get; set; }
    public bool IsArchived { get; set; }
    public bool IsDeleted { get; set; }
    public string? UserId { get; set; }
    public int CategoryId { get; set; }
    public int CollectionId { get; set; }
    public DecryptedLoginItemDto? LoginItem { get; set; }
    public List<TagDto> Tags { get; set; } = new();
}

/// <summary>
/// DTO for decrypted login item data (for UI display)
/// </summary>
public class DecryptedLoginItemDto
{
    public int Id { get; set; }
    public string? Website { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? TotpSecret { get; set; }
    public string? TwoFactorType { get; set; }
    public string? SecurityQuestion1 { get; set; }
    public string? SecurityAnswer1 { get; set; }
    public string? SecurityQuestion2 { get; set; }
    public string? SecurityAnswer2 { get; set; }
    public string? SecurityQuestion3 { get; set; }
    public string? SecurityAnswer3 { get; set; }
    public string? RecoveryEmail { get; set; }
    public string? RecoveryPhone { get; set; }
    public string? LoginUrl { get; set; }
    public string? SupportUrl { get; set; }
    public string? AdminConsoleUrl { get; set; }
    public DateTime? PasswordLastChanged { get; set; }
    public bool RequiresPasswordChange { get; set; }
    public DateTime? LastUsed { get; set; }
    public int UsageCount { get; set; }
    public string? CompanyName { get; set; }
    public string? Department { get; set; }
    public string? JobTitle { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for requesting password decryption
/// </summary>
public class DecryptPasswordRequestDto
{
    public int PasswordItemId { get; set; }
    public string MasterPassword { get; set; } = string.Empty;
}

/// <summary>
/// DTO for tag information
/// </summary>
public class TagDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}
