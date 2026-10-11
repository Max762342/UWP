using System.Text.Json;
using System.Text.Json.Serialization;

namespace XBLauncher.Core.Mojang;

/// <summary>
/// Root of https://launchermeta.mojang.com/mc/game/version_manifest_v2.json.
/// </summary>
public sealed class VersionManifest
{
    [JsonPropertyName("latest")] public LatestVersions Latest { get; set; } = new();
    [JsonPropertyName("versions")] public List<VersionEntry> Versions { get; set; } = new();
}

public sealed class LatestVersions
{
    [JsonPropertyName("release")] public string Release { get; set; } = "";
    [JsonPropertyName("snapshot")] public string Snapshot { get; set; } = "";
}

public sealed class VersionEntry
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("url")] public string Url { get; set; } = "";
    [JsonPropertyName("time")] public string Time { get; set; } = "";
    [JsonPropertyName("releaseTime")] public string ReleaseTime { get; set; } = "";
    [JsonPropertyName("sha1")] public string Sha1 { get; set; } = "";
}

/// <summary>
/// The per-version JSON (the "version file"). Only the fields XB-Launcher needs
/// are modelled; unknown JSON is ignored.
/// </summary>
public sealed class VersionDetail
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("mainClass")] public string MainClass { get; set; } = "";
    [JsonPropertyName("assets")] public string Assets { get; set; } = "";
    [JsonPropertyName("assetIndex")] public AssetIndexRef? AssetIndex { get; set; }
    [JsonPropertyName("downloads")] public Dictionary<string, DownloadArtifact> Downloads { get; set; } = new();
    [JsonPropertyName("libraries")] public List<Library> Libraries { get; set; } = new();
    [JsonPropertyName("arguments")] public GameArguments? Arguments { get; set; }
    [JsonPropertyName("minecraftArguments")] public string? LegacyArguments { get; set; }
    [JsonPropertyName("javaVersion")] public JavaVersionInfo? JavaVersion { get; set; }
}

public sealed class JavaVersionInfo
{
    [JsonPropertyName("component")] public string Component { get; set; } = "jre-21";
    [JsonPropertyName("majorVersion")] public int MajorVersion { get; set; } = 21;
}

public sealed class AssetIndexRef
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("sha1")] public string Sha1 { get; set; } = "";
    [JsonPropertyName("size")] public long Size { get; set; }
    [JsonPropertyName("totalSize")] public long TotalSize { get; set; }
    [JsonPropertyName("url")] public string Url { get; set; } = "";
}

public sealed class DownloadArtifact
{
    [JsonPropertyName("sha1")] public string Sha1 { get; set; } = "";
    [JsonPropertyName("size")] public long Size { get; set; }
    [JsonPropertyName("url")] public string Url { get; set; } = "";
}

public sealed class GameArguments
{
    [JsonPropertyName("game")] public List<JsonElement> Game { get; set; } = new();
    [JsonPropertyName("jvm")] public List<JsonElement> Jvm { get; set; } = new();
}

public sealed class Library
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("downloads")] public LibraryDownloads? Downloads { get; set; }
    [JsonPropertyName("natives")] public Dictionary<string, string>? Natives { get; set; }
    [JsonPropertyName("rules")] public List<Rule>? Rules { get; set; }
    [JsonPropertyName("extract")] public ExtractInfo? Extract { get; set; }
}

public sealed class LibraryDownloads
{
    [JsonPropertyName("artifact")] public DownloadArtifact? Artifact { get; set; }
    [JsonPropertyName("classifiers")] public Dictionary<string, DownloadArtifact>? Classifiers { get; set; }
}

public sealed class ExtractInfo
{
    [JsonPropertyName("exclude")] public List<string>? Exclude { get; set; }
}

public sealed class Rule
{
    [JsonPropertyName("action")] public string Action { get; set; } = "allow";
    [JsonPropertyName("os")] public OsRule? Os { get; set; }
}

public sealed class OsRule
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
}

/// <summary>
/// Asset index JSON: object map of "objects" -> hash/sizes.
/// </summary>
public sealed class AssetIndex
{
    [JsonPropertyName("objects")] public Dictionary<string, AssetObject> Objects { get; set; } = new();
}

public sealed class AssetObject
{
    [JsonPropertyName("hash")] public string Hash { get; set; } = "";
    [JsonPropertyName("size")] public long Size { get; set; }
}
