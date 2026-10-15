using System.Windows;
using System.Windows.Threading;

namespace EasyDiskManager;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnUnhandledException;
    }

    private static void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(e.Exception.Message, "Easy Disk Manager", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}
