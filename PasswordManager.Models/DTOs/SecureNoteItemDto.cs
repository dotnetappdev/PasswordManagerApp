namespace PasswordManager.Models.DTOs;

public class SecureNoteItemDto
{
    public int Id { get; set; }
    public string? Content { get; set; }
    public int PasswordItemId { get; set; }
}

public class CreateSecureNoteItemDto
{
    public string? Content { get; set; }
}

public class UpdateSecureNoteItemDto
{
    public string? Content { get; set; }
}
