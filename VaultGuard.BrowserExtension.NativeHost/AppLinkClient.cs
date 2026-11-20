using System.Buffers.Binary;
using System.IO.Pipes;
using System.Text.Json;

/// <summary>
/// Forwards browser requests to the running Vault Guard desktop app (WPF or MAUI) over its per-user named pipe —
/// the app answers from its unlocked vault, so the extension works whenever the app is unlocked (like 1Password).
/// Mirrors VaultGuard.Services/BrowserLink/BrowserLinkProtocol (this host doesn't reference that assembly):
/// one request per connection, 4-byte little-endian length + UTF-8 JSON.
/// </summary>
internal static class AppLinkClient
{
    public const int ProtocolVersion = 2;

    private static string PipeName => "VaultGuard.BrowserLink." + new string(Environment.UserName.Where(char.IsLetterOrDigit).ToArray());

    /// <summary>The app's JSON response, or null when the app isn't running / not reachable.</summary>
    public static async Task<JsonElement?> TryForwardAsync(Dictionary<string, object> message, string? callerOrigin, TimeSpan responseTimeout)
    {
        try
        {
            using var pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            using (var connect = new CancellationTokenSource(TimeSpan.FromMilliseconds(1500)))
                await pipe.ConnectAsync(connect.Token);

            var request = new Dictionary<string, object>(message);
            request.Remove("password"); // never pass a master password through the link
            if (!string.IsNullOrEmpty(callerOrigin)) request["origin"] = callerOrigin;

            var body = JsonSerializer.SerializeToUtf8Bytes(request);
            var header = new byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(header, body.Length);
            using var cts = new CancellationTokenSource(responseTimeout);
            await pipe.WriteAsync(header, cts.Token);
            await pipe.WriteAsync(body, cts.Token);
            await pipe.FlushAsync(cts.Token);

            if (!await ReadExactly(pipe, header, cts.Token)) return null;
            var length = BinaryPrimitives.ReadInt32LittleEndian(header);
            if (length <= 0 || length > 1024 * 1024) return null;
            var response = new byte[length];
            if (!await ReadExactly(pipe, response, cts.Token)) return null;
            using var doc = JsonDocument.Parse(response);
            return doc.RootElement.Clone();
        }
        catch (Exception ex) when (ex is TimeoutException or OperationCanceledException or IOException or UnauthorizedAccessException)
        {
            return null; // app not running (or not ours to talk to)
        }
    }

    private static async Task<bool> ReadExactly(Stream s, byte[] buffer, CancellationToken ct)
    {
        var read = 0;
        while (read < buffer.Length)
        {
            var n = await s.ReadAsync(buffer.AsMemory(read), ct);
            if (n == 0) return false;
            read += n;
        }
        return true;
    }
}
