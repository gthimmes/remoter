using System.Windows;
using System.Windows.Threading;
using Remoter.Core.Security;
using Remoter.Core.Storage;

namespace Remoter.App;

public partial class App : Application
{
    public ConnectionManager Connections { get; private set; } = null!;

    public static new App Current => (App)Application.Current;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var store = new ProfileStore(ProfileStore.DefaultPath);
        var credentials = new WindowsCredentialStore();
        Connections = new ConnectionManager(store, credentials);
        Connections.Load();

        DispatcherUnhandledException += OnUnhandledException;

        var main = new MainWindow();
        main.Show();
    }

    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            "Something went wrong:\n\n" + e.Exception.Message,
            "Remoter", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}
