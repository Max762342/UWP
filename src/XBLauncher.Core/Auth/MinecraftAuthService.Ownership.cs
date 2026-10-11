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
        var data = JsonSerializer.Deserialize<GameOwnershipResponse>(text, _json);

        var owns = data?.Items.Any(i =>
            i.Name is "product_minecraft" or "game_minecraft" or "minecraft_java"
            || i.Signature is not null) == true;

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
        return JsonSerializer.Deserialize<MinecraftProfile>(text, _json)
               ?? throw new InvalidOperationException("Minecraft-Profil fehlt");
    }
}
