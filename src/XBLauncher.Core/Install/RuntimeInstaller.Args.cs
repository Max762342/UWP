using System.Text.Json;
using XBLauncher.Core.Mojang;

namespace XBLauncher.Core.Install;

public sealed partial class RuntimeInstaller
{
    private static IEnumerable<string> FlattenArgs(List<JsonElement> elements)
    {
        foreach (var el in elements)
        {
            if (el.ValueKind == JsonValueKind.String)
            {
                yield return el.GetString()!;
            }
            else if (el.ValueKind == JsonValueKind.Object)
            {
                if (!RulesAllow(el))
                    continue;
                if (el.TryGetProperty("value", out var value))
                {
                    if (value.ValueKind == JsonValueKind.String)
                        yield return value.GetString()!;
                    else if (value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var v in value.EnumerateArray())
                            if (v.ValueKind == JsonValueKind.String)
                                yield return v.GetString()!;
                    }
                }
            }
        }
    }

    private static bool RulesAllow(JsonElement arg)
    {
        if (!arg.TryGetProperty("rules", out var rules) || rules.ValueKind != JsonValueKind.Array)
            return true;

        var allowed = false;
        foreach (var rule in rules.EnumerateArray())
        {
            var action = rule.TryGetProperty("action", out var a) ? a.GetString() : "allow";
            var osOk = true;
            if (rule.TryGetProperty("os", out var os) && os.ValueKind == JsonValueKind.Object)
            {
                if (os.TryGetProperty("name", out var osName))
                {
                    var name = osName.GetString();
                    osOk = name is null || name == "windows";
                }
            }

            if (osOk)
                allowed = string.Equals(action, "allow", StringComparison.OrdinalIgnoreCase);
        }

        return allowed;
    }

    private static bool IsAllowedOnOs(Library lib)
    {
        if (lib.Rules is null || lib.Rules.Count == 0)
            return true;

        var allowed = false;
        foreach (var rule in lib.Rules)
        {
            var osOk = rule.Os is null || rule.Os.Name == "windows";
            if (osOk)
                allowed = string.Equals(rule.Action, "allow", StringComparison.OrdinalIgnoreCase);
        }

        return allowed;
    }
}
