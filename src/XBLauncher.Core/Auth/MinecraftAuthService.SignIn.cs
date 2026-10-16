using System.Text;
using System.Text.Json;

namespace XBLauncher.Core.Auth;

public sealed partial class MinecraftAuthService
{
    /// <summary>
    /// Exchanges the Microsoft token for Xbox Live, XSTS and finally a Minecraft
    /// Services access token, then verifies that the account owns Java Edition.
    /// </summary>
    public async Task<MinecraftSession> CompleteSignInAsync(TokenResponse msToken,
        OwnershipResult? forcedOwnership = null, CancellationToken ct = default)
    {
        if (msToken.AccessToken is null)
            throw new InvalidOperationException("Microsoft-Token fehlt");

        // BanditLauncher/JavaUWP style: the OAuth access_token is passed to Xbox
        // Live as an RPS ticket with the "d=" prefix (standard OAuth JWT flow).
        var xblBody = new
        {
            Properties = new
            {
                AuthMethod = "RPS",
                SiteName = "user.auth.xboxlive.com",
                RpsTicket = "d=" + msToken.AccessToken
            },
            RelyingParty = "http://auth.xboxlive.com",
            TokenType = "JWT"
        };
        var xblJson = await PostJsonAsync(
            "https://user.auth.xboxlive.com/user/authenticate", xblBody, ct).ConfigureAwait(false);

        // Manual parse (like the device-code flow) for .NET Native safety - the
        // [JsonPropertyName] reflection metadata can be stripped by the compiler.
        var xblToken = GetString(xblJson, "Token");
        var uhs = ExtractUhs(xblJson)
                  ?? throw new InvalidOperationException("Xbox Live Token ohne User-Hash");

        var xstsBody = new
        {
            Properties = new
            {
                SandboxId = "RETAIL",
                UserTokens = new[] { xblToken }
            },
            RelyingParty = "rp://api.minecraftservices.com/",
            TokenType = "JWT"
        };
        var xstsJson = await PostJsonAsync(
            "https://xsts.auth.xboxlive.com/xsts/authorize", xstsBody, ct).ConfigureAwait(false);
        var xstsToken = GetString(xstsJson, "Token");

        var mcBody = new { identityToken = $"XBL3.0 x={uhs};{xstsToken}" };
        var mcJson = await PostJsonAsync(
            "https://api.minecraftservices.com/authentication/login_with_xbox", mcBody, ct)
            .ConfigureAwait(false);
        var mcAccessToken = GetString(mcJson, "access_token");
        var mcExpiresIn = GetInt(mcJson, "expires_in", 86400);

        // Ownership verification (mandatory, never skipped)
        var ownership = forcedOwnership ?? await VerifyOwnershipAsync(mcAccessToken, ct).ConfigureAwait(false);
        if (!ownership.OwnsJavaEdition)
            throw new InvalidOperationException(
                "Dieses Konto besitzt Minecraft: Java Edition nicht. XB-Launcher startet keine nicht-lizenzierten Kopien.");

        var profile = await GetProfileAsync(mcAccessToken, ct).ConfigureAwait(false);

        return new MinecraftSession
        {
            PlayerName = profile.Name,
            Uuid = profile.Id,
            AccessToken = mcAccessToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(mcExpiresIn > 0 ? mcExpiresIn : 86400),
            OwnsJavaEdition = true
        };
    }

    /// <summary>
    /// Extracts the user hash (uhs) from the DisplayClaims.xui[0].uhs field of an
    /// Xbox Live / XSTS auth response, using manual JsonDocument parsing (safe
    /// under .NET Native where [JsonPropertyName] reflection may be stripped).
    /// </summary>
    private static string? ExtractUhs(JsonElement root)
    {
        if (!root.TryGetProperty("DisplayClaims", out var dc)) return null;
        if (!dc.TryGetProperty("xui", out var xuiArr)) return null;
        if (xuiArr.ValueKind != JsonValueKind.Array || xuiArr.GetArrayLength() == 0) return null;
        var first = xuiArr[0];
        return first.TryGetProperty("uhs", out var uhs) && uhs.ValueKind == JsonValueKind.String
            ? uhs.GetString()
            : null;
    }

    private async Task<JsonElement> PostJsonAsync(string url, object body, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(body);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var resp = await _http.PostAsync(url, content, ct).ConfigureAwait(false);
        var text = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"Auth-Fehler {(int)resp.StatusCode}: {text}");
        var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
        return doc.RootElement.Clone();  // Clone so it survives the using-dispose
    }
}
