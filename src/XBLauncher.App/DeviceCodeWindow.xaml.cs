using System.Windows;
using XBLauncher.Core.Auth;

namespace XBLauncher.App;

public partial class DeviceCodeWindow : Window
{
    public DeviceCodeWindow(DeviceCodeResponse code)
    {
        InitializeComponent();
        // Show the pre-filled devicelogin URL (returned by the auth service),
        // falling back to the working /devicelogin page - never the flaky
        // /link landing page that throws "Microsoft account is unavailable
        // from this site".
        UrlText.Text = string.IsNullOrWhiteSpace(code.VerificationUri)
            ? "https://microsoft.com/devicelogin"
            : code.VerificationUri;
        CodeText.Text = code.UserCode;
        // Xbox: no window chrome / no mouse, fill the screen.
        WindowStyle = WindowStyle.None;
        WindowState = WindowState.Maximized;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
    }
}
