using XBLauncher.Core.Auth;
using XBLauncher.Core.Data;
using XBLauncher.Core.Install;

namespace XBLauncher.Core.Launch;

/// <summary>
/// Orchestrates install + argument assembly + process start for one profile.
/// </summary>
public sealed class GameLauncher
{
    private readonly IRuntimeInstaller _installer;
    private readonly JavaProvisioner _java;
    private readonly FabricInstaller? _fabric;
    private readonly NeoForgeInstaller? _neoforge;

    public GameLauncher(IRuntimeInstaller installer, JavaProvisioner java,
        FabricInstaller? fabric = null, NeoForgeInstaller? neoforge = null)
    {
        _installer = installer;
        _java = java;
        _fabric = fabric;
        _neoforge = neoforge;
    }

    public async Task<LaunchPlan> BuildPlanAsync(LaunchTarget target, Profile profile,
        MinecraftSession session, IProgress<InstallProgress>? progress, CancellationToken ct = default)
    {
        var ready = await _installer.EnsureInstalledAsync(target, profile, progress, ct)
            .ConfigureAwait(false);

        var classpath = new List<string>(ready.Classpath);

        // loader specific libraries
        if (target.Loader == LoaderKind.Fabric && _fabric is not null && target.LoaderVersion is not null)
        {
            var jars = await _fabric.InstallAsync(target.MinecraftVersion, target.LoaderVersion,
                Path.Combine(Store.RuntimeDir, "libraries"), progress, ct).ConfigureAwait(false);
            classpath.AddRange(jars);
        }
        else if (target.Loader == LoaderKind.NeoForge && _neoforge is not null && target.LoaderVersion is not null)
        {
            var jars = await _neoforge.InstallAsync(target.MinecraftVersion, target.LoaderVersion,
                progress, ct).ConfigureAwait(false);
            classpath.AddRange(jars);
        }

        var jvmArgs = new List<string>(ready.JvmArgs);
        // classpath last so it wins over any earlier -cp
        jvmArgs.Add("-cp");
        jvmArgs.Add(string.Join(Path.PathSeparator.ToString(), classpath));

        var gameArgs = ArgumentSubstitutor.Substitute(ready.GameArgs, target, profile, ready, session,
            ready.AssetsDir);

        var javaExe = await _java.EnsureJavaAsync(ready.JavaMajor, ct: ct).ConfigureAwait(false);

        return new LaunchPlan
        {
            JavaExe = javaExe,
            WorkingDirectory = ready.GameDir,
            Arguments = jvmArgs,
            GameArgs = gameArgs,
            MainClass = ready.MainClass,
            AccessToken = session.AccessToken,
            PlayerName = session.PlayerName,
            Uuid = session.Uuid,
            MaxMemoryMb = profile.MaxMemoryMb,
            EnvironmentVariables = new Dictionary<string, string>()
        };
    }
}
