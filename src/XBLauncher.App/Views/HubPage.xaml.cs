using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using XBLauncher.Core.Data;
using XBLauncher.Core.Modrinth;

namespace XBLauncher.App.Views;

public partial class HubPage : UserControl
{
    private readonly MainWindow _main;
    private readonly HubKind _hub;

    public HubPage(MainWindow main, HubKind hub)
    {
        InitializeComponent();
        _main = main;
        _hub = hub;

        SearchBox.Text = hub switch
        {
            HubKind.Mods => "sodium",
            HubKind.Shaders => "complementary",
            HubKind.ResourcePacks => "faithful",
            _ => ""
        };

        Loaded += async (_, _) => await SearchAsync();
    }

    private async void Search_Click(object sender, RoutedEventArgs e) => await SearchAsync();

    private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            await SearchAsync();
    }

    private async Task SearchAsync()
    {
        try
        {
            _main.SetStatus($"Suche {_hub} auf Modrinth...", 20);
            var target = TargetCatalog.Find(_main.CurrentProfile.TargetId);
            var mc = target?.MinecraftVersion ?? "1.21.11";
            var loader = target?.Loader switch
            {
                LoaderKind.Fabric => "fabric",
                LoaderKind.NeoForge => "neoforge",
                LoaderKind.Forge => "forge",
                _ => null
            };

            var result = await _main.ModrinthApi.SearchAsync(_hub, SearchBox.Text.Trim(), mc, loader);
            ResultsList.ItemsSource = result.Hits;
            _main.SetStatus($"{result.Hits.Count} Ergebnisse gefunden.", 100);
        }
        catch (Exception ex)
        {
            _main.SetStatus("Suche fehlgeschlagen: " + ex.Message);
        }
    }

    private async void Install_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: ModrinthProject project })
            return;

        try
        {
            var target = TargetCatalog.Find(_main.CurrentProfile.TargetId)
                ?? throw new InvalidOperationException("Kein Ziel ausgewÃ¤hlt.");

            _main.SetStatus($"Installiere {project.Title}...", 10);
            var path = await _main.ContentManager.InstallAsync(_main.CurrentProfile, target, _hub, project,
                new Progress<double>(d => _main.SetStatus($"Installiere {project.Title}...", d * 100)));

            _main.SetStatus($"Installiert: {Path.GetFileName(path)}", 100);
            MessageBox.Show($"\"{project.Title}\" wurde installiert.\n\nDatei: {Path.GetFileName(path)}",
                "Installiert", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _main.SetStatus("Installation fehlgeschlagen: " + ex.Message);
            MessageBox.Show(ex.Message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}



