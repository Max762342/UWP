using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using XBLauncher.Core.Modrinth;
using XBLauncher.UwpApp.Services;

namespace XBLauncher.UwpApp.Views
{
    public sealed partial class HubPage : Page
    {
        private MainPage _main;
        private string _hubType;
        private ModrinthClient _modrinth => AppServices.Instance.Modrinth;

        public HubPage()
        {
            this.InitializeComponent();
            Loaded += HubPage_Loaded;
        }

        protected override void OnNavigatedTo(Windows.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is Tuple<MainPage, string> param)
            {
                _main = param.Item1;
                _hubType = param.Item2;
            }
        }

        private async void HubPage_Loaded(object sender, RoutedEventArgs e)
        {
            await SearchAsync();
        }

        private async System.Threading.Tasks.Task SearchAsync()
        {
            try
            {
                _main?.SetStatus($"Suche {_hubType} auf Modrinth...", 20);

                var hub = _hubType switch
                {
                    "mods" => HubKind.Mods,
                    "shaders" => HubKind.Shaders,
                    "packs" => HubKind.ResourcePacks,
                    _ => HubKind.Mods
                };

                var query = SearchBox.Text.Trim();
                if (string.IsNullOrEmpty(query))
                {
                    query = _hubType switch
                    {
                        "mods" => "sodium",
                        "shaders" => "complementary",
                        "packs" => "faithful",
                        _ => ""
                    };
                }

                var result = await _modrinth.SearchAsync(hub, query, "1.21.11", "fabric");
                ResultsList.ItemsSource = result.Hits;
                _main?.SetStatus($"{result.Hits.Count} Ergebnisse gefunden.", 100);
            }
            catch (Exception ex)
            {
                _main?.SetStatus($"Suche fehlgeschlagen: {ex.Message}", 0);
            }
        }

        private async void Search_Click(object sender, RoutedEventArgs e)
        {
            await SearchAsync();
        }

        private async void Install_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ModrinthProject project)
            {
                try
                {
                    var services = AppServices.Instance;

                    // Install requires a signed-in profile to know the game dir.
                    var profiles = services.LoadProfiles();
                    var profile = profiles[0];
                    var target = XBLauncher.Core.Data.TargetCatalog.Find(profile.TargetId)
                                 ?? XBLauncher.Core.Data.TargetCatalog.Build()[0];

                    var hub = _hubType switch
                    {
                        "mods" => HubKind.Mods,
                        "shaders" => HubKind.Shaders,
                        "packs" => HubKind.ResourcePacks,
                        _ => HubKind.Mods
                    };

                    _main?.SetStatus($"Installiere {project.Title}...", 10);

                    var progress = new Progress<double>(p => _main?.SetStatus($"Installiere {project.Title}...", 10 + p * 80));
                    var dest = await services.Content.InstallAsync(profile, target, hub, project, progress);

                    _main?.SetStatus($"\"{project.Title}\" installiert nach {dest}", 100);
                }
                catch (Exception ex)
                {
                    _main?.SetStatus($"Fehler: {ex.Message}", 0);
                }
            }
        }
    }
}