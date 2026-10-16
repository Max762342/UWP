using System.Text.Json;

namespace XBLauncher.Core.Mojang;

public sealed partial class MojangClient
{
    public async Task<VersionDetail> GetVersionDetailAsync(string url, CancellationToken ct = default)
    {
        using var resp = await _http.GetAsync(url, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        var text = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
        var root = doc.RootElement;

        var detail = new VersionDetail
        {
            Id = GetString(root, "id"),
            MainClass = GetString(root, "mainClass"),
            Assets = GetString(root, "assets"),
            LegacyArguments = root.TryGetProperty("minecraftArguments", out var la) && la.ValueKind == JsonValueKind.String ? la.GetString() : null
        };

        if (root.TryGetProperty("assetIndex", out var ai))
            detail.AssetIndex = new AssetIndexRef { Id = GetString(ai, "id"), Sha1 = GetString(ai, "sha1"), Size = GetLong(ai, "size"), TotalSize = GetLong(ai, "totalSize"), Url = GetString(ai, "url") };

        if (root.TryGetProperty("downloads", out var dl) && dl.ValueKind == JsonValueKind.Object)
            foreach (var prop in dl.EnumerateObject())
                detail.Downloads[prop.Name] = ParseDownloadArtifact(prop.Value);

        if (root.TryGetProperty("libraries", out var libs) && libs.ValueKind == JsonValueKind.Array)
        {
            foreach (var l in libs.EnumerateArray())
            {
                var lib = new Library { Name = GetString(l, "name") };
                if (l.TryGetProperty("downloads", out var ld))
                {
                    lib.Downloads = new LibraryDownloads();
                    if (ld.TryGetProperty("artifact", out var art)) lib.Downloads.Artifact = ParseDownloadArtifact(art);
                    if (ld.TryGetProperty("classifiers", out var cls) && cls.ValueKind == JsonValueKind.Object)
                    {
                        lib.Downloads.Classifiers = new Dictionary<string, DownloadArtifact>();
                        foreach (var c in cls.EnumerateObject()) lib.Downloads.Classifiers[c.Name] = ParseDownloadArtifact(c.Value);
                    }
                }
                if (l.TryGetProperty("natives", out var nat) && nat.ValueKind == JsonValueKind.Object)
                {
                    lib.Natives = new Dictionary<string, string>();
                    foreach (var n in nat.EnumerateObject()) lib.Natives[n.Name] = n.Value.ValueKind == JsonValueKind.String ? n.Value.GetString() ?? "" : "";
                }
                if (l.TryGetProperty("rules", out var rules) && rules.ValueKind == JsonValueKind.Array)
                {
                    lib.Rules = new List<Rule>();
                    foreach (var r in rules.EnumerateArray())
                    {
                        var rule = new Rule { Action = GetString(r, "action") };
                        if (r.TryGetProperty("os", out var os)) rule.Os = new OsRule { Name = GetString(os, "name") };
                        lib.Rules.Add(rule);
                    }
                }
                if (l.TryGetProperty("extract", out var ex))
                {
                    lib.Extract = new ExtractInfo();
                    if (ex.TryGetProperty("exclude", out var excl) && excl.ValueKind == JsonValueKind.Array)
                    {
                        lib.Extract.Exclude = new List<string>();
                        foreach (var e2 in excl.EnumerateArray()) if (e2.ValueKind == JsonValueKind.String) lib.Extract.Exclude.Add(e2.GetString() ?? "");
                    }
                }
                detail.Libraries.Add(lib);
            }
        }

        if (root.TryGetProperty("javaVersion", out var jv))
            detail.JavaVersion = new JavaVersionInfo { Component = GetString(jv, "component"), MajorVersion = jv.TryGetProperty("majorVersion", out var mv) && mv.ValueKind == JsonValueKind.Number ? mv.GetInt32() : 21 };

        if (root.TryGetProperty("arguments", out var args))
        {
            detail.Arguments = new GameArguments();
            if (args.TryGetProperty("game", out var g) && g.ValueKind == JsonValueKind.Array)
                foreach (var ge in g.EnumerateArray()) detail.Arguments.Game.Add(ge.Clone());
            if (args.TryGetProperty("jvm", out var j) && j.ValueKind == JsonValueKind.Array)
                foreach (var je in j.EnumerateArray()) detail.Arguments.Jvm.Add(je.Clone());
        }

        return detail;
    }
}