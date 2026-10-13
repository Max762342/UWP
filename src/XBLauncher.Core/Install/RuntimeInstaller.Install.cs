using System.IO.Compression;
using System.Text.Json;
using XBLauncher.Core.Data;
using XBLauncher.Core.Launch;
using XBLauncher.Core.Mojang;

namespace XBLauncher.Core.Install;

public sealed partial class RuntimeInstaller
{
    public async Task<InstallReady> EnsureInstalledAsync(LaunchTarget target, Profile profile,
        IProgress<InstallProgress>? progress, CancellationToken ct = default)
    {
        progress?.Report(new InstallProgress("manifest", 0.0, "Lade Versions-Manifest..."));

        var manifest = await Mojang.GetVersionManifestAsync(ct).ConfigureAwait(false);
        var entry = manifest.Versions.FirstOrDefault(v =>
                string.Equals(v.Id, target.MinecraftVersion, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Minecraft {target.MinecraftVersion} nicht im Manifest");

        progress?.Report(new InstallProgress("manifest", 0.05, "Lade Versions-Details..."));
        var detail = await Mojang.GetVersionDetailAsync(entry.Url, ct).ConfigureAwait(false);

        var gameDir = Store.InstanceDir(profile);
        Directory.CreateDirectory(gameDir);

        var versionDir = Path.Combine(VersionsDir, detail.Id);
        Directory.CreateDirectory(versionDir);
        var clientJar = Path.Combine(versionDir, $"{detail.Id}.jar");
        if (detail.Downloads.TryGetValue("client", out var client))
        {
            await DownloadVerifiedAsync(client, clientJar, "client.jar",
                new InstallProgress("client", 0.05, "Lade Minecraft-Client..."), progress, ct)
                .ConfigureAwait(false);
        }

        progress?.Report(new InstallProgress("assets", 0.1, "Lade Asset-Index..."));
        var indexFile = Path.Combine(AssetsIndexesDir, $"{detail.AssetIndex?.Id ?? detail.Assets}.json");
        Directory.CreateDirectory(AssetsIndexesDir);
        if (detail.AssetIndex is not null)
        {
            await DownloadVerifiedAsync(
                new DownloadArtifact { Url = detail.AssetIndex.Url, Sha1 = detail.AssetIndex.Sha1, Size = detail.AssetIndex.Size },
                indexFile, "asset index",
                new InstallProgress("assets", 0.1, "Lade Asset-Index..."), progress, ct)
                .ConfigureAwait(false);
        }

        // Download the actual asset objects referenced by the index. Without these
        // Minecraft starts and then crashes while loading sounds/textures.
        await DownloadAssetObjectsAsync(indexFile, progress, ct).ConfigureAwait(false);

        var classpath = new List<string> { clientJar };
        var libs = detail.Libraries;
        var totalLibs = Math.Max(1, libs.Count);
        for (var i = 0; i < libs.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var lib = libs[i];
            if (!IsAllowedOnOs(lib))
                continue;

            if (lib.Downloads?.Artifact is { } artifact)
            {
                var rel = artifact.Url.Contains("/libraries/")
                    ? artifact.Url.Substring(artifact.Url.IndexOf("/libraries/", StringComparison.Ordinal) + "/libraries/".Length)
                    : artifact.Url;
                var path = Path.Combine(LibrariesDir, rel.Replace('/', Path.DirectorySeparatorChar));
                await DownloadVerifiedAsync(artifact, path, lib.Name,
                    new InstallProgress("libraries", 0.15 + 0.5 * i / totalLibs, lib.Name), progress, ct)
                    .ConfigureAwait(false);
                classpath.Add(path);
            }
        }

        var nativesDir = Path.Combine(Store.RuntimeDir, "natives", target.Id);
        Directory.CreateDirectory(nativesDir);
        foreach (var lib in libs)
        {
            ct.ThrowIfCancellationRequested();
            if (lib.Natives is null || lib.Downloads?.Classifiers is null)
                continue;
            if (!lib.Natives.TryGetValue("windows", out var classifier))
                continue;
            if (!lib.Downloads.Classifiers.TryGetValue(classifier, out var nativeArtifact))
                continue;

            var zipPath = Path.Combine(nativesDir, $"{Sanitize(lib.Name)}-{classifier}.jar");
            await DownloadVerifiedAsync(nativeArtifact, zipPath, lib.Name,
                new InstallProgress("natives", 0.7, lib.Name), progress, ct).ConfigureAwait(false);

            try { ZipFileExtensions.ExtractToDirectoryOverwrite(zipPath, nativesDir); }
            catch (InvalidDataException) { }
        }

        var mainClass = detail.MainClass;
        var jvmArgs = new List<string>();
        var gameArgs = new List<string>();

        if (detail.Arguments is not null)
        {
            jvmArgs.AddRange(FlattenArgs(detail.Arguments.Jvm));
            gameArgs.AddRange(FlattenArgs(detail.Arguments.Game));
        }
        else if (!string.IsNullOrEmpty(detail.LegacyArguments))
        {
            gameArgs.AddRange(detail.LegacyArguments.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        }

        jvmArgs.Add($"-Djava.library.path={nativesDir}");
        jvmArgs.Add("-cp");
        jvmArgs.Add(string.Join(Path.PathSeparator.ToString(), classpath));

        return new InstallReady
        {
            GameDir = gameDir,
            NativesDir = nativesDir,
            Classpath = classpath,
            MainClass = mainClass,
            JvmArgs = jvmArgs,
            GameArgs = gameArgs,
            AssetsDir = AssetsDir,
            AssetIndexId = detail.AssetIndex?.Id ?? detail.Assets,
            JavaMajor = detail.JavaVersion is { MajorVersion: >= 21 } ? detail.JavaVersion.MajorVersion : 21
        };
    }
}
