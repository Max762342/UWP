using System.Net.Http;
using System.Text.Json;

namespace XBLauncher.Core.Mojang;

/// <summary>
/// Thin async client for the official Mojang launcher metadata endpoints.
/// All JSON parsing uses manual JsonDocument parsing (NOT JsonSerializer with
/// [JsonPropertyName]) because .NET Native strips that reflection metadata.
/// </summary>
public sealed partial class MojangClient : IDisposable
{
    public const string VersionManifestUrl =
        "https://launchermeta.mojang.com/mc/game/version_manifest_v2.json";

    private readonly HttpClient _http;

    public MojangClient(HttpClient? http = null)
    {
        _http = http ?? new HttpClient(new SocketsHttpHandler
        {
            AutomaticDecompression = (System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate),
            PooledConnectionLifetime = TimeSpan.FromMinutes(5)
        })
        {
            Timeout = TimeSpan.FromMinutes(5)
        };
        if (!_http.DefaultRequestHeaders.UserAgent.Any())
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("XB-Launcher/1.0");
    }

    internal static string GetString(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";

    internal static long GetLong(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt64() : 0;

    public async Task<VersionManifest> GetVersionManifestAsync(CancellationToken ct = default)
    {
        using var resp = await _http.GetAsync(VersionManifestUrl, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        var text = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
        var root = doc.RootElement;

        var manifest = new VersionManifest();
        if (root.TryGetProperty("latest", out var latest))
            manifest.Latest = new LatestVersions { Release = GetString(latest, "release"), Snapshot = GetString(latest, "snapshot") };
        if (root.TryGetProperty("versions", out var versions) && versions.ValueKind == JsonValueKind.Array)
        {
            foreach (var v in versions.EnumerateArray())
                manifest.Versions.Add(new VersionEntry { Id = GetString(v, "id"), Type = GetString(v, "type"), Url = GetString(v, "url"), Time = GetString(v, "time"), ReleaseTime = GetString(v, "releaseTime"), Sha1 = GetString(v, "sha1") });
        }
        return manifest;
    }

    internal static DownloadArtifact ParseDownloadArtifact(JsonElement e) => new()
    {
        Sha1 = GetString(e, "sha1"), Size = GetLong(e, "size"), Url = GetString(e, "url")
    };

    public async Task<AssetIndex> GetAssetIndexAsync(string url, CancellationToken ct = default)
    {
        using var resp = await _http.GetAsync(url, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        var text = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
        var root = doc.RootElement;

        var index = new AssetIndex();
        if (root.TryGetProperty("objects", out var objs) && objs.ValueKind == JsonValueKind.Object)
        {
            foreach (var o in objs.EnumerateObject())
                index.Objects[o.Name] = new AssetObject { Hash = GetString(o.Value, "hash"), Size = GetLong(o.Value, "size") };
        }
        return index;
    }

    public HttpClient Http => _http;
    public void Dispose() => _http.Dispose();
}