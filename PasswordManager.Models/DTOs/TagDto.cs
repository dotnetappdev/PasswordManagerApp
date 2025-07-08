namespace PasswordManager.Models.DTOs;

public class TagDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? Description { get; set; }
    public bool IsSystemTag { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateTagDto
{
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? Description { get; set; }
    public bool IsSystemTag { get; set; }
}

public class UpdateTagDto
{
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? Description { get; set; }
    public bool IsSystemTag { get; set; }
}
