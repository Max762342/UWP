using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using XBLauncher.UwpApp.Services;

namespace XBLauncher.UwpApp.Views
{
    public sealed partial class SettingsPage : Page
    {
        private MainPage _main;

        public SettingsPage()
        {
            this.InitializeComponent();
            Loaded += SettingsPage_Loaded;
        }

        protected override void OnNavigatedTo(Windows.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is MainPage main)
            {
                _main = main;
            }
        }

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            var profiles = AppServices.Instance.LoadProfiles();
            var profile = profiles[0];
            NameBox.Text = profile.Name;
            RamSlider.Value = profile.MaxMemoryMb;
            RamLabel.Text = profile.MaxMemoryMb + " MB";
        }

        private void RamSlider_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            RamLabel.Text = $"{(int)e.NewValue} MB";
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var services = AppServices.Instance;
            var profiles = services.LoadProfiles();
            var profile = profiles[0];
            profile.Name = string.IsNullOrWhiteSpace(NameBox.Text) ? profile.Name : NameBox.Text;
            profile.MaxMemoryMb = (int)RamSlider.Value;
            services.SaveProfiles(profiles);
            _main?.SetStatus("Profil gespeichert.", 100);
        }
    }
}