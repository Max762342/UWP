using System.Text.Json.Serialization;

namespace XBLauncher.Core.Data;

/// <summary>
/// Loader types supported for a Minecraft target.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LoaderKind
{
    Vanilla,
    Fabric,
    NeoForge,
    Forge
}

/// <summary>
/// A single playable launch target: a Minecraft version combined with a mod loader.
/// The catalog intentionally covers every version requested for XB-Launcher:
/// 1.21, 1.21.1, 1.21.3, 1.21.4, 1.21.5, 1.21.6, 1.21.9, 1.21.11 and 26.1 - 26.4.
/// </summary>
public sealed class LaunchTarget
{
    public string Id { get; set; } = "";
    public string MinecraftVersion { get; set; } = "";
    public LoaderKind Loader { get; set; }
    public string? LoaderVersion { get; set; }
    public string DisplayName => Loader == LoaderKind.Vanilla
        ? MinecraftVersion
        : $"{MinecraftVersion} + {Loader} {LoaderVersion}".Trim();
}

/// <summary>
/// A named player profile. Each profile owns a separate game directory so mods,
/// resource packs, shader packs and saves never collide between profiles.
/// </summary>
public sealed class Profile
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string TargetId { get; set; } = "";
    public int MaxMemoryMb { get; set; } = 3072;
    public string? JvmArgs { get; set; }
    public string? GameDirName { get; set; }

    public string GameDirNameOrDefault() => string.IsNullOrWhiteSpace(GameDirName) ? Id : GameDirName;
}

/// <summary>
/// Runtime settings persisted between sessions. Nothing here contains account
/// secrets; tokens are stored separately and encrypted by the auth layer.
/// </summary>
public sealed class LauncherSettings
{
    public string? LastProfileId { get; set; }
    public bool TelemetryOptIn { get; set; }
    public bool KeepLauncherOpen { get; set; } = true;
    public string Language { get; set; } = "de";
    public int DownloadConcurrency { get; set; } = 8;
}
