using XBLauncher.Core.Data;
using XBLauncher.Core.Launch;

namespace XBLauncher.Core.Install;

/// <summary>
/// NeoForge install via the official NeoForge installer CLI
/// ("java -jar neoforge-installer.jar --installClient &lt;dir&gt;").
/// </summary>
public sealed class NeoForgeInstaller : IDisposable
{
    private readonly HttpClient _http;
    private readonly JavaProvisioner _java;

    public NeoForgeInstaller(HttpClient http, JavaProvisioner java)
    {
        _http = http;
        _java = java;
    }

    public async Task<List<string>> InstallAsync(string minecraftVersion, string neoVersion,
        IProgress<InstallProgress>? progress, CancellationToken ct = default)
    {
        progress?.Report(new InstallProgress("neoforge", 0.0, "Lade NeoForge-Installer..."));

        var installerJar = Path.Combine(Store.RuntimeDir, $"neoforge-installer-{minecraftVersion}-{neoVersion}.jar");
        if (!File.Exists(installerJar))
        {
            // NeoForge maven layout
            var url = $"https://maven.neoforged.net/releases/net/neoforged/neoforge/{neoVersion}/neoforge-{neoVersion}-installer.jar";
            using var resp = await _http.GetAsync(url, ct).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
            await using var fs = File.Create(installerJar);
            await resp.Content.CopyToAsync(fs, ct).ConfigureAwait(false);
        }

        var dir = Path.Combine(Store.RuntimeDir, "neoforge", $"{minecraftVersion}-{neoVersion}");
        Directory.CreateDirectory(dir);

        // 26.x+ needs Java 25, everything older runs on 21.
        var javaMajor = minecraftVersion.StartsWith("26.") ? 25 : 21;
        var java = await _java.EnsureJavaAsync(javaMajor, ct: ct).ConfigureAwait(false);

        progress?.Report(new InstallProgress("neoforge", 0.3, "Installiere NeoForge..."));

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = java,
            WorkingDirectory = dir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        psi.ArgumentList.Add("-jar");
        psi.ArgumentList.Add(installerJar);
        psi.ArgumentList.Add("--installClient");
        psi.ArgumentList.Add(dir);

        using var proc = System.Diagnostics.Process.Start(psi)
            ?? throw new InvalidOperationException("NeoForge-Installer konnte nicht gestartet werden");

        // Read stdout/stderr concurrently to avoid deadlocking when the child
        // process fills its output buffers before exiting.
        var stdoutTask = proc.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = proc.StandardError.ReadToEndAsync(ct);
        await proc.WaitForExitAsync(ct).ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);

        if (proc.ExitCode != 0)
        {
            throw new InvalidOperationException($"NeoForge-Installer fehlgeschlagen ({proc.ExitCode}): {stderr}");
        }

        var jars = Directory.GetFiles(dir, "*.jar", SearchOption.AllDirectories)
            .Where(j => j.Contains("libraries", StringComparison.OrdinalIgnoreCase))
            .ToList();

        progress?.Report(new InstallProgress("neoforge", 1.0, $"{jars.Count} NeoForge-Bibliotheken"));
        return jars;
    }

    public void Dispose() { }
}
