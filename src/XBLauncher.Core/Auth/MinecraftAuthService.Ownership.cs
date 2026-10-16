using System.Text.Json;

namespace XBLauncher.Core.Auth;

public sealed partial class MinecraftAuthService
{
    /// <summary>
    /// Queries the official entitlement endpoint. Only a response that contains
    /// a product owned by this account is accepted.
    /// </summary>
    public async Task<OwnershipResult> VerifyOwnershipAsync(string mcAccessToken, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get,
            "https://api.minecraftservices.com/entitlements/mcstore");
        req.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", mcAccessToken);

        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
            return new OwnershipResult { OwnsJavaEdition = false, Detail = $"HTTP {(int)resp.StatusCode}" };

        var text = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        // Manual parse (see PostJsonAsync) for .NET Native safety.
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
        var root = doc.RootElement;

        if (!root.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
            return new OwnershipResult { OwnsJavaEdition = false, Detail = "Keine items-Liste in der Antwort" };

        var owns = false;
        foreach (var item in items.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object) continue;
            if (!item.TryGetProperty("name", out var name) || name.ValueKind != JsonValueKind.String) continue;
            var n = name.GetString();
            if (n == "product_minecraft" || n == "game_minecraft" || n == "minecraft_java")
            {
                owns = true;
                break;
            }
        }

        return new OwnershipResult
        {
            OwnsJavaEdition = owns,
            Detail = owns ? "Java Edition verifiziert" : "Keine Java-Edition-Lizenz gefunden"
        };
    }

    private async Task<MinecraftProfile> GetProfileAsync(string mcAccessToken, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get,
            "https://api.minecraftservices.com/minecraft/profile");
        req.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", mcAccessToken);

        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        var text = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        // Manual parse (see PostJsonAsync) for .NET Native safety.
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
        var root = doc.RootElement;

        return new MinecraftProfile
        {
            Id = GetString(root, "id"),
            Name = GetString(root, "name")
        };
    }
}
