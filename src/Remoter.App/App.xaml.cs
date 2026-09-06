using System.IO;
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
        var log = LogError(e.Exception);
        MessageBox.Show(
            "Something went wrong:\n\n" + e.Exception.Message +
            (log is null ? "" : "\n\nDetails were written to " + log),
            "Remoter", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    /// <summary>Appends the full exception to a log beside the app data. Returns its path.</summary>
    private static string? LogError(Exception exception)
    {
        try
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Remoter", "errors.log");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.AppendAllText(path, $"{DateTimeOffset.Now:u}{Environment.NewLine}{exception}{Environment.NewLine}{Environment.NewLine}");
            return path;
        }
        catch
        {
            return null;
        }
    }
}
