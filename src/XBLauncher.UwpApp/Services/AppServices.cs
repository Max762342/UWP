using System;
using System.Threading.Tasks;
using XBLauncher.Core.Auth;
using XBLauncher.Core.Data;
using XBLauncher.Core.Install;
using XBLauncher.Core.Launch;
using XBLauncher.Core.Modrinth;
using XBLauncher.Core.Mojang;

namespace XBLauncher.UwpApp.Services
{
    /// <summary>
    /// Composition root for the launcher. Owns the single set of Core services
    /// (auth, installers, launch pipeline, Modrinth) and the signed-in session.
    /// Everything the UI needs goes through this class so lifetimes stay simple.
    /// </summary>
    public sealed class AppServices
    {
        private static AppServices _instance;
        public static AppServices Instance => _instance ?? (_instance = new AppServices());

        public MinecraftAuthService Auth { get; }
        public MojangClient Mojang { get; }
        public JavaProvisioner Java { get; }
        public RuntimeInstaller Installer { get; }
        public FabricInstaller Fabric { get; }
        public NeoForgeInstaller NeoForge { get; }
        public ModrinthClient Modrinth { get; }
        public ContentManager Content { get; }
        public GameLauncher Launcher { get; }

        /// <summary>The signed-in Minecraft session, or null when logged out.</summary>
        public MinecraftSession Session { get; set; }

        private AppServices()
        {
            Auth = new MinecraftAuthService();
            Mojang = new MojangClient();
            Java = new JavaProvisioner();
            Installer = new RuntimeInstaller(Mojang);
            Fabric = new FabricInstaller(Mojang.Http, Java);
            NeoForge = new NeoForgeInstaller(Mojang.Http, Java);
            Modrinth = new ModrinthClient();
            Content = new ContentManager(Modrinth);
            Launcher = new GameLauncher(Installer, Java, Fabric, NeoForge);
        }

        /// <summary>Loads the persisted profile list, creating a default when empty.</summary>
        public System.Collections.Generic.List<Profile> LoadProfiles()
        {
            var profiles = Store.Load(Store.ProfilesPath, () => new System.Collections.Generic.List<Profile>());
            if (profiles.Count == 0)
            {
                profiles.Add(new Profile
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Name = "Standard",
                    TargetId = "fabric-1.21.11",
                    MaxMemoryMb = 3072
                });
                Store.Save(Store.ProfilesPath, profiles);
            }
            return profiles;
        }

        public void SaveProfiles(System.Collections.Generic.List<Profile> profiles) =>
            Store.Save(Store.ProfilesPath, profiles);

        public LauncherSettings LoadSettings() =>
            Store.Load(Store.SettingsPath, () => new LauncherSettings());

        public void SaveSettings(LauncherSettings settings) =>
            Store.Save(Store.SettingsPath, settings);
    }
}