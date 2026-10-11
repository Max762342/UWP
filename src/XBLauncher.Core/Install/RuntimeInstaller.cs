using System.IO.Compression;
using System.Text.Json;
using XBLauncher.Core.Data;
using XBLauncher.Core.Launch;
using XBLauncher.Core.Mojang;

namespace XBLauncher.Core.Install;

/// <summary>
/// Downloads and verifies every official file a target needs and returns the
/// classpath + argument templates for the launch step.
/// Layout (all under %LOCALAPPDATA%\XB-Launcher):
///   runtime\libraries\...      shared libraries
///   runtime\assets\...         shared assets
///   runtime\natives\&lt;id&gt;\...    per-target extracted natives
///   instances\&lt;profile&gt;\...    per-profile game dir
/// </summary>
public sealed partial class RuntimeInstaller : IRuntimeInstaller, IDisposable
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    private readonly MojangClient Mojang;
    private readonly HttpClient Http;

    public RuntimeInstaller(MojangClient mojang, HttpClient? http = null)
    {
        Mojang = mojang;
        Http = http ?? mojang.Http;
    }

    private static string LibrariesDir => Path.Combine(Store.RuntimeDir, "libraries");
    private static string AssetsDir => Path.Combine(Store.RuntimeDir, "assets");
    private static string AssetsIndexesDir => Path.Combine(AssetsDir, "indexes");
    private static string VersionsDir => Path.Combine(Store.RuntimeDir, "versions");

    public void Dispose() { /* MojangClient is owned externally */ }
}
