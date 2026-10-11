using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using XBLauncher.App.Views;
using XBLauncher.Core.Auth;
using XBLauncher.Core.Data;
using XBLauncher.Core.Install;
using XBLauncher.Core.Launch;
using XBLauncher.Core.Modrinth;
using XBLauncher.Core.Mojang;

namespace XBLauncher.App;

public partial class MainWindow : Window
{
    private MinecraftSession? _session;
    private Profile _profile = null!;
    private List<Profile> _profiles = new();
    private List<LaunchTarget> _targets = new();

    private readonly MojangClient _mojang = new();
    private readonly ModrinthClient _modrinth = new();
    private readonly MinecraftAuthService _auth = new();
    private JavaProvisioner _java = null!;
    private RuntimeInstaller _installer = null!;
    private FabricInstaller _fabric = null!;
    private NeoForgeInstaller _neoforge = null!;
    private GameLauncher _launcher = null!;
    private ContentManager _content = null!;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
        Closed += (_, _) => { _mojang.Dispose(); _modrinth.Dispose(); _auth.Dispose(); };
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Xbox Dev Mode has no mouse/keyboard and no desktop window chrome.
        // Run fullscreen so the WPF surface fills the whole TV output.
        WindowStyle = WindowStyle.None;
        WindowState = WindowState.Maximized;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = 0;
        Top = 0;
        Width = SystemParameters.PrimaryScreenWidth;
        Height = SystemParameters.PrimaryScreenHeight;

        _java = new JavaProvisioner(_mojang.Http);
        _installer = new RuntimeInstaller(_mojang);
        _fabric = new FabricInstaller(_mojang.Http, _java);
        _neoforge = new NeoForgeInstaller(_mojang.Http, _java);
        _launcher = new GameLauncher(_installer, _java, _fabric, _neoforge);
        _content = new ContentManager(_modrinth);

        _targets = TargetCatalog.Build().ToList();
        _profiles = Store.Load(Store.ProfilesPath, () => new List<Profile>());

        if (_profiles.Count == 0)
        {
            var p = new Profile { Id = Guid.NewGuid().ToString("N"), Name = "Standard", TargetId = "fabric-1.21.11" };
            _profiles.Add(p);
            Store.Save(Store.ProfilesPath, _profiles);
        }

        _profile = _profiles.FirstOrDefault(p => p.Id == Store.Load(Store.SettingsPath, () => new LauncherSettings()).LastProfileId)
                   ?? _profiles[0];

        ShowPlay();

        // Give the Xbox controller keyboard focus so D-pad/A map to WPF
        // navigation without a mouse.
        NavPlay.Focus();
        Keyboard.Focus(NavPlay);
    }

    public Profile CurrentProfile => _profile;
    public IReadOnlyList<Profile> Profiles => _profiles;
    public IReadOnlyList<LaunchTarget> Targets => _targets;
    public MinecraftSession? Session => _session;
    public ContentManager ContentManager => _content;
    public GameLauncher Launcher => _launcher;
    public ModrinthClient ModrinthApi => _modrinth;

    public void SetStatus(string text, double percent = -1)
    {
        Dispatcher.Invoke(() =>
        {
            StatusText.Text = text;
            if (percent >= 0) StatusProgress.Value = percent;
        });
    }

    public void SetProfile(Profile p)
    {
        _profile = p;
        if (!_profiles.Any(x => x.Id == p.Id))
            _profiles.Add(p);
        Store.Save(Store.ProfilesPath, _profiles);
    }

    // ---- Navigation ----
    private void NavPlay_Click(object sender, RoutedEventArgs e) => ShowPlay();
    private void NavMods_Click(object sender, RoutedEventArgs e) => ShowHub(HubKind.Mods);
    private void NavShaders_Click(object sender, RoutedEventArgs e) => ShowHub(HubKind.Shaders);
    private void NavPacks_Click(object sender, RoutedEventArgs e) => ShowHub(HubKind.ResourcePacks);
    private void NavSettings_Click(object sender, RoutedEventArgs e) => ShowSettings();

    private void ShowPlay()
    {
        PageTitle.Text = "Spielen";
        PageSubtitle.Text = "WÃ¤hle Minecraft-Version und Mod-Loader";
        PageHost.Content = new PlayPage(this);
    }

    private void ShowHub(HubKind hub)
    {
        (PageTitle.Text, PageSubtitle.Text) = hub switch
        {
            HubKind.Mods => ("Mods Hub", "Installiere Mods direkt aus Modrinth"),
            HubKind.Shaders => ("Shader Hub", "Shader-Packs fÃ¼r mehr Grafik-QualitÃ¤t"),
            HubKind.ResourcePacks => ("Packs Hub", "Resource- und Textur-Packs"),
            _ => ("Hub", "")
        };
        PageHost.Content = new HubPage(this, hub);
    }

    private void ShowSettings()
    {
        PageTitle.Text = "Einstellungen";
        PageSubtitle.Text = "Profil, RAM und Pfade";
        PageHost.Content = new SettingsPage(this);
    }

    // ---- Auth ----
    private async void SignInButton_Click(object sender, RoutedEventArgs e)
    {
        DeviceCodeWindow? dlg = null;
        try
        {
            SignInButton.IsEnabled = false;
            SetStatus("Starte Microsoft-Anmeldung...");
            var code = await _auth.RequestDeviceCodeAsync();
            dlg = new DeviceCodeWindow(code);
            dlg.Owner = this;
            dlg.Show();

            var token = await _auth.PollForTokenAsync(code, s => SetStatus(s));

            SetStatus("Verifiziere Minecraft-Besitz...");
            var session = await _auth.CompleteSignInAsync(token);
            _session = session;

            UserText.Text = $"Angemeldet: {session.PlayerName}";
            SignInButton.Visibility = Visibility.Collapsed;
            SignOutButton.Visibility = Visibility.Visible;
            SetStatus($"Willkommen, {session.PlayerName}! Java Edition verifiziert.", 100);
        }
        catch (Exception ex)
        {
            SetStatus("Anmeldung fehlgeschlagen: " + ex.Message);
            MessageBox.Show(ex.Message, "Anmeldung", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            dlg?.Close();
            SignInButton.IsEnabled = true;
        }
    }

    private void SignOutButton_Click(object sender, RoutedEventArgs e)
    {
        _session = null;
        UserText.Text = "Nicht angemeldet";
        SignInButton.Visibility = Visibility.Visible;
        SignOutButton.Visibility = Visibility.Collapsed;
        SetStatus("Abgemeldet.");
    }
}

