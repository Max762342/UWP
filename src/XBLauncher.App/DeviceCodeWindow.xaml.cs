using System.Windows;
using XBLauncher.Core.Auth;

namespace XBLauncher.App;

public partial class DeviceCodeWindow : Window
{
    public DeviceCodeWindow(DeviceCodeResponse code)
    {
        InitializeComponent();
        UrlText.Text = code.VerificationUri;
        CodeText.Text = code.UserCode;
        // Xbox: no window chrome / no mouse, fill the screen.
        WindowStyle = WindowStyle.None;
        WindowState = WindowState.Maximized;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
    }
}
