using System.Net.Http;
using System.Text.Json;

namespace XBLauncher.Core.Mojang;

/// <summary>
/// Thin async client for the official Mojang launcher metadata endpoints.
/// All network access for version metadata goes through here.
/// </summary>
public sealed class MojangClient : IDisposable
{
    public const string VersionManifestUrl =
        "https://launchermeta.mojang.com/mc/game/version_manifest_v2.json";

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true
    };

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

    public async Task<VersionManifest> GetVersionManifestAsync(CancellationToken ct = default)
    {
        using var resp = await _http.GetAsync(VersionManifestUrl, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        var manifest = await JsonSerializer.DeserializeAsync<VersionManifest>(stream, Json, ct)
            .ConfigureAwait(false);
        return manifest ?? throw new InvalidOperationException("Empty version manifest");
    }

    public async Task<VersionDetail> GetVersionDetailAsync(string url, CancellationToken ct = default)
    {
        using var resp = await _http.GetAsync(url, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        var detail = await JsonSerializer.DeserializeAsync<VersionDetail>(stream, Json, ct)
            .ConfigureAwait(false);
        return detail ?? throw new InvalidOperationException("Empty version detail");
    }

    public async Task<AssetIndex> GetAssetIndexAsync(string url, CancellationToken ct = default)
    {
        using var resp = await _http.GetAsync(url, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        var index = await JsonSerializer.DeserializeAsync<AssetIndex>(stream, Json, ct)
            .ConfigureAwait(false);
        return index ?? throw new InvalidOperationException("Empty asset index");
    }

    public HttpClient Http => _http;

    public void Dispose() => _http.Dispose();
}
