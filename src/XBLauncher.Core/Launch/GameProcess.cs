using System.Diagnostics;
using XBLauncher.Core.Data;

namespace XBLauncher.Core.Launch;

/// <summary>
/// Spawns Minecraft. Kept intentionally simple: assemble args, start the JVM,
/// stream stdout/stderr to the log.
/// </summary>
public static class GameProcess
{
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

        psi.ArgumentList.Add("-Xms512M");
        psi.ArgumentList.Add($"-Xmx{plan.MaxMemoryMb}M");
        foreach (var arg in plan.Arguments)
            psi.ArgumentList.Add(arg);
        psi.ArgumentList.Add(plan.MainClass);
        foreach (var arg in plan.GameArgs)
            psi.ArgumentList.Add(arg);

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
