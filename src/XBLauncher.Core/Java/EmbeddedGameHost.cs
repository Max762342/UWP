using System.Runtime.InteropServices;
using XBLauncher.Core.Data;
using XBLauncher.Core.Install;

namespace XBLauncher.Core.Java;

/// <summary>
/// Launches Minecraft by embedding the JVM in the current process (via the
/// native xb_jvm bridge) instead of spawning java.exe. This is the path that
/// works inside the UWP AppContainer on Xbox, where CreateProcess for an
/// arbitrary Win32 java.exe is not permitted but LoadLibrary + JNI are.
/// </summary>
public sealed class EmbeddedGameHost
{
    private readonly JavaProvisioner _java;

    public EmbeddedGameHost(JavaProvisioner java) => _java = java;

    /// <summary>
    /// Boots the embedded JVM and invokes the game's main. Blocks until the game
    /// exits. Returns 0 on success; a negative value is a JNI/bridge error code.
    /// </summary>
    public async Task<int> RunAsync(
        string jreDir,
        string classpath,
        string mainClass,
        IReadOnlyList<string> jvmOptions,
        IReadOnlyList<string> gameArgs,
        string logFile,
        CancellationToken ct = default)
    {
        var jvmDll = ResolveJvmDll(jreDir);
        if (jvmDll is null)
            throw new InvalidOperationException(
                $"jvm.dll nicht gefunden unter {jreDir}. Das JRE ist nicht vollständig installiert.");

        // The bridge runs on its own native thread and blocks; keep the managed
        // call off the UI/async context.
        return await Task.Run(() =>
        {
            // Safe path handling - GetDirectoryName can return null for root paths
            var logDir = Path.GetDirectoryName(logFile);
            if (!string.IsNullOrEmpty(logDir))
                Directory.CreateDirectory(logDir);
            
            var opt = jvmOptions.ToArray();
            var args = gameArgs.ToArray();
            return JniBridge.XbLaunchGame(
                jvmDll, jreDir, classpath, mainClass,
                opt, opt.Length, args, args.Length, logFile);
        }, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Finds jvm.dll inside a provisioned JRE. Accepts both the classic
    /// <c>bin\server\jvm.dll</c> and <c>bin\client\jvm.dll</c> layouts.
    /// </summary>
    public static string? ResolveJvmDll(string jreDir)
    {
        var candidates = new[]
        {
            Path.Combine(jreDir, "bin", "server", "jvm.dll"),
            Path.Combine(jreDir, "bin", "client", "jvm.dll"),
            Path.Combine(jreDir, "lib", "server", "jvm.dll"),
        };
        return candidates.FirstOrDefault(File.Exists);
    }

    /// <summary>True when the native bridge is present and loadable.</summary>
    public static bool BridgeAvailable()
    {
        try
        {
            return !string.IsNullOrEmpty(JniBridge.BridgeVersion());
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }
}