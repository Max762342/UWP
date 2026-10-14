using System.Runtime.InteropServices;

namespace XBLauncher.Core.Java;

/// <summary>
/// P/Invoke surface over the native <c>xb_jvm.dll</c> bridge. The bridge loads
/// a HotSpot <c>jvm.dll</c>, creates the embedded JVM via JNI_CreateJavaVM and
/// calls a static <c>main(String[])</c>. Keeping the JNIEnv function-table
/// navigation in C (where jni.h guarantees the layout) avoids fragile vtable
/// index math in managed code.
/// </summary>
internal static class JniBridge
{
    private const string Dll = "xb_jvm";

    /// <summary>
    /// Loads <paramref name="jvmDllPath"/>, boots an embedded JVM with the given
    /// -D/-X options and invokes <c>main</c> on <paramref name="mainClass"/>.
    /// Returns 0 on success, or a negative JNI error code. The JVM runs on a
    /// dedicated native thread and blocks until the game exits.
    /// </summary>
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public static extern int XbLaunchGame(
        string jvmDllPath,
        string jreDir,
        string classpath,
        string mainClass,
        string[] jvmOptions,
        int jvmOptionCount,
        string[] gameArgs,
        int gameArgCount,
        string logFile);

    /// <summary>True while a game is running in-process.</summary>
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern int XbIsGameRunning();

    /// <summary>
    /// Requests the embedded game to stop. Best effort; the JVM cannot be
    /// restarted after a run (HotSpot allows one VM per process), so the host
    /// process should exit after the game ends.
    /// </summary>
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern void XbRequestStop();

    /// <summary>Version string of the bridge for diagnostics.</summary>
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr XbBridgeVersion();

    public static string BridgeVersion()
    {
        var p = XbBridgeVersion();
        return p == IntPtr.Zero ? "" : Marshal.PtrToStringAnsi(p) ?? "";
    }
}

