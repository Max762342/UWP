using System.Windows;

namespace XBLauncher.Uwp;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var app = new global::XBLauncher.App.App();
        app.InitializeComponent();
        var window = new global::XBLauncher.App.MainWindow();
        app.MainWindow = window;
        window.Show();
        app.Run();
    }
}
