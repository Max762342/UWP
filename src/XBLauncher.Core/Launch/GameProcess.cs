using System.Diagnostics;
using XBLauncher.Core.Data;
using XBLauncher.Core.Java;

namespace XBLauncher.Core.Launch;

/// <summary>
/// Spawns Minecraft. Two paths:
///  - <see cref="Start"/>: classic child-process java.exe (desktop / WPF).
///  - <see cref="StartEmbedded"/>: in-process JVM via the xb_jvm bridge (UWP/Xbox),
///    where the AppContainer blocks CreateProcess for java.exe.
/// </summary>
public static class GameProcess
{
    /// <summary>
    /// Launches Minecraft by embedding the JVM in the current process. Returns the
    /// JNI bridge exit code (0 = success). Used on Xbox/UWP.
    /// </summary>
    public static Task<int> StartEmbedded(LaunchPlan plan, string logFile, CancellationToken ct = default)
    {
        var host = new EmbeddedGameHost(new Install.JavaProvisioner());

        // Assemble JVM options exactly like the child-process path.
        var jvmOptions = new List<string>
        {
            "-Xms512M",
            $"-Xmx{plan.MaxMemoryMb}M",
            $"-Djava.library.path={plan.NativesDir}",
        };
        jvmOptions.AddRange(plan.Arguments);

        var jreDir = plan.JavaExe is { Length: > 0 }
            ? Path.GetDirectoryName(Path.GetDirectoryName(plan.JavaExe)) ?? ""
            : "";

        return host.RunAsync(
            jreDir,
            plan.Classpath,
            plan.MainClass,
            jvmOptions,
            plan.GameArgs,
            logFile,
            ct);
    }

    public static Process Start(LaunchPlan plan, string logFile)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(logFile)!);

        var psi = new ProcessStartInfo
        {
            FileName = plan.JavaExe,
            WorkingDirectory = plan.WorkingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        var args = new System.Diagnostics.CommandArguments(psi);
        args.Add("-Xms512M");
        args.Add($"-Xmx{plan.MaxMemoryMb}M");
        foreach (var arg in plan.Arguments)
            args.Add(arg);
        args.Add(plan.MainClass);
        foreach (var arg in plan.GameArgs)
            args.Add(arg);

        foreach (var (key, value) in plan.EnvironmentVariables)
            psi.Environment[key] = value;

        var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };

        var writer = new StreamWriter(logFile, append: true) { AutoFlush = true };
        proc.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                lock (writer) writer.WriteLine("[OUT] " + e.Data);
        };
        proc.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                lock (writer) writer.WriteLine("[ERR] " + e.Data);
        };

        // Dispose the log writer once the process has fully exited and the
        // redirected output streams are drained, so the file is not locked forever.
        proc.Exited += (_, _) =>
        {
            try
            {
                proc.WaitForExit();
                lock (writer) writer.Dispose();
            }
            catch
            {
                // best effort – never let cleanup throw on a background thread
            }
        };

        proc.Start();
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();
        return proc;
    }
}
