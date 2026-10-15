using System.Windows;
using XBLauncher.Core.Auth;

namespace XBLauncher.App;

public partial class DeviceCodeWindow : Window
{
    public DeviceCodeWindow(DeviceCodeResponse code)
    {
        InitializeComponent();
        // The auth service returns https://www.microsoft.com/link?user_code=XXX
        // (the code is pre-filled on that page). Fall back to the plain /link page.
        UrlText.Text = string.IsNullOrWhiteSpace(code.VerificationUri)
            ? "https://www.microsoft.com/link"
            : code.VerificationUri;
        CodeText.Text = code.UserCode;
        // Xbox: no window chrome / no mouse, fill the screen.
        WindowStyle = WindowStyle.None;
        WindowState = WindowState.Maximized;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
    }
}
