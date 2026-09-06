using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Remoter.App.Views;
using Remoter.Core.Models;
using Remoter.Core.Sessions;

namespace Remoter.App;

/// <summary>
/// Holds many live sessions as tabs in one window. Every session view stays parented in
/// <c>SessionHost</c> for the life of its tab and only its visibility changes, so switching tabs
/// never tears down a connection.
/// </summary>
public partial class SessionHostWindow : Window
{
    private static readonly List<SessionHostWindow> OpenWindows = new();

    private readonly ObservableCollection<SessionTab> _tabs = new();
    private bool _fullScreen;
    private bool _forceClose;
    private WindowState _preFullScreenState = WindowState.Normal;

    public SessionHostWindow()
    {
        InitializeComponent();
        Tabs.ItemsSource = _tabs;
        OpenWindows.Add(this);
        Activated += (_, _) => Promote();
        StateChanged += OnWindowStateChanged;
        PreviewKeyDown += OnPreviewKeyDown;
        Closing += OnClosing;
        Closed += (_, _) => OpenWindows.Remove(this);
    }

    /// <summary>
    /// Opens a session as a new tab. It docks into the host window you used last, unless
    /// <paramref name="newWindow"/> asks for a window of its own.
    /// </summary>
    public static SessionView OpenSession(ConnectionProfile profile, string? password, bool newWindow = false)
    {
        var host = newWindow || OpenWindows.Count == 0 ? new SessionHostWindow() : OpenWindows[^1];
        return host.AddSession(profile, password);
    }

    private SessionView AddSession(ConnectionProfile profile, string? password)
    {
        var view = new SessionView(profile, password)
        {
            TabTitle = SessionTitles.Unique(profile.DisplayName, _tabs.Select(t => t.Title)),
            Visibility = Visibility.Hidden,
        };
        var tab = new SessionTab(view);

        view.StateChanged += (_, _) => { tab.RefreshStatus(); UpdateStatusBar(); };
        view.LogChanged += (_, _) => UpdateStatusBar();
        view.CloseRequested += (_, _) => CloseTab(tab);
        view.FullScreenRequested += (_, fs) => { if (SelectedTab == tab) ApplyFullScreen(fs); };

        // Hidden views still take part in layout, so a background tab keeps its real size and does
        // not have to renegotiate the remote resolution when you come back to it.
        SessionHost.Children.Add(view);
        _tabs.Add(tab);
        Tabs.SelectedItem = tab;

        Show();
        Activate();

        // Connect after layout, so the view has a size to ask the remote desktop for.
        Dispatcher.BeginInvoke(view.Start, DispatcherPriority.Loaded);
        UpdateTitle();
        return view;
    }

    private SessionTab? SelectedTab => Tabs.SelectedItem as SessionTab;

    private void Promote()
    {
        OpenWindows.Remove(this);
        OpenWindows.Add(this);
    }

    // ----- Tab selection -----

    private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(e.OriginalSource, Tabs))
            return;

        foreach (var tab in _tabs)
            tab.View.Visibility = tab == SelectedTab ? Visibility.Visible : Visibility.Hidden;

        // Full screen belongs to one session, so leave it when the user switches away.
        if (_fullScreen)
            ApplyFullScreen(false);

        UpdateTitle();
        UpdateStatusBar();
        SelectedTab?.View.FocusSession();
    }

    private void Tab_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Middle && sender is TabItem { DataContext: SessionTab tab })
        {
            e.Handled = true;
            CloseTab(tab);
        }
    }

    private void TabClose_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: SessionTab tab })
            CloseTab(tab);
    }

    private void CloseTab(SessionTab tab)
    {
        if (!_tabs.Contains(tab))
            return;

        var index = _tabs.IndexOf(tab);
        _tabs.Remove(tab);
        SessionHost.Children.Remove(tab.View);
        tab.View.Dispose();

        if (_tabs.Count == 0)
        {
            if (_fullScreen)
                ApplyFullScreen(false);
            _forceClose = true;
            Close();
            return;
        }

        Tabs.SelectedItem = _tabs[Math.Min(index, _tabs.Count - 1)];
        UpdateTitle();
        UpdateStatusBar();
    }

    // ----- Toolbar -----

    private void Disconnect_Click(object sender, RoutedEventArgs e) => CloseActiveTab();

    private void FullScreen_Click(object sender, RoutedEventArgs e) => ApplyFullScreen(!_fullScreen);

    private void Cad_Click(object sender, RoutedEventArgs e) => SelectedTab?.View.SendCtrlAltDel();

    private void Fit_Click(object sender, RoutedEventArgs e) => SelectedTab?.View.FitNow();

    private void CloseActiveTab()
    {
        if (SelectedTab is not { } tab)
            return;
        tab.View.Disconnect();
        CloseTab(tab);
    }

    // ----- Keyboard -----

    // These reach us only while the WPF chrome has focus. Once the remote desktop has the
    // keyboard, its own host sees the keys instead; clicking a tab always works.
    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        var ctrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
        var shift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);

        if (e.Key == Key.F11)
        {
            ApplyFullScreen(!_fullScreen);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && _fullScreen)
        {
            ApplyFullScreen(false);
            e.Handled = true;
        }
        else if (ctrl && e.Key == Key.W)
        {
            CloseActiveTab();
            e.Handled = true;
        }
        else if (ctrl && e.Key == Key.Tab)
        {
            Cycle(shift ? -1 : 1);
            e.Handled = true;
        }
        else if (ctrl && e.Key is >= Key.D1 and <= Key.D9)
        {
            var index = e.Key - Key.D1;
            if (index < _tabs.Count)
                Tabs.SelectedItem = _tabs[index];
            e.Handled = true;
        }
    }

    private void Cycle(int delta)
    {
        if (_tabs.Count < 2 || SelectedTab is not { } current)
            return;
        var next = (_tabs.IndexOf(current) + delta + _tabs.Count) % _tabs.Count;
        Tabs.SelectedItem = _tabs[next];
    }

    // ----- Full screen -----

    private void OnWindowStateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Maximized && !_fullScreen)
            ApplyFullScreen(true);
    }

    private void ApplyFullScreen(bool value)
    {
        if (_fullScreen == value)
            return;
        _fullScreen = value;

        if (value)
        {
            // Come back to a normal window afterwards; restoring to Maximized would immediately
            // re-enter full screen through OnWindowStateChanged.
            _preFullScreenState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState;
            Chrome.Visibility = Visibility.Collapsed;
            StatusBar.Visibility = Visibility.Collapsed;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            if (WindowState == WindowState.Maximized)
                WindowState = WindowState.Normal; // force a re-maximize so the taskbar is covered
            WindowState = WindowState.Maximized;
            Topmost = true;
        }
        else
        {
            Chrome.Visibility = Visibility.Visible;
            StatusBar.Visibility = Visibility.Visible;
            WindowStyle = WindowStyle.SingleBorderWindow;
            ResizeMode = ResizeMode.CanResize;
            Topmost = false;
            WindowState = _preFullScreenState;
        }
        SelectedTab?.View.SetFullScreen(value);
    }

    // ----- Chrome text -----

    private void UpdateTitle() => Title = SessionTitles.WindowTitle(SelectedTab?.Title, _tabs.Count);

    private void UpdateStatusBar()
    {
        StateText.Text = SelectedTab?.StatusText ?? "";
        LogText.Text = SelectedTab?.View.LastLog ?? "";
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!_forceClose && _tabs.Count > 1)
        {
            var result = MessageBox.Show(this,
                $"Disconnect all {_tabs.Count} sessions and close this window?",
                "Remoter", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                e.Cancel = true;
                return;
            }
        }

        foreach (var tab in _tabs.ToList())
            tab.View.Dispose();
        _tabs.Clear();
        SessionHost.Children.Clear();
    }
}
