using System.Text.Json.Serialization;
using System.Text.Json;

namespace PasswordManagerImports.OnePassword.Models;

// Root structure for 1PUX export.data JSON
public class OnePasswordPuxExport
{
    [JsonPropertyName("accounts")]
    public List<PuxAccount> Accounts { get; set; } = new();
}

public class PuxAccount
{
    [JsonPropertyName("attrs")]
    public PuxAccountAttributes Attrs { get; set; } = new();

    [JsonPropertyName("vaults")]
    public List<PuxVault> Vaults { get; set; } = new();
}

public class PuxAccountAttributes
{
    [JsonPropertyName("accountName")]
    public string AccountName { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("uuid")]
    public string Uuid { get; set; } = string.Empty;

    [JsonPropertyName("domain")]
    public string Domain { get; set; } = string.Empty;
}

public class PuxVault
{
    [JsonPropertyName("attrs")]
    public PuxVaultAttributes Attrs { get; set; } = new();

    [JsonPropertyName("items")]
    public List<PuxItem> Items { get; set; } = new();
}

public class PuxVaultAttributes
{
    [JsonPropertyName("uuid")]
    public string Uuid { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("desc")]
    public string Desc { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}

public class PuxItem
{
    [JsonPropertyName("uuid")]
    public string Uuid { get; set; } = string.Empty;

    [JsonPropertyName("favIndex")]
    public int FavIndex { get; set; }

    [JsonPropertyName("createdAt")]
    public long CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public long UpdatedAt { get; set; }

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("categoryUuid")]
    public string CategoryUuid { get; set; } = string.Empty;

    [JsonPropertyName("overview")]
    public PuxOverview Overview { get; set; } = new();

    [JsonPropertyName("details")]
    public PuxDetails Details { get; set; } = new();
}

public class PuxOverview
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("subtitle")]
    public string Subtitle { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("urls")]
    public List<PuxUrlObject> Urls { get; set; } = new();

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();
}

public class PuxUrlObject
{
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}

public class PuxDetails
{
    [JsonPropertyName("loginFields")]
    public List<PuxLoginField> LoginFields { get; set; } = new();

    [JsonPropertyName("notesPlain")]
    public string NotesPlain { get; set; } = string.Empty;

    [JsonPropertyName("sections")]
    public List<PuxSection> Sections { get; set; } = new();

    [JsonPropertyName("passwordHistory")]
    public List<PuxPasswordHistory> PasswordHistory { get; set; } = new();
}

public class PuxLoginField
{
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("fieldType")]
    public string FieldType { get; set; } = string.Empty;

    [JsonPropertyName("designation")]
    public string Designation { get; set; } = string.Empty;
}

public class PuxSection
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("fields")]
    public List<PuxField> Fields { get; set; } = new();
}

public class PuxField
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    [JsonConverter(typeof(PuxFieldValueConverter))]
    public PuxFieldValue Value { get; set; } = new();
}

[JsonConverter(typeof(PuxFieldValueConverter))]
public class PuxFieldValue
{
    [JsonPropertyName("string")]
    public string String { get; set; } = string.Empty;

    [JsonPropertyName("concealed")]
    public string Concealed { get; set; } = string.Empty;

    // email is an object: {email_address: string, provider: string|null}
    // We flatten it to just the email_address string during parsing
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("date")]
    public long Date { get; set; }

    // monthYear is stored as an integer YYYYMM, e.g. 202612 = December 2026
    public int MonthYear { get; set; }

    // creditCardType stores the network code, e.g. "mc", "visa", "amex"
    public string CreditCardType { get; set; } = string.Empty;

    // creditCardNumber — card number stored directly (alternative to concealed for card number fields)
    public string CreditCardNumber { get; set; } = string.Empty;

    // menu type (e.g. card type selected from a dropdown) — treat same as string
    public string Menu { get; set; } = string.Empty;

    // reference — pointer to another item
    public string Reference { get; set; } = string.Empty;

    // address type — complex object; we skip it during parsing but record key sub-fields
    public string Address { get; set; } = string.Empty;

    // totp / otp — the TOTP URI or secret key
    public string Totp { get; set; } = string.Empty;

    // gender — stored as a menu field; treat as string
    public string Gender { get; set; } = string.Empty;
}

// Custom converter to handle both string and object formats for field values
public class PuxFieldValueConverter : JsonConverter<PuxFieldValue>
{
    public override PuxFieldValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            // Value is a direct string
            return new PuxFieldValue { String = reader.GetString() ?? string.Empty };
        }
        else if (reader.TokenType == JsonTokenType.StartObject)
        {
            // Value is an object, deserialize normally
            var fieldValue = new PuxFieldValue();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return fieldValue;
                }

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString()?.ToLowerInvariant();
                    reader.Read();

                    switch (propertyName)
                    {
                        case "string":
                            if (reader.TokenType == JsonTokenType.String)
                                fieldValue.String = reader.GetString() ?? string.Empty;
                            else
                                SkipToken(ref reader);
                            break;
                        case "concealed":
                            if (reader.TokenType == JsonTokenType.String)
                                fieldValue.Concealed = reader.GetString() ?? string.Empty;
                            else
                                SkipToken(ref reader);
                            break;
                        case "email":
                            // email is an object {email_address, provider} or occasionally a plain string
                            if (reader.TokenType == JsonTokenType.String)
                            {
                                fieldValue.Email = reader.GetString() ?? string.Empty;
                            }
                            else if (reader.TokenType == JsonTokenType.StartObject)
                            {
                                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                                {
                                    if (reader.TokenType == JsonTokenType.PropertyName)
                                    {
                                        var key = reader.GetString()?.ToLowerInvariant();
                                        reader.Read();
                                        if (key == "email_address" && reader.TokenType == JsonTokenType.String)
                                            fieldValue.Email = reader.GetString() ?? string.Empty;
                                        else
                                            SkipToken(ref reader);
                                    }
                                }
                            }
                            else
                                SkipToken(ref reader);
                            break;
                        case "phone":
                            if (reader.TokenType == JsonTokenType.String)
                                fieldValue.Phone = reader.GetString() ?? string.Empty;
                            else
                                SkipToken(ref reader);
                            break;
                        case "url":
                            if (reader.TokenType == JsonTokenType.String)
                                fieldValue.Url = reader.GetString() ?? string.Empty;
                            else
                                SkipToken(ref reader);
                            break;
                        case "date":
                            if (reader.TokenType == JsonTokenType.Number)
                                fieldValue.Date = reader.GetInt64();
                            else
                                SkipToken(ref reader);
                            break;
                        case "monthyear":
                            if (reader.TokenType == JsonTokenType.Number)
                                fieldValue.MonthYear = reader.GetInt32();
                            else
                                SkipToken(ref reader);
                            break;
                        case "creditcardtype":
                            if (reader.TokenType == JsonTokenType.String)
                                fieldValue.CreditCardType = reader.GetString() ?? string.Empty;
                            else
                                SkipToken(ref reader);
                            break;
                        case "creditcardnumber":
                            if (reader.TokenType == JsonTokenType.String)
                                fieldValue.CreditCardNumber = reader.GetString() ?? string.Empty;
                            else
                                SkipToken(ref reader);
                            break;
                        case "reference":
                            if (reader.TokenType == JsonTokenType.String)
                                fieldValue.Reference = reader.GetString() ?? string.Empty;
                            else
                                SkipToken(ref reader);
                            break;
                        case "menu":
                            if (reader.TokenType == JsonTokenType.String)
                                fieldValue.Menu = reader.GetString() ?? string.Empty;
                            else
                                SkipToken(ref reader);
                            break;
                        case "gender":
                            if (reader.TokenType == JsonTokenType.String)
                                fieldValue.Gender = reader.GetString() ?? string.Empty;
                            else
                                SkipToken(ref reader);
                            break;
                        case "totp":
                        case "otp":
                            if (reader.TokenType == JsonTokenType.String)
                                fieldValue.Totp = reader.GetString() ?? string.Empty;
                            else
                                SkipToken(ref reader);
                            break;
                        case "address":
                            // address is a nested object — flatten to a readable string
                            if (reader.TokenType == JsonTokenType.StartObject)
                            {
                                var addrParts = new List<string>();
                                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                                {
                                    if (reader.TokenType == JsonTokenType.PropertyName)
                                    {
                                        var key = reader.GetString();
                                        reader.Read();
                                        if (reader.TokenType == JsonTokenType.String)
                                        {
                                            var val = reader.GetString();
                                            if (!string.IsNullOrWhiteSpace(val))
                                                addrParts.Add(val);
                                        }
                                        else
                                            SkipToken(ref reader);
                                    }
                                }
                                fieldValue.Address = string.Join(", ", addrParts);
                            }
                            else
                                SkipToken(ref reader);
                            break;
                        default:
                            // Skip unknown properties (including nested objects and arrays)
                            SkipToken(ref reader);
                            break;
                    }
                }
            }
            return fieldValue;
        }
        else if (reader.TokenType == JsonTokenType.StartArray || reader.TokenType == JsonTokenType.Number || reader.TokenType == JsonTokenType.True || reader.TokenType == JsonTokenType.False || reader.TokenType == JsonTokenType.Null)
        {
            // Skip arrays, numbers, booleans, and nulls
            SkipToken(ref reader);
            return new PuxFieldValue();
        }

        return new PuxFieldValue();
    }

    private static void SkipToken(ref Utf8JsonReader reader)
    {
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            int depth = 1;
            while (depth > 0 && reader.Read())
            {
                if (reader.TokenType == JsonTokenType.StartObject)
                    depth++;
                else if (reader.TokenType == JsonTokenType.EndObject)
                    depth--;
            }
        }
        else if (reader.TokenType == JsonTokenType.StartArray)
        {
            int depth = 1;
            while (depth > 0 && reader.Read())
            {
                if (reader.TokenType == JsonTokenType.StartArray)
                    depth++;
                else if (reader.TokenType == JsonTokenType.EndArray)
                    depth--;
            }
        }
        // For simple values (String, Number, True, False, Null), they're already consumed by the current position
    }

    public override void Write(Utf8JsonWriter writer, PuxFieldValue value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        if (!string.IsNullOrEmpty(value.String))
            writer.WriteString("string", value.String);
        if (!string.IsNullOrEmpty(value.Concealed))
            writer.WriteString("concealed", value.Concealed);
        if (!string.IsNullOrEmpty(value.Email))
            writer.WriteString("email", value.Email);
        if (!string.IsNullOrEmpty(value.Phone))
            writer.WriteString("phone", value.Phone);
        if (!string.IsNullOrEmpty(value.Url))
            writer.WriteString("url", value.Url);
        if (value.Date > 0)
            writer.WriteNumber("date", value.Date);
        if (value.MonthYear > 0)
            writer.WriteNumber("monthYear", value.MonthYear);
        if (!string.IsNullOrEmpty(value.CreditCardType))
            writer.WriteString("creditCardType", value.CreditCardType);
        if (!string.IsNullOrEmpty(value.CreditCardNumber))
            writer.WriteString("creditCardNumber", value.CreditCardNumber);
        if (!string.IsNullOrEmpty(value.Reference))
            writer.WriteString("reference", value.Reference);
        if (!string.IsNullOrEmpty(value.Menu))
            writer.WriteString("menu", value.Menu);
        if (!string.IsNullOrEmpty(value.Totp))
            writer.WriteString("totp", value.Totp);
        if (!string.IsNullOrEmpty(value.Gender))
            writer.WriteString("gender", value.Gender);
        if (!string.IsNullOrEmpty(value.Address))
            writer.WriteString("address", value.Address);

        writer.WriteEndObject();
    }
}

public class PuxPasswordHistory
{
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("time")]
    public long Time { get; set; }
}
