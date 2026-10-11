using System.Text.Json;
using XBLauncher.Core.Mojang;

namespace XBLauncher.Core.Install;

public sealed partial class RuntimeInstaller
{
    private async Task DownloadVerifiedAsync(DownloadArtifact artifact, string destPath, string label,
        XBLauncher.Core.Launch.InstallProgress stage, IProgress<XBLauncher.Core.Launch.InstallProgress>? progress, CancellationToken ct)
    {
        if (File.Exists(destPath))
        {
            if (string.IsNullOrEmpty(artifact.Sha1) || VerifySha1(destPath, artifact.Sha1))
            {
                progress?.Report(stage with { Detail = $"{label} (bereits vorhanden)" });
                return;
            }

            File.Delete(destPath);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
        progress?.Report(stage);

        using var resp = await Http.GetAsync(artifact.Url, HttpCompletionOption.ResponseHeadersRead, ct)
            .ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();

        await using (var input = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false))
        await using (var output = File.Create(destPath))
        {
            await input.CopyToAsync(output, ct).ConfigureAwait(false);
        }

        if (!string.IsNullOrEmpty(artifact.Sha1) && !VerifySha1(destPath, artifact.Sha1))
        {
            File.Delete(destPath);
            throw new InvalidDataException($"SHA1-Mismatch fÃ¼r {label}");
        }
    }

    private static bool VerifySha1(string path, string expected)
    {
        using var sha = System.Security.Cryptography.SHA1.Create();
        using var stream = File.OpenRead(path);
        var actual = Convert.ToHexString(sha.ComputeHash(stream)).ToLowerInvariant();
        return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Reads the downloaded asset index and fetches every referenced asset object
    /// into assets/objects/&lt;xx&gt;/&lt;sha1&gt;. Skips objects that already exist and
    /// match their hash so re-installing is cheap.
    /// </summary>
    private async Task DownloadAssetObjectsAsync(string indexFile,
        IProgress<XBLauncher.Core.Launch.InstallProgress>? progress, CancellationToken ct)
    {
        if (!File.Exists(indexFile))
            return;

        AssetIndex index;
        try
        {
            await using (var stream = File.OpenRead(indexFile))
            {
                index = await JsonSerializer.DeserializeAsync<AssetIndex>(stream, Json, ct)
                            .ConfigureAwait(false) ?? new AssetIndex();
            }
        }
        catch (JsonException)
        {
            // Unreadable index -> nothing we can do, but do not fail the launch.
            return;
        }

        var objects = index.Objects;
        if (objects.Count == 0)
            return;

        var objectsDir = Path.Combine(AssetsDir, "objects");
        var total = objects.Count;
        var done = 0;

        foreach (var (name, obj) in objects)
        {
            ct.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(obj.Hash) || obj.Hash.Length < 2)
                continue;

            var prefix = obj.Hash[..2].ToLowerInvariant();
            var destPath = Path.Combine(objectsDir, prefix, obj.Hash.ToLowerInvariant());

            if (File.Exists(destPath))
            {
                if (string.IsNullOrEmpty(obj.Hash) || VerifySha1(destPath, obj.Hash))
                {
                    done++;
                    continue;
                }
                File.Delete(destPath);
            }

            var url = $"https://resources.download.minecraft.net/{prefix}/{obj.Hash.ToLowerInvariant()}";
            var artifact = new DownloadArtifact { Url = url, Sha1 = obj.Hash, Size = obj.Size };

            try
            {
                await DownloadVerifiedAsync(artifact, destPath, $"asset {name}",
                    new XBLauncher.Core.Launch.InstallProgress("assets",
                        0.12 + 0.55 * done / Math.Max(1, total), name),
                    progress, ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // A single missing asset should not abort the whole install; the
                // game can still run and will re-fetch on demand.
                progress?.Report(new XBLauncher.Core.Launch.InstallProgress("assets",
                    0.12 + 0.55 * done / Math.Max(1, total),
                    $"Asset übersprungen: {name} ({ex.Message})"));
            }

            done++;
        }
    }

    private static string Sanitize(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }
}

