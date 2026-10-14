using System.Text.Json;

namespace XBLauncher.Core.Auth;

public sealed partial class MinecraftAuthService
{
    /// <summary>
    /// Starts the device-code flow. The user opens the returned URI on their
    /// phone/PC and enters the code.
    /// </summary>
    public async Task<DeviceCodeResponse> RequestDeviceCodeAsync(CancellationToken ct = default)
    {
        var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = ClientId,
            ["scope"] = Scope
        });

        using var resp = await _http.PostAsync(
            "https://login.microsoftonline.com/consumers/oauth2/v2.0/devicecode", body, ct)
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

        return new DeviceCodeResponse
        {
            DeviceCode = GetString(root, "device_code"),
            UserCode = GetString(root, "user_code"),
            VerificationUri = GetString(root, "verification_uri"),
            ExpiresIn = GetInt(root, "expires_in", 900),
            Interval = GetInt(root, "interval", 5),
            Message = GetString(root, "message")
        };
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
                "https://login.microsoftonline.com/consumers/oauth2/v2.0/token", body, ct)
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

            var err = GetString(root, "error");
            if (err is "authorization_declined" or "expired_token" or "bad_verification_code")
            {
                var desc = GetString(root, "error_description");
                throw new InvalidOperationException($"Anmeldung fehlgeschlagen: {err} - {desc}");
            }

            status?.Invoke("Warte auf Anmeldung im Browser...");
        }

        throw new TimeoutException("Device-Code abgelaufen. Bitte erneut versuchen.");
    }
}
