using PasswordManager.Models;

namespace PasswordManager.Models.DTOs;

public class CustomFieldDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public CustomFieldType Type { get; set; } = CustomFieldType.Text;
    public bool IsRequired { get; set; } = false;
    public bool IsProtected { get; set; } = false;
    public int DisplayOrder { get; set; } = 0;
    public int PasswordItemId { get; set; }
}

public class CreateCustomFieldDto
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public CustomFieldType Type { get; set; } = CustomFieldType.Text;
    public bool IsRequired { get; set; } = false;
    public bool IsProtected { get; set; } = false;
    public int DisplayOrder { get; set; } = 0;
}

public class UpdateCustomFieldDto
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public CustomFieldType Type { get; set; } = CustomFieldType.Text;
    public bool IsRequired { get; set; } = false;
    public bool IsProtected { get; set; } = false;
    public int DisplayOrder { get; set; } = 0;
}