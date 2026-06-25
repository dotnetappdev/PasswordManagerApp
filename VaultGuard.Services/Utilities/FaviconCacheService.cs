using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace PasswordManager.Services.Utilities
{
    /// <summary>
    /// Cross-platform favicon cache service. Downloads favicons and caches them under
    /// AppData (per-process base directory) in `favicons/{host}.png`.
    /// Designed to be usable from WinUI, Blazor server/wasm hosting, and Uno.
    /// </summary>
    public class FaviconCacheService
    {
        private readonly string _cacheDir;
        private readonly HttpClient _http;

        public FaviconCacheService() : this(new HttpClient()) { }

        public FaviconCacheService(HttpClient httpClient)
        {
            _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            var baseDir = AppDomain.CurrentDomain.BaseDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _cacheDir = Path.Combine(baseDir, "favicons");
            try { Directory.CreateDirectory(_cacheDir); } catch { }
        }

        /// <summary>
        /// Returns a local file path (full path) to the cached favicon for the given host.
        /// If not present, attempts to download using the provided favicon URL (or google service fallback).
        /// Returns null if download failed.
        /// </summary>
        public async Task<string?> GetOrCreateFaviconAsync(string hostOrUrl)
        {
            if (string.IsNullOrWhiteSpace(hostOrUrl)) return null;

            string host = hostOrUrl;
            try
            {
                if (!host.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !host.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    host = "https://" + host;

                var uri = new Uri(host);
                host = uri.Host.Replace("www.", "");
            }
            catch
            {
                // if parsing fails, use the raw string sanitized
                host = hostOrUrl.Replace("/", "_").Replace(":", "_");
            }

            var fileName = SanitizeFileName(host) + ".png";
            var filePath = Path.Combine(_cacheDir, fileName);

            if (File.Exists(filePath)) return filePath;

            // Try google favicon service
            var favUrl = new Uri($"https://www.google.com/s2/favicons?domain={host}&sz=64");

            try
            {
                using var resp = await _http.GetAsync(favUrl);
                if (resp.IsSuccessStatusCode)
                {
                    var bytes = await resp.Content.ReadAsByteArrayAsync();
                    if (bytes != null && bytes.Length > 0)
                    {
                        await File.WriteAllBytesAsync(filePath, bytes);
                        return filePath;
                    }
                }
            }
            catch
            {
                // swallow - return null below
            }

            return null;
        }

        private static string SanitizeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }
    }
}
