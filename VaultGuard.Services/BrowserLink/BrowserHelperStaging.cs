using System.Security.Cryptography;
using System.Text;

namespace VaultGuard.Services.BrowserLink;

/// <summary>
/// Gives the browsers their own copy of the browser helper, under %LocalAppData%\VaultGuard\BrowserSupport\&lt;version&gt;,
/// instead of the one in the app's folder. The browser starts the helper for each request; while a copy runs, its
/// files are locked. When that copy was the one in the app's build output, rebuilding the app in Visual Studio
/// failed (or hung) trying to overwrite them. A copy per helper version also survives app updates cleanly.
/// </summary>
public static class BrowserHelperStaging
{
    /// <summary>Returns the staged helper's path, or <paramref name="helperPath"/> itself if staging isn't possible.</summary>
    public static string Stage(string helperPath, string? rootOverride = null)
    {
        try
        {
            var sourceDir = Path.GetDirectoryName(helperPath);
            if (sourceDir == null || !File.Exists(helperPath)) return helperPath;
            var files = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);

            // The version is a fingerprint of the helper's files, so a rebuilt helper gets a fresh folder.
            var sb = new StringBuilder();
            foreach (var f in files.OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                var info = new FileInfo(f);
                sb.Append(Path.GetRelativePath(sourceDir, f)).Append('|').Append(info.Length).Append('|').Append(info.LastWriteTimeUtc.Ticks).Append('\n');
            }
            var version = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString())))[..16].ToLowerInvariant();

            var root = rootOverride ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VaultGuard", "BrowserSupport");
            var target = Path.Combine(root, version);
            var marker = Path.Combine(target, ".complete");
            if (!File.Exists(marker))
            {
                var temp = target + ".tmp-" + Guid.NewGuid().ToString("N")[..8];
                foreach (var f in files)
                {
                    var dest = Path.Combine(temp, Path.GetRelativePath(sourceDir, f));
                    Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                    File.Copy(f, dest, overwrite: true);
                }
                File.WriteAllText(Path.Combine(temp, ".complete"), DateTime.UtcNow.ToString("O"));
                if (Directory.Exists(target)) { try { Directory.Delete(target, true); } catch { /* in use: keep using it */ } }
                if (!Directory.Exists(target)) Directory.Move(temp, target);
                else { try { Directory.Delete(temp, true); } catch { } }
            }

            // Tidy older versions; one still running is locked and simply stays until next time.
            foreach (var dir in Directory.GetDirectories(root))
            {
                if (string.Equals(Path.GetFileName(dir), version, StringComparison.OrdinalIgnoreCase)) continue;
                try { Directory.Delete(dir, true); } catch { /* in use */ }
            }

            var staged = Path.Combine(target, Path.GetFileName(helperPath));
            return File.Exists(staged) ? staged : helperPath;
        }
        catch (Exception ex)
        {
            VaultGuard.Services.Logging.AppLogger.Warning("Couldn't copy the browser helper; the browser will use the app's own copy", ex);
            return helperPath;
        }
    }
}
