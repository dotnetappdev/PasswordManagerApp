using FileHelpers;

namespace PasswordManagerImports.OnePassword.Models;

[DelimitedRecord(",")]
[IgnoreFirst(1)] // Skip header row
public class OnePasswordCsvRecord
{
    [FieldQuoted('"', QuoteMode.OptionalForBoth)]
    public string Title { get; set; } = string.Empty;

    [FieldQuoted('"', QuoteMode.OptionalForBoth)]
    public string Website { get; set; } = string.Empty;

    [FieldQuoted('"', QuoteMode.OptionalForBoth)]
    public string Username { get; set; } = string.Empty;

    [FieldQuoted('"', QuoteMode.OptionalForBoth)]
    public string Password { get; set; } = string.Empty;

    [FieldQuoted('"', QuoteMode.OptionalForBoth)]
    public string Notes { get; set; } = string.Empty;
}
