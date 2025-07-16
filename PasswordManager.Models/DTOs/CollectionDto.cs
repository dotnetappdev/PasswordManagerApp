namespace PasswordManager.Models.DTOs;

public class CollectionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsDefault { get; set; }
    public string? UserId { get; set; }
    public int? ParentId { get; set; } // For mapping
    public CollectionDto? Parent { get; set; } // For mapping
    public List<CollectionDto> Children { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();
    public DateTime LastModified { get; set; } // Added for mapping
}

public class CreateCollectionDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public bool IsDefault { get; set; }
    public int? ParentId { get; set; } // For mapping
    public DateTime LastModified { get; set; }
}

public class UpdateCollectionDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public bool IsDefault { get; set; }
    public int? ParentId { get; set; } // For mapping
    public DateTime LastModified { get; set; }
}
