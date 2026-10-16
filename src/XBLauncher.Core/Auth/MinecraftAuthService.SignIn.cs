using System.Text;
using System.Text.Json;

namespace XBLauncher.Core.Auth;

public sealed partial class MinecraftAuthService
{
    /// <summary>
    /// Escapes a string for safe inclusion in a JSON string literal. Same as
    /// BanditLauncher (JavaUWP) JsonEscape in http_client.cpp.
    /// </summary>
    private static string JsonEscape(string value)
    {
        var sb = new StringBuilder(value.Length + 8);
        foreach (var c in value)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '"': sb.Append("\\\""); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                    else sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Exchanges the Microsoft token for Xbox Live, XSTS and Minecraft Services.
    /// 1:1 port of BanditLauncher BuildMinecraftAuth.
    /// </summary>
    public async Task<MinecraftSession> CompleteSignInAsync(TokenResponse msToken,
        OwnershipResult? forcedOwnership = null, CancellationToken ct = default)
    {
        if (msToken.AccessToken is null)
            throw new InvalidOperationException("Microsoft-Token fehlt");

        var xblPayload =
            "{\"Properties\":{\"AuthMethod\":\"RPS\",\"SiteName\":\"user.auth.xboxlive.com\",\"RpsTicket\":\"d=" +
            JsonEscape(msToken.AccessToken) +
            "\"},\"RelyingParty\":\"http://auth.xboxlive.com\",\"TokenType\":\"JWT\"}";
        var xblJson = await PostJsonRawAsync(
            "https://user.auth.xboxlive.com/user/authenticate", xblPayload, ct).ConfigureAwait(false);

        var xblToken = GetString(xblJson, "Token");
        var uhs = ExtractUhs(xblJson);
        if (string.IsNullOrEmpty(xblToken) || string.IsNullOrEmpty(uhs))
            throw new InvalidOperationException("Xbox Live auth response was missing token fields.");

        var xstsPayload =
            "{\"Properties\":{\"SandboxId\":\"RETAIL\",\"UserTokens\":[\"" +
            JsonEscape(xblToken) +
            "\"]},\"RelyingParty\":\"rp://api.minecraftservices.com/\",\"TokenType\":\"JWT\"}";
        var xstsJson = await PostJsonRawAsync(
            "https://xsts.auth.xboxlive.com/xsts/authorize", xstsPayload, ct).ConfigureAwait(false);
        var xstsToken = GetString(xstsJson, "Token");
        if (string.IsNullOrEmpty(xstsToken))
            throw new InvalidOperationException("XSTS response was missing token fields.");

        var identity = "XBL3.0 x=" + uhs + ";" + xstsToken;
        var mcPayload = "{\"identityToken\":\"" + JsonEscape(identity) + "\"}";
        var mcJson = await PostJsonRawAsync(
            "https://api.minecraftservices.com/authentication/login_with_xbox", mcPayload, ct)
            .ConfigureAwait(false);
        var mcAccessToken = GetString(mcJson, "access_token");
        var mcExpiresIn = GetInt(mcJson, "expires_in", 86400);
        if (string.IsNullOrEmpty(mcAccessToken))
            throw new InvalidOperationException("Minecraft login response did not include access_token.");

        var ownership = forcedOwnership ?? await VerifyOwnershipAsync(mcAccessToken, ct).ConfigureAwait(false);
        if (!ownership.OwnsJavaEdition)
            throw new InvalidOperationException(
                "Dieses Konto besitzt Minecraft: Java Edition nicht.");

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

    private static string? ExtractUhs(JsonElement root)
    {
        if (!root.TryGetProperty("DisplayClaims", out var dc)) return null;
        if (!dc.TryGetProperty("xui", out var xuiArr)) return null;
        if (xuiArr.ValueKind != JsonValueKind.Array) return null;
        foreach (var entry in xuiArr.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object) continue;
            if (entry.TryGetProperty("uhs", out var uhs) && uhs.ValueKind == JsonValueKind.String)
                return uhs.GetString();
        }
        return null;
    }

    private async Task<JsonElement> PostJsonRawAsync(string url, string jsonBody, CancellationToken ct)
    {
        using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        using var resp = await _http.PostAsync(url, content, ct).ConfigureAwait(false);
        var text = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"Auth-Fehler {(int)resp.StatusCode} von {url}: {text}");
        var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
        return doc.RootElement.Clone();
    }
}