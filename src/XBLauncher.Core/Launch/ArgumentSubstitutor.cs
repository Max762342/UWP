using XBLauncher.Core.Auth;
using XBLauncher.Core.Data;
using XBLauncher.Core.Launch;

namespace XBLauncher.Core.Launch;

/// <summary>
/// Replaces the ${placeholders} Mojang and the loaders use in their argument
/// templates with the concrete values of the current session/profile.
/// </summary>
public static class ArgumentSubstitutor
{
    public static List<string> Substitute(IEnumerable<string> template, LaunchTarget target,
        Profile profile, InstallReady ready, MinecraftSession session, string assetsDir)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["auth_player_name"] = session.PlayerName,
            ["version_name"] = target.MinecraftVersion,
            ["game_directory"] = ready.GameDir,
            ["assets_root"] = assetsDir,
            ["assets_index_name"] = ready.AssetIndexId,
            ["auth_uuid"] = session.Uuid,
            ["auth_access_token"] = session.AccessToken,
            ["clientid"] = "xb-launcher",
            ["auth_xuid"] = session.Uuid,
            ["user_type"] = "msa",
            ["version_type"] = "release",
            ["natives_directory"] = ready.NativesDir,
            ["launcher_name"] = "XB-Launcher",
            ["launcher_version"] = "1.0.0",
            ["classpath_separator"] = Path.PathSeparator.ToString(),
            ["user_properties"] = "{}",
            ["resolution_width"] = "1280",
            ["resolution_height"] = "720",
        };

        var result = new List<string>();
        foreach (var raw in template)
        {
            if (raw is "--demo" or "--demoMode")
                continue;

            var s = raw;
            foreach (var (key, value) in map)
                s = ReplaceIgnoreCase(s, "${" + key + "}", value);

            result.Add(s);
        }

        return result;
    }

    private static string ReplaceIgnoreCase(string input, string search, string replacement)
    {
        if (string.IsNullOrEmpty(search) || input is null)
            return input;
        var idx = input.IndexOf(search, StringComparison.OrdinalIgnoreCase);
        while (idx >= 0)
        {
            input = input.Substring(0, idx) + replacement + input.Substring(idx + search.Length);
            idx = input.IndexOf(search, idx + replacement.Length, StringComparison.OrdinalIgnoreCase);
        }
        return input;
    }
}
