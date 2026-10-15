using System.Text.Json;

namespace XBLauncher.Core.Auth;

public sealed partial class MinecraftAuthService
{
    /// <summary>
    /// Starts the device-code flow via the official Minecraft Launcher client.
    /// Uses the login.live.com OAuth2 device-code endpoints (response_type=device_code)
    /// which issue user codes for the pre-authorized launcher client without
    /// requiring user consent. The user opens the returned URI on their
    /// phone/PC and enters the code.
    /// </summary>
    public async Task<DeviceCodeResponse> RequestDeviceCodeAsync(CancellationToken ct = default)
    {
        var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = ClientId,
            ["response_type"] = "device_code",
            ["scope"] = Scope
        });

        using var resp = await _http.PostAsync(
            "https://login.live.com/oauth20_connect.srf", body, ct)
            .ConfigureAwait(false);
        var text = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        // Parse manually with JsonDocument. .NET Native can strip the
        // [JsonPropertyName] reflection metadata, leaving auto-properties empty;
        // reading the fields directly avoids that entirely.
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
        var root = doc.RootElement;

        if (root.TryGetProperty("error", out var err) && err.ValueKind == JsonValueKind.String)
        {
            var desc = root.TryGetProperty("error_description", out var d) ? d.GetString() : null;
            throw new InvalidOperationException(
                "Device-Code Fehler: " + err.GetString() + (desc is null ? "" : " - " + desc));
        }

        var userCode = GetString(root, "user_code");
        var rawVerificationUri = GetString(root, "verification_uri");

        return new DeviceCodeResponse
        {
            DeviceCode = GetString(root, "device_code"),
            UserCode = userCode,
            // Build the verification URL: the /link landing page with the user
            // code pre-filled as a query parameter. This is what the QR code
            // encodes so scanning it opens the sign-in page directly.
            VerificationUri = BuildVerificationUri(rawVerificationUri, userCode),
            ExpiresIn = GetInt(root, "expires_in", 900),
            Interval = GetInt(root, "interval", 5),
            Message = GetString(root, "message")
        };
    }

    /// <summary>
    /// Builds the URL the user has to open on their phone/PC. Microsoft's
    /// device-code endpoint returns "https://www.microsoft.com/link"; we always
    /// append the user_code as a query parameter so the sign-in form is
    /// pre-filled and the user never has to type the code manually. This URL is
    /// also what the QR code encodes on the Xbox login screen.
    /// </summary>
    private static string BuildVerificationUri(string rawUri, string userCode)
    {
        // Always prefer Microsoft's /link landing page (as the user requested)
        // with the code pre-filled via ?user_code=.
        if (!string.IsNullOrWhiteSpace(userCode))
        {
            return LinkUri + "?user_code=" + Uri.EscapeDataString(userCode);
        }

        // No user code available (should not happen) -> fall back to whatever
        // Microsoft returned, or the plain /link page.
        if (string.IsNullOrWhiteSpace(rawUri))
            return LinkUri;

        return rawUri;
    }

    private static string GetString(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";

    private static int GetInt(JsonElement e, string name, int fallback) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : fallback;

    /// <summary>
    /// Polls the token endpoint until the user finishes signing in. Throws when
    /// the code expires or the user declines.
    /// </summary>
    public async Task<TokenResponse> PollForTokenAsync(DeviceCodeResponse deviceCode,
        Action<string>? status = null, CancellationToken ct = default)
    {
        var interval = Math.Max(3, deviceCode.Interval);
        var deadline = DateTimeOffset.UtcNow.AddSeconds(deviceCode.ExpiresIn);

        while (DateTimeOffset.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(TimeSpan.FromSeconds(interval), ct).ConfigureAwait(false);

            var body = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code",
                ["client_id"] = ClientId,
                ["device_code"] = deviceCode.DeviceCode
            });

            using var resp = await _http.PostAsync(
                "https://login.live.com/oauth20_token.srf", body, ct)
                .ConfigureAwait(false);
            var text = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            // Manual parse (see RequestDeviceCodeAsync) for .NET Native safety.
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
            var root = doc.RootElement;

            var accessToken = GetString(root, "access_token");
            if (!string.IsNullOrEmpty(accessToken))
            {
                return new TokenResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = GetString(root, "refresh_token"),
                    TokenType = GetString(root, "token_type"),
                    ExpiresIn = GetInt(root, "expires_in", 3600)
                };
            }

            // login.live.com MBI_SSL flow may return the token under a different
            // field name (e.g. "t" or "ticket") or as a bare token string without
            // the OAuth "access_token" JSON key. Check for these variants too.
            var altToken = GetString(root, "t");
            if (string.IsNullOrEmpty(altToken)) altToken = GetString(root, "ticket");
            if (string.IsNullOrEmpty(altToken)) altToken = GetString(root, "token");
            if (!string.IsNullOrEmpty(altToken))
            {
                return new TokenResponse
                {
                    AccessToken = altToken.StartsWith("t=") ? altToken : "t=" + altToken,
                    RefreshToken = GetString(root, "refresh_token"),
                    TokenType = GetString(root, "token_type"),
                    ExpiresIn = GetInt(root, "expires_in", 3600)
                };
            }

            var err = GetString(root, "error");
            if (err == "authorization_declined" || err == "expired_token" || err == "bad_verification_code")
            {
                var desc = GetString(root, "error_description");
                throw new InvalidOperationException($"Anmeldung fehlgeschlagen: {err} - {desc}");
            }

            // rfc8628#section-3.5: handle polling backoff.
            // - 'authorization_pending': keep polling at the same interval.
            // - 'slow_down': increase interval by 5 seconds.
            // - No error field (transient failure/timeout): double the interval.
            if (err == "slow_down")
            {
                interval += 5;
            }
            else if (err == "authorization_pending")
            {
                // Normal waiting state - keep polling at the same interval.
            }
            else if (string.IsNullOrEmpty(err))
            {
                // No error field but also no token -> likely a transient failure
                // (timeout, 5xx). Back off to be a polite poller.
                interval = Math.Min(interval * 2, 60);
            }

            status?.Invoke("Warte auf Anmeldung im Browser...");
        }

        throw new TimeoutException("Device-Code abgelaufen. Bitte erneut versuchen.");
    }
}
