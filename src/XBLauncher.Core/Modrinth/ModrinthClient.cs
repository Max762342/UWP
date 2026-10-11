using System.Net.Http;
using System.Text.Json;
using System.Web;

namespace XBLauncher.Core.Modrinth;

/// <summary>
/// The three hub surfaces of XB-Launcher map directly to Modrinth project types.
/// </summary>
public enum HubKind
{
    Mods,
    Shaders,
    ResourcePacks
}

/// <summary>
/// Client for the public Modrinth API (https://api.modrinth.com/v2).
/// Powers the Mods Hub, Shader Hub and Packs Hub. Modrinth is fully documented
/// and requires no API key for anonymous read access.
/// </summary>
public sealed partial class ModrinthClient : IDisposable
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;

    public ModrinthClient(HttpClient? http = null)
    {
        _http = http ?? new HttpClient(new SocketsHttpHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.All,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5)
        })
        {
            Timeout = TimeSpan.FromMinutes(2)
        };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("XB-Launcher/1.0 (contact: local)");
    }

    private static string ProjectType(HubKind hub) => hub switch
    {
        HubKind.Mods => "mod",
        HubKind.Shaders => "shader",
        HubKind.ResourcePacks => "resourcepack",
        _ => "mod"
    };
}
