using System.IO.Compression;
using System.Text.Json;
using XBLauncher.Core.Data;
using XBLauncher.Core.Launch;
using XBLauncher.Core.Mojang;

namespace XBLauncher.Core.Install;

/// <summary>
/// Fabric-specific install: runs the official fabric-installer in server-less
/// profile mode to produce the loader libraries and the intermediary mappings,
/// then merges them into the launch classpath.
/// </summary>
public sealed class FabricInstaller : IDisposable
{
    private readonly HttpClient _http;
    private readonly JavaProvisioner _java;

    public FabricInstaller(HttpClient http, JavaProvisioner java)
    {
        _http = http;
        _java = java;
    }

    public const string InstallerUrl = "https://maven.fabricmc.net/net/fabricmc/fabric-installer/1.0.1/fabric-installer-1.0.1.jar";

    public async Task<List<string>> InstallAsync(string minecraftVersion, string loaderVersion,
        string librariesDir, IProgress<InstallProgress>? progress, CancellationToken ct = default)
    {
        progress?.Report(new InstallProgress("fabric", 0.0, "Lade Fabric-Installer..."));

        var installerJar = Path.Combine(Store.RuntimeDir, "fabric-installer.jar");
        if (!File.Exists(installerJar))
        {
            using var resp = await _http.GetAsync(InstallerUrl, ct).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
            await using var fs = File.Create(installerJar);
            await resp.Content.CopyToAsync(fs, ct).ConfigureAwait(false);
        }

        // Fabric installer supports "-d <dir> -mcversion -loader -downloadMinecraft false"
        // which only lays down libraries + profile metadata into the given dir.
        var fabricDir = Path.Combine(Store.RuntimeDir, "fabric", $"{minecraftVersion}-{loaderVersion}");
        Directory.CreateDirectory(fabricDir);

        // 26.x+ needs Java 25, everything older runs on 21.
        var javaMajor = minecraftVersion.StartsWith("26.") ? 25 : 21;
        var java = await _java.EnsureJavaAsync(javaMajor, ct: ct).ConfigureAwait(false);

        progress?.Report(new InstallProgress("fabric", 0.2, "Installiere Fabric-Loader..."));

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = java,
            WorkingDirectory = fabricDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        psi.ArgumentList.Add("-jar");
        psi.ArgumentList.Add(installerJar);
        psi.ArgumentList.Add("client");
        psi.ArgumentList.Add("-dir");
        psi.ArgumentList.Add(fabricDir);
        psi.ArgumentList.Add("-mcversion");
        psi.ArgumentList.Add(minecraftVersion);
        psi.ArgumentList.Add("-loader");
        psi.ArgumentList.Add(loaderVersion);
        psi.ArgumentList.Add("-downloadMinecraft");
        psi.ArgumentList.Add("false");
        psi.ArgumentList.Add("-noprofile");

        using var proc = System.Diagnostics.Process.Start(psi)
            ?? throw new InvalidOperationException("Fabric-Installer konnte nicht gestartet werden");

        // Read stdout/stderr concurrently to avoid deadlocking when the child
        // process fills its output buffers before exiting.
        var stdoutTask = proc.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = proc.StandardError.ReadToEndAsync(ct);
        await proc.WaitForExitAsync(ct).ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);

        if (proc.ExitCode != 0)
        {
            throw new InvalidOperationException($"Fabric-Installer fehlgeschlagen ({proc.ExitCode}): {stderr}");
        }

        progress?.Report(new InstallProgress("fabric", 0.8, "Sammle Fabric-Bibliotheken..."));

        // Collect every jar the fabric installer placed under libraries
        var jars = Directory.GetFiles(fabricDir, "*.jar", SearchOption.AllDirectories)
            .Where(j => j.Contains("libraries", StringComparison.OrdinalIgnoreCase))
            .ToList();

        progress?.Report(new InstallProgress("fabric", 1.0, $"{jars.Count} Fabric-Bibliotheken"));
        return jars;
    }

    public void Dispose() { }
}
