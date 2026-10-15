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

        // The token from login.live.com/oauth20_token.srf (MBI_SSL flow) can come
        // in two forms:
        // 1. A plain OAuth access_token (JWT) -> pass as "d=<token>" to Xbox Live.
        // 2. An MBI_SSL ticket prefixed with "t=" -> pass as-is (Xbox Live expects
        //    the "t=" prefix for these tickets, NOT "d=").
        // Detect which format we received and build the RpsTicket accordingly.
        var rpsTicket = msToken.AccessToken.StartsWith("t=")
            ? msToken.AccessToken                    // already has "t=" prefix (MBI_SSL ticket)
            : "d=" + msToken.AccessToken;            // OAuth JWT -> prefix with "d="

        var xblBody = new
        {
            Properties = new
            {
                AuthMethod = "RPS",
                SiteName = "user.auth.xboxlive.com",
                RpsTicket = rpsTicket
            },
            RelyingParty = "http://auth.xboxlive.com",
            TokenType = "JWT"
        };
        var xbl = await PostJsonAsync<XboxAuthResponse>(
            "https://user.auth.xboxlive.com/user/authenticate", xblBody, ct).ConfigureAwait(false);

        var uhs = xbl.DisplayClaims?.Xui.FirstOrDefault()?.Uhs
                  ?? throw new InvalidOperationException("Xbox Live Token ohne User-Hash");

        var xstsBody = new
        {
            Properties = new
            {
                SandboxId = "RETAIL",
                UserTokens = new[] { xbl.Token }
            },
            RelyingParty = "rp://api.minecraftservices.com/",
            TokenType = "JWT"
        };
        var xsts = await PostJsonAsync<XstsResponse>(
            "https://xsts.auth.xboxlive.com/xsts/authorize", xstsBody, ct).ConfigureAwait(false);

        var mcBody = new { identityToken = $"XBL3.0 x={uhs};{xsts.Token}" };
        var mc = await PostJsonAsync<MinecraftLoginResponse>(
            "https://api.minecraftservices.com/authentication/login_with_xbox", mcBody, ct)
            .ConfigureAwait(false);

        // Ownership verification (mandatory, never skipped)
        var ownership = forcedOwnership ?? await VerifyOwnershipAsync(mc.AccessToken, ct).ConfigureAwait(false);
        if (!ownership.OwnsJavaEdition)
            throw new InvalidOperationException(
                "Dieses Konto besitzt Minecraft: Java Edition nicht. XB-Launcher startet keine nicht-lizenzierten Kopien.");

        var profile = await GetProfileAsync(mc.AccessToken, ct).ConfigureAwait(false);

        return new MinecraftSession
        {
            PlayerName = profile.Name,
            Uuid = profile.Id,
            AccessToken = mc.AccessToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(mc.ExpiresIn > 0 ? mc.ExpiresIn : 86400),
            OwnsJavaEdition = true
        };
    }

    private async Task<T> PostJsonAsync<T>(string url, object body, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(body);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var resp = await _http.PostAsync(url, content, ct).ConfigureAwait(false);
        var text = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"Auth-Fehler {(int)resp.StatusCode}: {text}");
        return JsonSerializer.Deserialize<T>(text, _json)
               ?? throw new InvalidOperationException($"Leere Antwort von {url}");
    }
}
