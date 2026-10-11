using System.IO;
using System.Windows;
using System.Windows.Controls;
using XBLauncher.Core.Data;
using XBLauncher.Core.Modrinth;

namespace XBLauncher.App.Views;

public partial class SettingsPage : UserControl
{
    private readonly MainWindow _main;

    public SettingsPage(MainWindow main)
    {
        InitializeComponent();
        _main = main;

        NameBox.Text = main.CurrentProfile.Name;
        RamSlider.Value = main.CurrentProfile.MaxMemoryMb;
        RamLabel.Text = $"{main.CurrentProfile.MaxMemoryMb} MB";
        GameDirText.Text = Store.InstanceDir(main.CurrentProfile);

        RamSlider.ValueChanged += (_, _) => RamLabel.Text = $"{(int)RamSlider.Value} MB";

        RefreshSummary();
    }

    private void RefreshSummary()
    {
        var mods = _main.ContentManager.ListInstalled(_main.CurrentProfile, HubKind.Mods).Count;
        var shaders = _main.ContentManager.ListInstalled(_main.CurrentProfile, HubKind.Shaders).Count;
        var packs = _main.ContentManager.ListInstalled(_main.CurrentProfile, HubKind.ResourcePacks).Count;
        ContentSummary.Text = $"Mods: {mods}   Â·   Shader: {shaders}   Â·   Resource-Packs: {packs}";
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _main.CurrentProfile.Name = string.IsNullOrWhiteSpace(NameBox.Text) ? _main.CurrentProfile.Name : NameBox.Text.Trim();
        _main.CurrentProfile.MaxMemoryMb = (int)RamSlider.Value;
        _main.SetProfile(_main.CurrentProfile);
        MessageBox.Show("Profil gespeichert.", "Einstellungen", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        var dir = Store.InstanceDir(_main.CurrentProfile);
        Directory.CreateDirectory(dir);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = dir,
            UseShellExecute = true
        });
        RefreshSummary();
    }
}


