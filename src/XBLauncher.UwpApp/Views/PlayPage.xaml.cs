using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using XBLauncher.Core.Data;

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
            // Load profiles
            _profiles = XBLauncher.Core.Data.Store.Load(
                XBLauncher.Core.Data.Store.ProfilesPath, 
                () => new List<Profile>());

            if (_profiles.Count == 0)
            {
                _profiles.Add(new Profile
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Name = "Standard",
                    TargetId = "fabric-1.21.11",
                    MaxMemoryMb = 3072
                });
            }

            // Load targets
            _targets = XBLauncher.Core.Data.TargetCatalog.Build().ToList();

            // Populate UI
            ProfileBox.ItemsSource = _profiles.Select(p => p.Name).ToList();
            ProfileBox.SelectedIndex = 0;

            TargetBox.ItemsSource = _targets.Select(t => t.DisplayName).ToList();
            TargetBox.SelectedIndex = 0;

            VersionList.ItemsSource = _targets;

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

                var target = _targets[TargetBox.SelectedIndex];
                _main?.SetStatus($"Starte Minecraft {target.DisplayName}...", 30);

                // Simuliere Ladevorgang
                await System.Threading.Tasks.Task.Delay(1000);

                _main?.SetStatus("Hinweis: Spielstart benötigt die native App. Konfiguration wurde gespeichert.", 100);
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
    }
}