using FileHelpers;

namespace PasswordManagerImports.OnePassword.Models;

// Old 1Password CSV export format (pre-2023):
// Title,Url,Username,Password,OTPAuth,Favorite,Archived,Tags,Notes
[DelimitedRecord(",")]
[IgnoreFirst(1)] // Skip header row
public class OnePasswordCsvRecord
{
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

// New 1Password CSV export format (2023+):
// Title,URL,Username,Password,Notes,Type
[DelimitedRecord(",")]
[IgnoreFirst(1)] // Skip header row
public class OnePasswordCsvRecordNew
{
    [FieldQuoted('"', QuoteMode.OptionalForBoth)]
    public string Title { get; set; } = string.Empty;

    [FieldQuoted('"', QuoteMode.OptionalForBoth)]
    public string URL { get; set; } = string.Empty;

    [FieldQuoted('"', QuoteMode.OptionalForBoth)]
    public string Username { get; set; } = string.Empty;

    [FieldQuoted('"', QuoteMode.OptionalForBoth)]
    public string Password { get; set; } = string.Empty;

    [FieldQuoted('"', QuoteMode.OptionalForBoth)]
    [FieldOptional]
    public string Notes { get; set; } = string.Empty;

    [FieldQuoted('"', QuoteMode.OptionalForBoth)]
    [FieldOptional]
    public string Type { get; set; } = string.Empty;
}
