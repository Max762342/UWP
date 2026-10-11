using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;

namespace XBLauncher.Core.Install;

/// <summary>
/// Downloads a portable Eclipse Temurin JRE into the launcher runtime folder so
/// the user never has to install Java manually. Minecraft 1.21+ needs Java 21,
/// the 26.x line needs Java 25.
/// </summary>
public sealed class JavaProvisioner : IDisposable
{
    private readonly HttpClient _http;

    public JavaProvisioner(HttpClient? http = null)
    {
        _http = http ?? new HttpClient(new SocketsHttpHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.All
        })
        {
            Timeout = TimeSpan.FromMinutes(30)
        };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("XB-Launcher/1.0");
    }

    /// <summary>
    /// Returns the path to a java executable for the requested major version,
    /// downloading and extracting a Temurin JRE first when needed.
    /// </summary>
    public async Task<string> EnsureJavaAsync(int majorVersion, IProgress<string>? log = null,
        CancellationToken ct = default)
    {
        var root = Path.Combine(Data.Store.RuntimeDir, $"jdk-{majorVersion}");
        var javaExe = Path.Combine(root, "bin", "java.exe");
        if (File.Exists(javaExe))
            return javaExe;

        Directory.CreateDirectory(root);
        log?.Report($"Lade Temurin JRE {majorVersion} herunter...");

        // Adoptium API: latest GA JRE for Windows x64, zip archive
        var url = $"https://api.adoptium.net/v3/binary/latest/{majorVersion}/ga/windows/x64/jre/hotspot/normal/eclipse";
        using var resp = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct)
            .ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();

        var zipPath = Path.Combine(Data.Store.RuntimeDir, $"jdk-{majorVersion}.zip");
        await using (var fs = File.Create(zipPath))
        {
            await resp.Content.CopyToAsync(fs, ct).ConfigureAwait(false);
        }

        log?.Report("Entpacke JRE...");
        var tmpExtract = Path.Combine(Data.Store.RuntimeDir, $"jdk-{majorVersion}-tmp");
        if (Directory.Exists(tmpExtract))
            Directory.Delete(tmpExtract, true);
        ZipFile.ExtractToDirectory(zipPath, tmpExtract);

        // Adoptium zips contain a single top level folder like jdk-21.0.x+y-jre
        var top = Directory.GetDirectories(tmpExtract).FirstOrDefault()
                  ?? throw new InvalidOperationException("Unexpected JRE archive layout");
        foreach (var entry in Directory.GetFileSystemEntries(top))
        {
            var name = Path.GetFileName(entry);
            var dest = Path.Combine(root, name);
            if (Directory.Exists(entry))
                Directory.Move(entry, dest);
            else
                File.Move(entry, dest, true);
        }

        Directory.Delete(tmpExtract, true);
        File.Delete(zipPath);

        if (!File.Exists(javaExe))
            throw new InvalidOperationException($"java.exe fehlt nach Installation unter {javaExe}");

        log?.Report($"JRE {majorVersion} bereit: {javaExe}");
        return javaExe;
    }

    public void Dispose() => _http.Dispose();
}
