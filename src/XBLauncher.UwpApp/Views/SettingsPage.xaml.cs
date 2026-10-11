using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

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
            NameBox.Text = "Standard";
            RamSlider.Value = 3072;
            RamLabel.Text = "3072 MB";
        }

        private void RamSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            RamLabel.Text = $"{(int)e.NewValue} MB";
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            _main?.SetStatus("Profil gespeichert.", 100);
        }
    }
}