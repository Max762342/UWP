using XBLauncher.Core.Data;

namespace XBLauncher.Core.Launch;

/// <summary>
/// Holds everything needed to spawn the Minecraft process.
/// </summary>
public sealed class LaunchPlan
{
    public string JavaExe { get; set; } = "";
    public string WorkingDirectory { get; set; } = "";
    public List<string> Arguments { get; set; } = new();
    public List<string> GameArgs { get; set; } = new();
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
    public string MainClass { get; set; } = "";
    public string? AccessToken { get; set; }
    public string? PlayerName { get; set; }
    public string? Uuid { get; set; }
    public int MaxMemoryMb { get; set; } = 3072;
}

/// <summary>
/// Interface for the runtime install step. The concrete installer downloads all
/// official game files; this interface keeps the launcher UI decoupled.
/// </summary>
public interface IRuntimeInstaller
{
    /// <summary>
    /// Ensures the given target is fully installed and returns the resolved
    /// version detail used to build the launch arguments.
    /// </summary>
    Task<InstallReady> EnsureInstalledAsync(LaunchTarget target, Profile profile,
        IProgress<InstallProgress>? progress, CancellationToken ct = default);
}

public sealed class InstallReady
{
    public string GameDir { get; set; } = "";
    public string NativesDir { get; set; } = "";
    public List<string> Classpath { get; set; } = new();
    public string MainClass { get; set; } = "";
    public List<string> JvmArgs { get; set; } = new();
    public List<string> GameArgs { get; set; } = new();
    public string AssetsDir { get; set; } = "";
    public string AssetIndexId { get; set; } = "";
    public int JavaMajor { get; set; } = 21;
}

public sealed record InstallProgress(string Stage, double Fraction, string Detail);
