using System.Text.Json;
using System.Text.Json.Serialization;

namespace XBLauncher.Core.Data;

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

    public static string RootDir { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "XB-Launcher");

    public static string ProfilesPath => Path.Combine(RootDir, "profiles.json");
    public static string SettingsPath => Path.Combine(RootDir, "settings.json");
    public static string CatalogPath => Path.Combine(RootDir, "catalog.json");
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

    public static string InstanceDir(Profile profile) =>
        Path.Combine(InstancesDir, Sanitize(profile.GameDirNameOrDefault()));

    public static string Sanitize(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = name.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
        return new string(chars);
    }
}
