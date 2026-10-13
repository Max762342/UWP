using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using XBLauncher.Core.Data;
using XBLauncher.UwpApp.Services;

namespace XBLauncher.UwpApp.Views
{
    public sealed partial class PlayPage : Page
    {
        private MainPage _main;
        private List<Profile> _profiles = new List<Profile>();
        private List<LaunchTarget> _targets = new List<LaunchTarget>();

        public PlayPage()
        {
            this.InitializeComponent();
            Loaded += PlayPage_Loaded;
        }

        protected override void OnNavigatedTo(Windows.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is MainPage main)
            {
                _main = main;
            }
        }

        private void PlayPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void LoadData()
        {
            // Load profiles via the shared service (creates a default if empty).
            _profiles = AppServices.Instance.LoadProfiles();

            // Load targets
            _targets = XBLauncher.Core.Data.TargetCatalog.Build().ToList();

            // Populate UI
            ProfileBox.ItemsSource = _profiles.Select(p => p.Name).ToList();
            ProfileBox.SelectedIndex = 0;

            TargetBox.ItemsSource = _targets.Select(t => t.DisplayName).ToList();
            TargetBox.SelectedIndex = 0;

            VersionList.ItemsSource = _targets;

            if (AppServices.Instance.Session != null)
                LoginButton.Content = "Angemeldet ✓";

            _main?.SetStatus("Bereit.", 0);
        }

        private void ProfileBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProfileBox.SelectedIndex >= 0 && ProfileBox.SelectedIndex < _profiles.Count)
            {
                _main?.SetStatus($"Profil: {_profiles[ProfileBox.SelectedIndex].Name}", 0);
            }
        }

        private void TargetBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TargetBox.SelectedIndex >= 0 && TargetBox.SelectedIndex < _targets.Count)
            {
                var target = _targets[TargetBox.SelectedIndex];
                _main?.SetStatus($"Ausgewählt: {target.DisplayName}", 0);
            }
        }

        private void AddProfile_Click(object sender, RoutedEventArgs e)
        {
            var newProfile = new Profile
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = $"Profil {_profiles.Count + 1}",
                TargetId = "fabric-1.21.11",
                MaxMemoryMb = 3072
            };

            _profiles.Add(newProfile);
            AppServices.Instance.SaveProfiles(_profiles);
            ProfileBox.ItemsSource = _profiles.Select(p => p.Name).ToList();
            ProfileBox.SelectedIndex = _profiles.Count - 1;
            _main?.SetStatus($"Profil \"{newProfile.Name}\" erstellt.", 0);
        }

        private void Version_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is Border border && border.DataContext is LaunchTarget target)
            {
                TargetBox.SelectedItem = target.DisplayName;
                _main?.SetStatus($"Ausgewählt: {target.DisplayName}", 0);
            }
        }

        private async void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            PlayButton.IsEnabled = false;

            try
            {
                if (TargetBox.SelectedIndex < 0 || TargetBox.SelectedIndex >= _targets.Count)
                {
                    _main?.SetStatus("Bitte wähle eine Version aus.", 0);
                    return;
                }

                var services = AppServices.Instance;

                // Auth gate: a real session that owns the game is required to launch.
                if (services.Session is null)
                {
                    _main?.SetStatus("Anmeldung erforderlich...", 5);
                    if (!await EnsureSignedInAsync())
                    {
                        _main?.SetStatus("Abgebrochen – bitte anmelden.", 0);
                        return;
                    }
                }

                var target = _targets[TargetBox.SelectedIndex];
                var profile = _profiles[ProfileBox.SelectedIndex >= 0 ? ProfileBox.SelectedIndex : 0];

                _main?.SetStatus($"Installiere Minecraft {target.DisplayName}...", 10);

                var progress = new Progress<XBLauncher.Core.Launch.InstallProgress>(p =>
                    _main?.SetStatus($"{p.Stage}: {p.Detail}", 10 + p.Fraction * 80));

                // Downloads client, assets, libraries, natives and returns the launch templates.
                var ready = await services.Installer.EnsureInstalledAsync(target, profile, progress);

                _main?.SetStatus($"Starte Minecraft {target.DisplayName}...", 95);

                var plan = await services.Launcher.BuildPlanAsync(target, profile, services.Session, progress);

                // On Xbox the AppContainer blocks spawning the Win32 java.exe, so this
                // step reports a clear, actionable error instead of a cryptic 0x80070002.
                try
                {
                    var logFile = System.IO.Path.Combine(XBLauncher.Core.Data.Store.LogsDir,
                        $"launch-{DateTime.Now:yyyyMMdd-HHmmss}.log");
                    var proc = XBLauncher.Core.Launch.GameProcess.Start(plan, logFile);
                    _main?.SetStatus($"Minecraft läuft (PID {proc.Id}). Log: {logFile}", 100);
                }
                catch (Exception launchEx)
                {
                    _main?.SetStatus(
                        "Spiel konnte nicht gestartet werden. Auf Xbox ist das Starten von Java (Win32) " +
                        "durch die AppContainer-Sandbox blockiert. Installation und Konfiguration sind OK. " +
                        "Details: " + launchEx.Message, 100);
                }
            }
            catch (Exception ex)
            {
                _main?.SetStatus($"Fehler: {ex.Message}", 0);
            }
            finally
            {
                PlayButton.IsEnabled = true;
            }
        }

        private async System.Threading.Tasks.Task<bool> EnsureSignedInAsync()
        {
            // Navigate to the login dialog and wait for it to complete.
            var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
            Frame.Navigated += OnLoginNavigatedBack;

            void OnLoginNavigatedBack(object sender, Windows.UI.Xaml.Navigation.NavigationEventArgs e)
            {
                if (e.SourcePageType == typeof(PlayPage))
                {
                    Frame.Navigated -= OnLoginNavigatedBack;
                    tcs.TrySetResult(AppServices.Instance.Session != null);
                }
            }

            Frame.Navigate(typeof(LoginDialog));
            return await tcs.Task;
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            await EnsureSignedInAsync();
            if (AppServices.Instance.Session != null)
            {
                LoginButton.Content = "Angemeldet ✓";
                _main?.SetStatus($"Angemeldet als {AppServices.Instance.Session.PlayerName}.", 100);
            }
        }
    }
}