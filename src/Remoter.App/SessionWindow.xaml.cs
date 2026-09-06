using System.Windows;
using System.Windows.Threading;
using Remoter.Core.Models;
using Remoter.Core.Sessions;
using Remoter.Rdp;

namespace Remoter.App;

public partial class SessionWindow : Window
{
    private readonly ConnectionProfile _profile;
    private readonly string? _password;
    private readonly RdpSessionControl _session;
    private readonly DispatcherTimer _resizeDebounce;
    private bool _connectStarted;
    private bool _userClosing;
    private bool _fullScreen;
    private WindowState _preFullScreenState = WindowState.Normal;
    private DisconnectInfo? _lastDisconnect;

    public SessionWindow(ConnectionProfile profile, string? password)
    {
        InitializeComponent();
        _profile = profile;
        _password = password;
        Title = $"{profile.DisplayName} - Remoter";

        _session = new RdpSessionControl();
        _session.StateChanged += (_, e) => Dispatcher.Invoke(() => OnStateChanged(e));
        _session.Disconnected += (_, e) => Dispatcher.Invoke(() => OnDisconnected(e.Info));
        _session.Reconnecting += (_, e) => Dispatcher.Invoke(() =>
            StateText.Text = $"Reconnecting (attempt {e.Attempt} of {e.MaxAttempts})...");
        _session.Log += (_, e) => Dispatcher.Invoke(() => LogText.Text = e.Message);
        _session.FullScreenChanged += (_, fs) => Dispatcher.Invoke(() => ApplyFullScreen(fs));
        Host.Child = _session;

        _resizeDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _resizeDebounce.Tick += (_, _) => { _resizeDebounce.Stop(); PushSizeIfFitToWindow(); };

        Loaded += OnLoaded;
        SizeChanged += (_, _) => { if (_profile.Display.SizeMode == SizeMode.FitToWindow) _resizeDebounce.Stop(); _resizeDebounce.Start(); };
        Closing += OnClosing;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_connectStarted)
            return;
        _connectStarted = true;

        var size = ComputeInitialSize();
        StateText.Text = "Connecting...";
        _session.Connect(_profile, _password, size);

        if (_profile.Display.FullScreen)
            Dispatcher.BeginInvoke(() => ApplyFullScreen(true), DispatcherPriority.Loaded);
    }

    private DesktopSize ComputeInitialSize()
    {
        var scale = _profile.Display.DesktopScalePercent ?? (int)Math.Round(GetDpiScale() * 100);
        if (_profile.Display.SizeMode == SizeMode.FitToWindow)
        {
            var (w, h) = HostPixelSize();
            return DesktopSize.Clamp(w, h, scale);
        }
        return DesktopSize.Clamp(_profile.Display.Width, _profile.Display.Height, scale);
    }

    private void PushSizeIfFitToWindow()
    {
        if (_profile.Display.SizeMode != SizeMode.FitToWindow || _session.State != SessionState.Connected)
            return;
        var scale = _profile.Display.DesktopScalePercent ?? (int)Math.Round(GetDpiScale() * 100);
        var (w, h) = HostPixelSize();
        _session.UpdateDesktopSize(DesktopSize.Clamp(w, h, scale));
    }

    private (int Width, int Height) HostPixelSize()
    {
        var dpi = GetDpiScale();
        var w = (int)Math.Round(Math.Max(Host.ActualWidth, 200) * dpi);
        var h = (int)Math.Round(Math.Max(Host.ActualHeight, 200) * dpi);
        return (w, h);
    }

    private double GetDpiScale()
    {
        var source = PresentationSource.FromVisual(this);
        return source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
    }

    private void OnStateChanged(SessionStateChangedEventArgs e)
    {
        StateText.Text = e.NewState switch
        {
            SessionState.Connecting => "Connecting...",
            SessionState.Connected => "Connected",
            SessionState.Reconnecting => "Reconnecting...",
            SessionState.Disconnected => "Disconnected",
            _ => "",
        };
        if (e.NewState == SessionState.Connected)
            PushSizeIfFitToWindow();
    }

    private void OnDisconnected(DisconnectInfo info)
    {
        _lastDisconnect = info;
        StateText.Text = "Disconnected";

        if (_userClosing || DisconnectReasons.IsUserInitiated(info.Reason, info.ExtendedReason))
        {
            Close();
            return;
        }

        if (info.IsError)
        {
            var result = MessageBox.Show(this,
                info.Message + "\n\nDo you want to try again?",
                _profile.DisplayName, MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                _session.Connect(_profile, _password, ComputeInitialSize());
                return;
            }
        }
        Close();
    }

    // ----- Toolbar -----

    private void Disconnect_Click(object sender, RoutedEventArgs e)
    {
        _userClosing = true;
        _session.Disconnect();
        Close();
    }

    private void FullScreen_Click(object sender, RoutedEventArgs e) => ApplyFullScreen(!_fullScreen);

    private void Cad_Click(object sender, RoutedEventArgs e) => _session.SendCtrlAltDel();

    private void Fit_Click(object sender, RoutedEventArgs e)
    {
        var scale = _profile.Display.DesktopScalePercent ?? (int)Math.Round(GetDpiScale() * 100);
        var (w, h) = HostPixelSize();
        _session.UpdateDesktopSize(DesktopSize.Clamp(w, h, scale));
    }

    private void ApplyFullScreen(bool value)
    {
        if (_fullScreen == value)
            return;
        _fullScreen = value;

        if (value)
        {
            _preFullScreenState = WindowState;
            Toolbar.Visibility = Visibility.Collapsed;
            LogText.Visibility = Visibility.Collapsed;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            if (WindowState == WindowState.Maximized)
                WindowState = WindowState.Normal; // force a re-maximize so the taskbar is covered
            WindowState = WindowState.Maximized;
            Topmost = true;
        }
        else
        {
            Toolbar.Visibility = Visibility.Visible;
            LogText.Visibility = Visibility.Visible;
            WindowStyle = WindowStyle.SingleBorderWindow;
            ResizeMode = ResizeMode.CanResize;
            Topmost = false;
            WindowState = _preFullScreenState;
        }
        _session.SetFullScreen(value);
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _userClosing = true;
        _resizeDebounce.Stop();
        try { _session.Disconnect(); } catch { }
        try { _session.Dispose(); } catch { }
    }
}
