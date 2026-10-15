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
    // The official Minecraft Launcher first-party title client. This client is
    // pre-authorized by Microsoft for the XboxLive sign-in resource, so it does
    // NOT require user consent and is exempt from the "first party application"
    // consent restriction that kills other public clients (e.g. Azure PowerShell).
    // Scope must be the MBI_SSL title scope that the launcher client is registered for.
    private const string ClientId = "00000000402b5328";
    private const string Scope = "service::user.auth.xboxlive.com::MBI_SSL";

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
