using System.Text.Json;
using System.Text.Json.Serialization;

namespace XBLauncher.Core.Data;

/// <summary>
/// Persisted Minecraft session (access token + refresh token + profile info).
/// Saved as JSON under the Store root so the user stays signed in across app
/// restarts. The refresh token is used to silently renew the access token when
/// it expires (via MinecraftAuthService.RefreshAsync).
/// </summary>
public sealed class PersistedSession
{
    [JsonPropertyName("playerName")] public string PlayerName { get; set; } = "";
    [JsonPropertyName("uuid")] public string Uuid { get; set; } = "";
    [JsonPropertyName("accessToken")] public string AccessToken { get; set; } = "";
    [JsonPropertyName("refreshToken")] public string RefreshToken { get; set; } = "";
    [JsonPropertyName("expiresAt")] public DateTimeOffset ExpiresAt { get; set; }
    [JsonPropertyName("ownsJavaEdition")] public bool OwnsJavaEdition { get; set; }
}

/// <summary>
/// Thread safe JSON persistence for profiles, settings and install state.
/// Everything lives under %LOCALAPPDATA%\XB-Launcher so it survives app updates
/// and is reachable when the app runs inside the MSIX/APPX container.
/// </summary>
public static class Store
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly object Gate = new();

    private static string ResolveRootDir()
    {
        // Under UWP/AppContainer (Xbox) the writable location is the package
        // LocalFolder. Resolve it via reflection so this netstandard2.0 assembly
        // does not take a hard dependency on Windows.winmd; fall back to
        // %LOCALAPPDATA% on desktop.
        try
        {
            var t = Type.GetType("Windows.Storage.ApplicationData, Windows, ContentType=WindowsRuntime");
            var current = t?.GetProperty("Current")?.GetValue(null);
            var local = current?.GetType().GetProperty("LocalFolder")?.GetValue(current);
            var path = local?.GetType().GetProperty("Path")?.GetValue(local) as string;
            if (!string.IsNullOrEmpty(path))
                return Path.Combine(path, "XB-Launcher");
        }
        catch
        {
            // Not running under UWP – fall through to the desktop path.
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "XB-Launcher");
    }

    public static string RootDir { get; } = ResolveRootDir();

    public static string ProfilesPath => Path.Combine(RootDir, "profiles.json");
    public static string SettingsPath => Path.Combine(RootDir, "settings.json");
    public static string CatalogPath => Path.Combine(RootDir, "catalog.json");
    public static string SessionPath => Path.Combine(RootDir, "session.json");
    public static string LogsDir => Path.Combine(RootDir, "logs");
    public static string InstancesDir => Path.Combine(RootDir, "instances");
    public static string RuntimeDir => Path.Combine(RootDir, "runtime");

    static Store()
    {
        Directory.CreateDirectory(RootDir);
        Directory.CreateDirectory(LogsDir);
        Directory.CreateDirectory(InstancesDir);
        Directory.CreateDirectory(RuntimeDir);
    }

    public static T Load<T>(string path, Func<T> fallback) where T : class
    {
        lock (Gate)
        {
            try
            {
                if (!File.Exists(path))
                    return fallback();
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<T>(json, JsonOptions) ?? fallback();
            }
            catch
            {
                return fallback();
            }
        }
    }

    public static void Save<T>(string path, T value)
    {
        lock (Gate)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(path, JsonSerializer.Serialize(value, JsonOptions));
        }
    }

    public static void Delete(string path)
    {
        lock (Gate)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }
    }

    public static string InstanceDir(Profile profile) =>
        Path.Combine(InstancesDir, Sanitize(profile.GameDirNameOrDefault()));

    public static string Sanitize(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = name.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
        return new string(chars);
    }
}
