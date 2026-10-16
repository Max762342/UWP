using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace XBLauncher.Core.Auth;

/// <summary>
/// Implements the official Microsoft device-code sign-in (the same flow the
/// Xbox mobile app uses) followed by Xbox Live + XSTS + Minecraft Services
/// token exchange and an explicit ownership verification of Minecraft: Java
/// Edition. There is intentionally no offline, cracked or bypass path: a
/// launcher session always requires a real account that owns the game.
/// </summary>
public sealed partial class MinecraftAuthService : IDisposable
{
    // BanditLauncher (JavaUWP) public client id - registered as a public client
    // with device-code flow support and XboxLive.signin + offline_access scopes.
    // This is a standard third-party OAuth client (NOT first-party), so users can
    // consent to it during sign-in. Source: https://github.com/veroxsity/JavaUWP
    private const string ClientId = "c36a9fb6-4f2a-41ff-90bd-ae7cc92031eb";
    private const string Scope = "XboxLive.signin offline_access";

    /// <summary>
    /// The verification URI the user has to open on their phone/PC. Microsoft's
    /// device-code endpoint returns "https://www.microsoft.com/link"; we pass
    /// the user_code as a query parameter so the code is pre-filled on that
    /// page (this is what the QR code encodes as well).
    /// </summary>
    private const string LinkUri = "https://www.microsoft.com/link";

    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public MinecraftAuthService(HttpClient? http = null)
    {
        _http = http ?? new HttpClient(new SocketsHttpHandler
        {
            AutomaticDecompression = (System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate)
        })
        {
            Timeout = TimeSpan.FromMinutes(2)
        };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("XB-Launcher/1.0");
    }

    public void Dispose() => _http.Dispose();
}
