namespace PasswordManager.Models.DTOs;

public class SecureNoteItemDto
{
    public int Id { get; set; }
    public int PasswordItemId { get; set; }
    public string? Content { get; set; }
    public bool IsMarkdown { get; set; }
    public bool IsRichText { get; set; }
    public string? AttachmentPaths { get; set; }
    public string? Category { get; set; }
    public string? TemplateType { get; set; }
    public bool IsHighSecurity { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsShared { get; set; }
    public string? SharedWith { get; set; }
    public int Version { get; set; }
    public string? LastEditedBy { get; set; }
    public DateTime? LastUsed { get; set; }
    public int UsageCount { get; set; }
}

public class CreateSecureNoteItemDto
{
    public string? Content { get; set; }
    public bool IsMarkdown { get; set; }
    public bool IsRichText { get; set; }
    public string? AttachmentPaths { get; set; }
    public string? Category { get; set; }
    public string? TemplateType { get; set; }
    public bool IsHighSecurity { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsShared { get; set; }
    public string? SharedWith { get; set; }
    public string? LastEditedBy { get; set; }
}

public class UpdateSecureNoteItemDto
{
    public string? Content { get; set; }
    public bool IsMarkdown { get; set; }
    public bool IsRichText { get; set; }
    public string? AttachmentPaths { get; set; }
    public string? Category { get; set; }
    public string? TemplateType { get; set; }
    public bool IsHighSecurity { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsShared { get; set; }
    public string? SharedWith { get; set; }
    public string? LastEditedBy { get; set; }
}
