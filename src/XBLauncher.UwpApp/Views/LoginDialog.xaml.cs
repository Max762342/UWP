using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
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
                // The auth service returns https://www.microsoft.com/link?user_code=XXX
                // (the code is pre-filled on that page). Fall back to the plain
                // /link page if no URL is available.
                UriText.Text = string.IsNullOrWhiteSpace(dev.VerificationUri)
                    ? "https://www.microsoft.com/link"
                    : dev.VerificationUri;
                UserCodeText.Text = string.IsNullOrWhiteSpace(dev.UserCode)
                    ? "(Code wird geladen...)"
                    : dev.UserCode;
                StatusText.Text = dev.Message ?? "Scanne den QR-Code oder gib den Code ein.";

                // Render a QR of the verification URL (with the user code already
                // embedded) so a phone camera opens the sign-in page directly.
                var target = string.IsNullOrWhiteSpace(dev.VerificationUri)
                    ? "https://www.microsoft.com/link"
                    : dev.VerificationUri;
                RenderQr(target);

                var token = await auth.PollForTokenAsync(dev,
                    s => _ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => StatusText.Text = s),
                    _cts.Token);

                StatusText.Text = "Tausche Token...";
                var session = await auth.CompleteSignInAsync(token, null, _cts.Token);

                AppServices.Instance.Session = session;
                Result = session;

                _ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    // Navigate to the main launcher after a successful sign-in.
                    Frame.Navigate(typeof(MainPage));
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

        private async void RenderQr(string content)
        {
            try
            {
                byte[] png = QrCode.ToPngBytes(content, 8);
                using (var ms = new System.IO.MemoryStream(png))
                {
                    var bmp = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
                    await bmp.SetSourceAsync(ms.AsRandomAccessStream());
                    QrImage.Source = bmp;
                }
            }
            catch
            {
                // If QR rendering fails the user can still use the text code.
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            StatusText.Text = "Anmeldung abgebrochen. Starte die App neu, um es erneut zu versuchen.";
        }
    }
}