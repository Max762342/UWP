using System.Windows;

namespace XBLauncher.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _ = Core.Data.Store.RootDir;
    }
}
