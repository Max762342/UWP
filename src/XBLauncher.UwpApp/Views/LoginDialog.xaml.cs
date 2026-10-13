using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using XBLauncher.Core.Auth;
using XBLauncher.UwpApp.Services;

namespace XBLauncher.UwpApp.Views
{
    /// <summary>
    /// Runs the Microsoft device-code sign-in: shows the code to enter on another
    /// device, polls until the user finishes, then completes the Xbox/Minecraft
    /// token exchange and verifies ownership. On success the session is stored in
    /// AppServices and the dialog returns true.
    /// </summary>
    public sealed partial class LoginDialog : Page
    {
        private CancellationTokenSource _cts;
        private bool _done;

        public MinecraftSession Result { get; private set; }

        public LoginDialog()
        {
            this.InitializeComponent();
            Loaded += LoginDialog_Loaded;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _cts = new CancellationTokenSource();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            _cts?.Cancel();
        }

        private async void LoginDialog_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var auth = AppServices.Instance.Auth;
                StatusText.Text = "Fordere Gerätecode an...";
                var dev = await auth.RequestDeviceCodeAsync(_cts.Token);

                CodeCard.Visibility = Visibility.Visible;
                UriText.Text = dev.VerificationUri;
                UserCodeText.Text = dev.UserCode;
                StatusText.Text = dev.Message ?? "Öffne die Adresse und gib den Code ein.";

                var token = await auth.PollForTokenAsync(dev,
                    s => _ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => StatusText.Text = s),
                    _cts.Token);

                StatusText.Text = "Tausche Token...";
                var session = await auth.CompleteSignInAsync(token, null, _cts.Token);

                AppServices.Instance.Session = session;
                Result = session;
                _done = true;

                _ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    if (Frame.CanGoBack) Frame.GoBack();
                });
            }
            catch (OperationCanceledException)
            {
                // user cancelled; nothing to do
            }
            catch (Exception ex)
            {
                StatusText.Text = "Anmeldung fehlgeschlagen: " + ex.Message;
                LoginProgress.IsIndeterminate = false;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            if (Frame.CanGoBack) Frame.GoBack();
        }
    }
}