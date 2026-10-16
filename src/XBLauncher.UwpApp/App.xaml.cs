using System;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace XBLauncher.UwpApp
{
    sealed partial class App : Application
    {
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            if (rootFrame == null)
            {
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // Sign-in always comes first: land on the login screen (with the
                    // Voxel logo + QR code). MainPage is reached after a successful
                    // sign-in via LoginDialog's GoBack(), or by skipping when a session
                    // already exists.

                    // Try to restore the persisted session so the user stays signed
                    // in across app restarts (like BanditLauncher's PasswordVault).
                    var persisted = XBLauncher.UwpApp.Services.AppServices.Instance.LoadPersistedSession();
                    if (persisted != null)
                        XBLauncher.UwpApp.Services.AppServices.Instance.Session = persisted;

                    if (XBLauncher.UwpApp.Services.AppServices.Instance.Session != null)
                        rootFrame.Navigate(typeof(MainPage), e.Arguments);
                    else
                        rootFrame.Navigate(typeof(XBLauncher.UwpApp.Views.LoginDialog), e.Arguments);
                }
                Window.Current.Activate();
            }
        }

        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            deferral.Complete();
        }
    }
}