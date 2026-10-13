namespace XBLauncher.Core.Modrinth;

public sealed partial class ModrinthClient
{
    /// <summary>
    /// Download a Modrinth file to <paramref name="destFile"/> and verify its SHA1
    /// before accepting it.
    /// </summary>
    public async Task DownloadFileAsync(
        ModrinthFile file,
        string destFile,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        var dir = Path.GetDirectoryName(destFile);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        using var resp = await _http.GetAsync(file.Url, HttpCompletionOption.ResponseHeadersRead, ct)
            .ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();

        var total = resp.Content.Headers.ContentLength ?? file.Size;
        using (var input = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false))
        using (var output = File.Create(destFile))
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

        progress?.Report(1.0);

        if (file.Hashes.TryGetValue("sha1", out var sha1) && !string.IsNullOrEmpty(sha1))
        {
            // Verify against a separately opened read stream after the write
            // stream has been flushed/closed, so no buffered bytes are missed.
            using (var verify = File.OpenRead(destFile))
            {
                using (var sha = System.Security.Cryptography.SHA1.Create())
                {
                    var actual = BitConverter.ToString(sha.ComputeHash(verify)).Replace("-", "").ToLowerInvariant();
                    if (!string.Equals(actual, sha1, StringComparison.OrdinalIgnoreCase))
                    {
                        File.Delete(destFile);
                        throw new InvalidDataException(
                            $"Hash mismatch for {file.Filename}: expected {sha1}, got {actual}");
                    }
                }
            }
        }
    }
}
