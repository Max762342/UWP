using XBLauncher.Core.Data;
using XBLauncher.Core.Modrinth;

namespace XBLauncher.Core.Install;

/// <summary>
/// Manages the three hub folders inside a profile's game dir:
///   mods\            Fabric/Forge/NeoForge mods
///   shaderpacks\     Shader packs (Iris/OptiFine compatible)
///   resourcepacks\   Resource / texture packs
/// </summary>
public sealed class ContentManager
{
    private readonly ModrinthClient _modrinth;

    public ContentManager(ModrinthClient modrinth)
    {
        _modrinth = modrinth;
    }

    private static string HubFolder(HubKind hub) => hub switch
    {
        HubKind.Mods => "mods",
        HubKind.Shaders => "shaderpacks",
        HubKind.ResourcePacks => "resourcepacks",
        _ => "mods"
    };

    public string ContentDir(Profile profile, HubKind hub) =>
        Path.Combine(Store.InstanceDir(profile), HubFolder(hub));

    public IReadOnlyList<string> ListInstalled(Profile profile, HubKind hub)
    {
        var dir = ContentDir(profile, hub);
        if (!Directory.Exists(dir))
            return Array.Empty<string>();
        return Directory.GetFiles(dir).Select(Path.GetFileName).Where(n => n is not null).Select(n => n!)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public async Task<string> InstallAsync(Profile profile, LaunchTarget target, HubKind hub,
        ModrinthProject project, IProgress<double>? progress, CancellationToken ct = default)
    {
        var loader = target.Loader switch
        {
            LoaderKind.Fabric => "fabric",
            LoaderKind.NeoForge => "neoforge",
            LoaderKind.Forge => "forge",
            _ => null
        };

        // shader and resource packs are loader agnostic
        if (hub != HubKind.Mods)
            loader = null;

        var version = await _modrinth.ResolveVersionAsync(project.ProjectId, target.MinecraftVersion, loader, ct)
            .ConfigureAwait(false);

        if (version is null)
            throw new InvalidOperationException(
                $"Keine kompatible Version von \"{project.Title}\" für {target.MinecraftVersion} gefunden.");

        var file = version.Files.FirstOrDefault(f => f.Primary) ?? version.Files.FirstOrDefault()
            ?? throw new InvalidOperationException("Version enthält keine Datei.");

        var destDir = ContentDir(profile, hub);
        Directory.CreateDirectory(destDir);
        var destFile = Path.Combine(destDir, file.Filename);

        await _modrinth.DownloadFileAsync(file, destFile, progress, ct).ConfigureAwait(false);
        return destFile;
    }

    public void Remove(Profile profile, HubKind hub, string fileName)
    {
        var path = Path.Combine(ContentDir(profile, hub), fileName);
        if (File.Exists(path))
            File.Delete(path);
    }
}
