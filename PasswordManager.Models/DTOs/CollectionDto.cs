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
    public int? ParentCollectionId { get; set; }
    public CollectionDto? ParentCollection { get; set; }
    public List<CollectionDto> Children { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();
}

public class CreateCollectionDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public bool IsDefault { get; set; }
    public int? ParentCollectionId { get; set; }
}

public class UpdateCollectionDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public bool IsDefault { get; set; }
    public int? ParentCollectionId { get; set; }
}
