using System;
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
                UriText.Text = dev.VerificationUri;
                UserCodeText.Text = dev.UserCode;
                StatusText.Text = dev.Message ?? "Scanne den QR-Code oder gib den Code ein.";

                // Render the QR code of the verification URL.
                var target = string.IsNullOrWhiteSpace(dev.VerificationUri)
                    ? "https://microsoft.com/link"
                    : dev.VerificationUri;
                if (!string.IsNullOrWhiteSpace(dev.UserCode))
                    target += (target.Contains("?") ? "&" : "?") + "code=" + dev.UserCode;
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

        private void RenderQr(string content)
        {
            try
            {
                var matrix = QrCode.Generate(content);
                int n = matrix.GetLength(0);
                int scale = 4, quiet = 2;
                int size = (n + quiet * 2) * scale;
                var bmp = new Windows.UI.Xaml.Media.Imaging.WriteableBitmap(size, size);
                using (var stream = bmp.PixelBuffer.AsStream())
                {
                    var buf = new byte[size * size * 4];
                    for (int i = 0; i < buf.Length; i += 4) { buf[i] = 255; buf[i + 1] = 255; buf[i + 2] = 255; buf[i + 3] = 255; }
                    for (int y = 0; y < n; y++)
                        for (int x = 0; x < n; x++)
                        {
                            if (!matrix[y, x]) continue;
                            for (int sy = 0; sy < scale; sy++)
                                for (int sx = 0; sx < scale; sx++)
                                {
                                    int px = (x + quiet) * scale + sx;
                                    int py = (y + quiet) * scale + sy;
                                    int idx = (py * size + px) * 4;
                                    buf[idx] = 0; buf[idx + 1] = 0; buf[idx + 2] = 0; buf[idx + 3] = 255;
                                }
                        }
                    stream.Write(buf, 0, buf.Length);
                }
                bmp.Invalidate();
                QrImage.Source = bmp;
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