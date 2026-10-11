using System.Text.Json;
using System.Web;

namespace XBLauncher.Core.Modrinth;

public sealed partial class ModrinthClient
{
    /// <summary>
    /// Search Modrinth for a hub. When <paramref name="query"/> is empty the
    /// result is the popular/recommended list for that project type.
    /// </summary>
    public async Task<ModrinthSearchResponse> SearchAsync(
        HubKind hub,
        string? query,
        string minecraftVersion,
        string? loader,
        int limit = 20,
        int offset = 0,
        CancellationToken ct = default)
    {
        var facets = new List<string>();
        facets.Add($"[\"project_type:{ProjectType(hub)}\"]");

        if (!string.IsNullOrWhiteSpace(minecraftVersion))
            facets.Add($"[\"versions:{minecraftVersion}\"]");

        if (hub == HubKind.Mods && !string.IsNullOrWhiteSpace(loader))
            facets.Add($"[\"categories:{loader}\"]");

        var qs = HttpUtility.ParseQueryString(string.Empty);
        qs["facets"] = "[" + string.Join(",", facets) + "]";
        qs["limit"] = limit.ToString();
        qs["offset"] = offset.ToString();
        qs["index"] = string.IsNullOrWhiteSpace(query) ? "downloads" : "relevance";
        if (!string.IsNullOrWhiteSpace(query))
            qs["query"] = query;

        var url = $"https://api.modrinth.com/v2/search?{qs}";
        using var resp = await _http.GetAsync(url, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        await using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        var result = await JsonSerializer.DeserializeAsync<ModrinthSearchResponse>(stream, Json, ct)
            .ConfigureAwait(false);
        return result ?? new ModrinthSearchResponse();
    }

    /// <summary>
    /// All versions of a project.
    /// </summary>
    public async Task<List<ModrinthVersion>> GetVersionsAsync(string projectId, CancellationToken ct = default)
    {
        var url = $"https://api.modrinth.com/v2/project/{Uri.EscapeDataString(projectId)}/version";
        using var resp = await _http.GetAsync(url, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        await using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        var versions = await JsonSerializer.DeserializeAsync<List<ModrinthVersion>>(stream, Json, ct)
            .ConfigureAwait(false);
        return versions ?? new List<ModrinthVersion>();
    }

    /// <summary>
    /// Pick the best version for a given Minecraft version + loader.
    /// Returns null when the project has no compatible build.
    /// </summary>
    public async Task<ModrinthVersion?> ResolveVersionAsync(
        string projectId,
        string minecraftVersion,
        string? loader,
        CancellationToken ct = default)
    {
        var versions = await GetVersionsAsync(projectId, ct).ConfigureAwait(false);
        foreach (var v in versions)
        {
            if (!v.GameVersions.Contains(minecraftVersion))
                continue;

            if (!string.IsNullOrWhiteSpace(loader) && v.Loaders.Count > 0)
            {
                var wanted = NormalizeLoader(loader!);
                if (!v.Loaders.Any(l => string.Equals(NormalizeLoader(l), wanted, StringComparison.OrdinalIgnoreCase)))
                    continue;
            }

            return v;
        }

        return null;
    }

    private static string NormalizeLoader(string loader) => loader.ToLowerInvariant();

    public HttpClient Http => _http;

    public void Dispose() => _http.Dispose();
}
