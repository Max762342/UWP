using XBLauncher.Core.Data;

namespace XBLauncher.Core.Launch;

/// <summary>
/// Holds everything needed to spawn the Minecraft process.
/// </summary>
public sealed class LaunchPlan
{
    public required string JavaExe { get; init; }
    public required string WorkingDirectory { get; init; }
    public required List<string> Arguments { get; init; }
    public required List<string> GameArgs { get; init; }
    public required Dictionary<string, string> EnvironmentVariables { get; init; }
    public required string MainClass { get; init; }
    public string? AccessToken { get; init; }
    public string? PlayerName { get; init; }
    public string? Uuid { get; init; }
    public int MaxMemoryMb { get; init; } = 3072;
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
    public required string GameDir { get; init; }
    public required string NativesDir { get; init; }
    public required List<string> Classpath { get; init; }
    public required string MainClass { get; init; }
    public required List<string> JvmArgs { get; init; }
    public required List<string> GameArgs { get; init; }
    public required string AssetsDir { get; init; }
    public required string AssetIndexId { get; init; }
    public int JavaMajor { get; init; } = 21;
}

public sealed record InstallProgress(string Stage, double Fraction, string Detail);
