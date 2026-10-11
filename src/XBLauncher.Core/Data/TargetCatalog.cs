namespace XBLauncher.Core.Data;

/// <summary>
/// Catalog of every launch target supported by XB-Launcher. This is the single
/// source of truth for the version picker in the UI.
/// </summary>
public static class TargetCatalog
{
    /// <summary>
    /// Fabric Loader version used for all Fabric targets. Kept in one place so a
    /// single bump updates the entire matrix.
    /// </summary>
    public const string DefaultFabricLoader = "0.19.2";

    /// <summary>
    /// NeoForge version mapping per Minecraft version. Values are the loader
    /// versions that officially support the matching Minecraft release.
    /// </summary>
    private static readonly Dictionary<string, string> NeoForgeByMinecraft = new()
    {
        ["1.21"] = "21.0.167",
        ["1.21.1"] = "21.1.172",
        ["1.21.3"] = "21.3.39",
        ["1.21.4"] = "21.4.147",
        ["1.21.5"] = "21.5.86",
        ["1.21.6"] = "21.6.27",
        ["1.21.9"] = "21.9.38",
        ["1.21.11"] = "21.11.30",
        ["26.1"] = "26.1.10",
        ["26.2"] = "26.2.5",
        ["26.3"] = "26.3.2",
        ["26.4"] = "26.4.1",
    };

    /// <summary>
    /// Forge is only available for a subset of versions. Missing entries simply
    /// do not produce a Forge target.
    /// </summary>
    private static readonly Dictionary<string, string> ForgeByMinecraft = new()
    {
        ["1.21"] = "52.0.16",
        ["1.21.1"] = "52.0.28",
        ["1.21.3"] = "53.1.0",
        ["1.21.4"] = "54.1.4",
        ["1.21.5"] = "55.0.12",
    };

    /// <summary>
    /// Every Minecraft version XB-Launcher must support on Xbox.
    /// </summary>
    public static readonly IReadOnlyList<string> MinecraftVersions = new[]
    {
        "1.21",
        "1.21.1",
        "1.21.3",
        "1.21.4",
        "1.21.5",
        "1.21.6",
        "1.21.9",
        "1.21.11",
        "26.1",
        "26.2",
        "26.3",
        "26.4",
    };

    public static IReadOnlyList<LaunchTarget> Build()
    {
        var list = new List<LaunchTarget>();

        foreach (var mc in MinecraftVersions)
        {
            list.Add(new LaunchTarget
            {
                Id = $"vanilla-{mc}",
                MinecraftVersion = mc,
                Loader = LoaderKind.Vanilla
            });

            list.Add(new LaunchTarget
            {
                Id = $"fabric-{mc}",
                MinecraftVersion = mc,
                Loader = LoaderKind.Fabric,
                LoaderVersion = DefaultFabricLoader
            });

            if (NeoForgeByMinecraft.TryGetValue(mc, out var neo))
            {
                list.Add(new LaunchTarget
                {
                    Id = $"neoforge-{mc}",
                    MinecraftVersion = mc,
                    Loader = LoaderKind.NeoForge,
                    LoaderVersion = neo
                });
            }

            if (ForgeByMinecraft.TryGetValue(mc, out var forge))
            {
                list.Add(new LaunchTarget
                {
                    Id = $"forge-{mc}",
                    MinecraftVersion = mc,
                    Loader = LoaderKind.Forge,
                    LoaderVersion = forge
                });
            }
        }

        return list;
    }

    public static LaunchTarget? Find(string targetId) =>
        Build().FirstOrDefault(t => string.Equals(t.Id, targetId, StringComparison.OrdinalIgnoreCase));
}
