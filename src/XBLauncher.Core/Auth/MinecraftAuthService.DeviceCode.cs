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
        var dev = JsonSerializer.Deserialize<DeviceCodeResponse>(text, _json)
                  ?? throw new InvalidOperationException("Device code response leer");
        return dev;
    }

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
            var token = JsonSerializer.Deserialize<TokenResponse>(text, _json);

            if (token?.AccessToken is not null)
                return token;

            if (token?.Error is "authorization_declined" or "expired_token" or "bad_verification_code")
                throw new InvalidOperationException($"Anmeldung fehlgeschlagen: {token.Error} - {token.ErrorDescription}");

            status?.Invoke("Warte auf Anmeldung im Browser...");
        }

        throw new TimeoutException("Device-Code abgelaufen. Bitte erneut versuchen.");
    }
}
