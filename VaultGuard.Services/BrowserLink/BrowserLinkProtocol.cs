using System.Buffers.Binary;
using System.Text;
using System.Text.Json;

namespace VaultGuard.Services.BrowserLink;

/// <summary>
/// The local link between the browser extension's native-messaging host and the running Vault Guard app
/// (WPF or MAUI) — the same model as 1Password's browser integration: the app holds the unlocked vault and
/// answers; the extension never sees the master password. One request per connection over a named pipe that
/// only the current OS user can open; each message is a 4-byte little-endian length followed by UTF-8 JSON.
/// Since protocol 3 the JSON is an encrypted envelope (BrowserLinkCrypto), so the pipe carries no secrets.
/// The native host keeps its own copy of these constants (it doesn't reference this assembly) — keep in sync.
/// </summary>
public static class BrowserLinkProtocol
{
    /// <summary>Bump when request/response shapes change incompatibly; the extension shows a "please update" hint.</summary>
    /// <summary>3: requests and responses are end-to-end encrypted (BrowserLinkCrypto).</summary>
    public const int Version = 3;
    public const int MaxMessageBytes = 1024 * 1024;

    /// <summary>Per-user pipe name (also access-controlled to the current user by the server).</summary>
    public static string PipeName => "VaultGuard.BrowserLink." + Sanitize(Environment.UserName);

    private static string Sanitize(string s) => new(s.Where(char.IsLetterOrDigit).ToArray());

    public static async Task<JsonDocument?> ReadAsync(Stream stream, CancellationToken ct)
    {
        var header = new byte[4];
        if (!await ReadExactlyAsync(stream, header, ct)) return null;
        var length = BinaryPrimitives.ReadInt32LittleEndian(header);
        if (length <= 0 || length > MaxMessageBytes) throw new InvalidDataException($"Invalid message length {length}.");
        var body = new byte[length];
        if (!await ReadExactlyAsync(stream, body, ct)) return null;
        return JsonDocument.Parse(body);
    }

    public static async Task WriteAsync(Stream stream, object message, CancellationToken ct)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(message);
        var header = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(header, body.Length);
        await stream.WriteAsync(header, ct);
        await stream.WriteAsync(body, ct);
        await stream.FlushAsync(ct);
    }

    private static async Task<bool> ReadExactlyAsync(Stream stream, byte[] buffer, CancellationToken ct)
    {
        var read = 0;
        while (read < buffer.Length)
        {
            var n = await stream.ReadAsync(buffer.AsMemory(read), ct);
            if (n == 0) return false;
            read += n;
        }
        return true;
    }

    public static string Utf8(this JsonElement e, string name)
        => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";
}
