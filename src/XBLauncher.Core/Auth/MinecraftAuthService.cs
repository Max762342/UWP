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
    // Public client id that supports the OAuth2 device-code flow. The classic
    // Minecraft launcher id (00000000402b5328) only works for the authorization
    // code flow, not device code; this Azure public client issues user codes.
    private const string ClientId = "1950a258-227b-4e31-a9cf-717495945fc2";
    private const string Scope = "XboxLive.signin offline_access";

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
