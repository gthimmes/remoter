using System.IO;
using System.Windows;
using System.Windows.Threading;
using Remoter.App.Theming;
using Remoter.Core.Security;
using Remoter.Core.Sessions;
using Remoter.Core.Storage;

namespace Remoter.App;

public partial class App : Application
{
    public ConnectionManager Connections { get; private set; } = null!;

    public ThemeManager Theme { get; private set; } = null!;

    /// <summary>Which saved connections have a live session, for the list's gutter dot.</summary>
    public static LiveSessionTracker LiveSessions { get; } = new();

    public static new App Current => (App)Application.Current;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var store = new ProfileStore(ProfileStore.DefaultPath);
        var credentials = new WindowsCredentialStore();
        Connections = new ConnectionManager(store, credentials);
        Connections.Load();

        // Paint the chosen theme before the first window exists, so it never flashes the default.
        var settingsStore = new SettingsStore(SettingsStore.DefaultPath);
        Theme = new ThemeManager(settingsStore, settingsStore.Load());
        Theme.Initialize();

        // Every window that keeps the system caption gets it painted to match, as it appears and
        // again whenever the theme changes. The main window supplies its own chrome and ignores this.
        EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent,
            new RoutedEventHandler((sender, _) => TitleBarTheme.Apply((Window)sender, Theme.IsDark)));
        Theme.Changed += (_, _) => TitleBarTheme.ApplyToAll(Theme.IsDark);
        OverlayScroll.Register();

        DispatcherUnhandledException += OnUnhandledException;

        var main = new MainWindow();
        main.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Theme?.Dispose();
        base.OnExit(e);
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

    /// <summary>
    /// Swaps the theme dictionary in place. It sits at index 0 of the merged dictionaries, so
    /// every DynamicResource colour reference re-resolves and repaints without a restart.
    /// </summary>
    public static void ApplyTheme(string name)
    {
        var dict = new ResourceDictionary
        {
            Source = new Uri($"Themes/{name}.xaml", UriKind.Relative),
        };
        Current.Resources.MergedDictionaries[0] = dict;
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
