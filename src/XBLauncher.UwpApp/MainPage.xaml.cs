using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using XBLauncher.UwpApp.Views;

namespace XBLauncher.UwpApp
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            Loaded += MainPage_Loaded;
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            ShowPlay();
        }

        public void SetStatus(string text, double percent = -1)
        {
            _ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                StatusText.Text = text;
                if (percent >= 0) StatusProgress.Value = percent;
            });
        }

        // Navigation
        private void NavPlay_Click(object sender, RoutedEventArgs e) => ShowPlay();
        private void NavMods_Click(object sender, RoutedEventArgs e) => ShowHub("mods");
        private void NavShaders_Click(object sender, RoutedEventArgs e) => ShowHub("shaders");
        private void NavPacks_Click(object sender, RoutedEventArgs e) => ShowHub("packs");
        private void NavSettings_Click(object sender, RoutedEventArgs e) => ShowSettings();

        private void ShowPlay()
        {
            PageTitle.Text = "Spielen";
            PageSubtitle.Text = "Wähle Minecraft-Version und Mod-Loader";
            PageFrame.Navigate(typeof(PlayPage), this);
        }

        private void ShowHub(string hubType)
        {
            PageTitle.Text = hubType switch
            {
                "mods" => "Mods Hub",
                "shaders" => "Shader Hub",
                "packs" => "Packs Hub",
                _ => "Hub"
            };
            PageSubtitle.Text = "Installiere Inhalte direkt aus Modrinth";
            PageFrame.Navigate(typeof(HubPage), new Tuple<MainPage, string>(this, hubType));
        }

        private void ShowSettings()
        {
            PageTitle.Text = "Einstellungen";
            PageSubtitle.Text = "Profil, RAM und Pfade";
            PageFrame.Navigate(typeof(SettingsPage), this);
        }
    }
}