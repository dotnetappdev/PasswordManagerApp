using FileHelpers;

namespace PasswordManagerImports.OnePassword.Models;

[DelimitedRecord(",")]
[IgnoreFirst(1)] // Skip header row
public class OnePasswordCsvRecord
{
    // Following the exact order from 1Password export format:
    // Title,Url,Username,Password,OTPAuth,Favorite,Archived,Tags,Notes
    
    [FieldQuoted('"', QuoteMode.OptionalForBoth)]
    public string Title { get; set; } = string.Empty;

    [FieldQuoted('"', QuoteMode.OptionalForBoth)]
    public string Url { get; set; } = string.Empty;

    [FieldQuoted('"', QuoteMode.OptionalForBoth)]
    public string Username { get; set; } = string.Empty;

    [FieldQuoted('"', QuoteMode.OptionalForBoth)]
    public string Password { get; set; } = string.Empty;

    [FieldQuoted('"', QuoteMode.OptionalForBoth)]
    [FieldOptional]
    public string OTPAuth { get; set; } = string.Empty;

    [FieldQuoted('"', QuoteMode.OptionalForBoth)]
    [FieldOptional]
    public string Favorite { get; set; } = string.Empty;

    [FieldQuoted('"', QuoteMode.OptionalForBoth)]
    [FieldOptional]
    public string Archived { get; set; } = string.Empty;

    [FieldQuoted('"', QuoteMode.OptionalForBoth)]
    [FieldOptional]
    public string Tags { get; set; } = string.Empty;

    [FieldQuoted('"', QuoteMode.OptionalForBoth)]
    [FieldOptional]
    public string Notes { get; set; } = string.Empty;
}
