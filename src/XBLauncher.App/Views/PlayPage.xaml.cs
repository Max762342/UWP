using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using XBLauncher.Core.Data;

namespace XBLauncher.App.Views;

public partial class PlayPage : UserControl
{
    private readonly MainWindow _main;

    public PlayPage(MainWindow main)
    {
        InitializeComponent();
        _main = main;

        ProfileBox.ItemsSource = main.Profiles;
        ProfileBox.DisplayMemberPath = nameof(Profile.Name);
        ProfileBox.SelectedItem = main.CurrentProfile;

        TargetBox.ItemsSource = main.Targets;
        TargetBox.DisplayMemberPath = nameof(LaunchTarget.DisplayName);
        TargetBox.SelectedItem = main.Targets.FirstOrDefault(t => t.Id == main.CurrentProfile.TargetId);

        VersionList.ItemsSource = main.Targets;
    }

    private void ProfileBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProfileBox.SelectedItem is Profile p)
        {
            _main.SetProfile(p);
            TargetBox.SelectedItem = _main.Targets.FirstOrDefault(t => t.Id == p.TargetId);
        }
    }

    private void Version_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: LaunchTarget t })
        {
            TargetBox.SelectedItem = t;
            _main.CurrentProfile.TargetId = t.Id;
            _main.SetProfile(_main.CurrentProfile);
        }
    }

    private void AddProfile_Click(object sender, RoutedEventArgs e)
    {
        var p = new Profile
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = $"Profil {_main.Profiles.Count + 1}",
            TargetId = "fabric-1.21.11"
        };
        _main.SetProfile(p);
        ProfileBox.ItemsSource = null;
        ProfileBox.ItemsSource = _main.Profiles;
        ProfileBox.SelectedItem = p;
    }

    private async void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        if (_main.Session is null)
        {
            MessageBox.Show("Bitte zuerst mit deinem Microsoft-Konto anmelden, das Minecraft: Java Edition besitzt.",
                "Anmeldung erforderlich", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (TargetBox.SelectedItem is not LaunchTarget target)
            return;

        PlayButton.IsEnabled = false;
        try
        {
            _main.CurrentProfile.TargetId = target.Id;
            _main.SetProfile(_main.CurrentProfile);

            var progress = new Progress<Core.Launch.InstallProgress>(p =>
                _main.SetStatus($"{p.Stage}: {p.Detail}", p.Fraction * 100));

            var plan = await _main.Launcher.BuildPlanAsync(target, _main.CurrentProfile, _main.Session, progress);
            _main.SetStatus("Starte Minecraft...", 100);

            var logFile = Path.Combine(Core.Data.Store.LogsDir, "mc_launch.log");
            var proc = Core.Launch.GameProcess.Start(plan, logFile);
            _main.SetStatus($"Minecraft gestartet (PID {proc.Id}).", 100);
        }
        catch (Exception ex)
        {
            _main.SetStatus("Fehler: " + ex.Message);
            MessageBox.Show(ex.Message, "Start fehlgeschlagen", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            PlayButton.IsEnabled = true;
        }
    }
}

