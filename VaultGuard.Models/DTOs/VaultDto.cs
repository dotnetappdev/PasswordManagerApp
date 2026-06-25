namespace PasswordManager.Models.DTOs;

public class VaultDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string UserId { get; set; } = string.Empty;
    public List<CategoryDto> Categories { get; set; } = new();
    public int PasswordItemsCount { get; set; } // Count of items in this vault
}

public class CreateVaultDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
}

public class UpdateVaultDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
}
