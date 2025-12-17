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

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("date")]
    public long Date { get; set; }
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
                            if (reader.TokenType == JsonTokenType.String)
                                fieldValue.Email = reader.GetString() ?? string.Empty;
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
