using System.Text;
using VaultGuard.Imports.Interfaces;
using VaultGuard.Models;

namespace VaultGuard.Imports.Services;

/// <summary>Builds the end-of-import report: what carried over, what the file didn't have, and what failed.</summary>
public static class ImportReport
{
    public static string FriendlyType(ItemType type) => type switch
    {
        ItemType.Login => "Login",
        ItemType.CreditCard => "Credit card",
        ItemType.SecureNote => "Secure note",
        ItemType.WiFi => "Wi-Fi network",
        ItemType.Passkey => "Passkey",
        ItemType.Password => "Password",
        _ => type.ToString()
    };

    /// <summary>The usual fields of this item's type that are empty. Call before the item is encrypted.</summary>
    public static List<string> MissingFields(PasswordItem item)
    {
        var missing = new List<string>();
        void Need(string? value, string name) { if (string.IsNullOrWhiteSpace(value)) missing.Add(name); }

        if (item.CreditCardItem is { } cc)
        {
            Need(cc.CardNumber, "card number");
            Need(cc.CardholderName, "cardholder name");
            Need(cc.ExpiryDate, "expiry date");
        }
        else if (item.WiFiItem is { } wifi)
        {
            Need(wifi.NetworkName, "network name");
            Need(wifi.Password, "password");
        }
        else if (item.SecureNoteItem is { } note)
        {
            Need(note.Content == "(empty)" ? null : note.Content, "note text");
        }
        else if (item.PasskeyItem is not null)
        {
            // Passkeys can't be exported by other managers with their private key; nothing to check here.
        }
        else if (item.LoginItem is { } login)
        {
            if (item.Type == ItemType.Password)
            {
                // A stand-alone password has no username or website by design.
                Need(login.Password, "password");
            }
            else if (string.IsNullOrWhiteSpace(login.Username) && string.IsNullOrWhiteSpace(login.Password) &&
                     string.IsNullOrWhiteSpace(login.WebsiteUrl ?? login.Website ?? item.Website) &&
                     item.CustomFields?.Any(f => !string.IsNullOrWhiteSpace(f.Value)) == true)
            {
                // Types we don't have (identities, documents, licences…) are saved as a login whose details are
                // all custom fields; they aren't missing anything.
            }
            else
            {
                Need(login.Username, "username");
                Need(login.Password, "password");
                Need(login.WebsiteUrl ?? login.Website ?? item.Website, "website");
            }
        }
        return missing;
    }

    public static ImportReportEntry Imported(PasswordItem item, List<string> missing) => new()
    {
        Title = string.IsNullOrWhiteSpace(item.Title) ? "Untitled item" : item.Title,
        ItemType = FriendlyType(item.Type),
        Outcome = missing.Count == 0 ? ImportItemOutcome.Imported : ImportItemOutcome.ImportedWithGaps,
        MissingFields = missing
    };

    public static ImportReportEntry Failed(PasswordItem item, string error) => new()
    {
        Title = string.IsNullOrWhiteSpace(item.Title) ? "Untitled item" : item.Title,
        ItemType = FriendlyType(item.Type),
        Outcome = ImportItemOutcome.Failed,
        Error = error
    };

    /// <summary>A plain-text copy of the report, for saving or pasting into a support request.</summary>
    public static string ToText(ImportResult result, string? fileName = null, string? source = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Vault Guard import report");
        sb.AppendLine($"Date: {DateTime.Now:f}");
        if (!string.IsNullOrEmpty(source)) sb.AppendLine($"From: {source}");
        if (!string.IsNullOrEmpty(fileName)) sb.AppendLine($"File: {fileName}");
        sb.AppendLine();

        if (!result.Success)
        {
            sb.AppendLine("The file couldn't be imported.");
            sb.AppendLine(result.ErrorMessage ?? "No reason was given.");
            return sb.ToString();
        }

        var ok = result.Report.Count(r => r.Outcome == ImportItemOutcome.Imported);
        var gaps = result.Report.Count(r => r.Outcome == ImportItemOutcome.ImportedWithGaps);
        var failed = result.Report.Count(r => r.Outcome == ImportItemOutcome.Failed);
        sb.AppendLine($"Imported: {ok + gaps} of {result.Report.Count} item(s)");
        sb.AppendLine($"  Complete: {ok}");
        sb.AppendLine($"  Missing some fields in the file: {gaps}");
        sb.AppendLine($"Not imported: {failed}");

        void Section(string heading, ImportItemOutcome outcome)
        {
            var rows = result.Report.Where(r => r.Outcome == outcome)
                .OrderBy(r => r.Title, StringComparer.CurrentCultureIgnoreCase).ToList();
            if (rows.Count == 0) return;
            sb.AppendLine();
            sb.AppendLine(heading);
            foreach (var r in rows)
                sb.AppendLine(outcome switch
                {
                    ImportItemOutcome.Failed => $"  - {r.Title} ({r.ItemType}): {r.Error}",
                    ImportItemOutcome.ImportedWithGaps => $"  - {r.Title} ({r.ItemType}): no {string.Join(", ", r.MissingFields)} in the file",
                    _ => $"  - {r.Title} ({r.ItemType})"
                });
        }
        Section("Not imported", ImportItemOutcome.Failed);
        Section("Imported, but the file had no value for", ImportItemOutcome.ImportedWithGaps);
        Section("Imported", ImportItemOutcome.Imported);

        var notes = result.Warnings.Where(w => !w.StartsWith("Failed to import '", StringComparison.Ordinal)).ToList();
        if (notes.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Notes from reading the file");
            foreach (var n in notes) sb.AppendLine($"  - {n}");
        }
        return sb.ToString();
    }
}
