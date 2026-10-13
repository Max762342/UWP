using System.IO.Compression;
using System.Net.Http;

namespace XBLauncher.Core.Install;

/// <summary>
/// Downloads a zip from a URL and extracts it to a destination folder.
/// Used for LWJGL natives and loader installers.
/// </summary>
public static class ZipUtil
{
    public static async Task DownloadAndExtractAsync(HttpClient http, string url, string destDir,
        IProgress<double>? progress = null, CancellationToken ct = default)
    {
        Directory.CreateDirectory(destDir);

        using var resp = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct)
            .ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();

        var tmp = Path.GetTempFileName();
        try
        {
            var total = resp.Content.Headers.ContentLength ?? 0;
            using (var input = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false))
            using (var output = File.Create(tmp))
            {
                var buffer = new byte[81920];
                long read = 0;
                int n;
                while ((n = await input.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false)) > 0)
                {
                    await output.WriteAsync(buffer, 0, n, ct).ConfigureAwait(false);
                    read += n;
                    if (total > 0)
                        progress?.Report((double)read / total);
                }
            }

            ZipFileExtensions.ExtractToDirectoryOverwrite(tmp, destDir);
            progress?.Report(1.0);
        }
        finally
        {
            try { File.Delete(tmp); } catch { /* best effort */ }
        }
    }
}
