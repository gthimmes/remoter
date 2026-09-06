using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Remoter.Core.Models;
using Remoter.Core.Sessions;
using Remoter.Rdp;

namespace Remoter.App.Views;

/// <summary>
/// One live session, as a control that can sit alongside its siblings in a tabbed host window.
/// It owns the engine (an <see cref="IRemoteSession"/>) and translates its events into the small
/// surface the host window needs: state, a log line, and requests to go full screen or close.
/// </summary>
public partial class SessionView : UserControl, IDisposable
{
    private readonly RdpSessionControl _session;
    private readonly DispatcherTimer _resizeDebounce;
    private readonly string? _password;
    private bool _started;
    private bool _closing;
    private bool _everConnected;
    private bool _disposed;

    public SessionView(ConnectionProfile profile, string? password)
    {
        InitializeComponent();
        Profile = profile;
        _password = password;

        _session = new RdpSessionControl();
        _session.StateChanged += (_, e) => Dispatcher.Invoke(() => OnStateChanged(e));
        _session.Disconnected += (_, e) => Dispatcher.Invoke(() => OnDisconnected(e.Info));
        _session.Reconnecting += (_, e) => Dispatcher.Invoke(() =>
            SetLog($"Reconnecting (attempt {e.Attempt} of {e.MaxAttempts})..."));
        _session.Log += (_, e) => Dispatcher.Invoke(() => SetLog(e.Message));
        _session.FullScreenChanged += (_, fs) => Dispatcher.Invoke(() => FullScreenRequested?.Invoke(this, fs));
        Host.Child = _session;

        _resizeDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _resizeDebounce.Tick += (_, _) => { _resizeDebounce.Stop(); PushSizeIfFitToWindow(); };
        SizeChanged += (_, _) => { _resizeDebounce.Stop(); _resizeDebounce.Start(); };

        UpdatePlaceholder();
    }

    public ConnectionProfile Profile { get; }

    /// <summary>The label shown on this session's tab; unique within its host window.</summary>
    public string TabTitle { get; set; } = "";

    public SessionState State => _session.State;

    public string LastLog { get; private set; } = "";

    /// <summary>True once the session has disconnected with an error and is showing the retry banner.</summary>
    public bool HasError { get; private set; }

    public event EventHandler? StateChanged;

    public event EventHandler? LogChanged;

    /// <summary>The session is finished and its tab should go away.</summary>
    public event EventHandler? CloseRequested;

    /// <summary>The remote side (its connection bar) asked to enter or leave full screen.</summary>
    public event EventHandler<bool>? FullScreenRequested;

    /// <summary>Raised once, the first time this session successfully connects.</summary>
    public event EventHandler? FirstConnected;

    /// <summary>Begins connecting. Call after the view is in the visual tree so it has a real size.</summary>
    public void Start()
    {
        if (_started)
            return;
        _started = true;
        SetLog("Connecting...");
        if (!TryConnect())
            return;

        if (Profile.Display.FullScreen)
            Dispatcher.BeginInvoke(() => FullScreenRequested?.Invoke(this, true), DispatcherPriority.Loaded);
    }

    /// <summary>
    /// Starts a connection attempt, reporting a failure to set one up in this tab's banner rather
    /// than letting it escape as an unhandled exception across every other live session.
    /// </summary>
    private bool TryConnect()
    {
        // The hosted control only builds itself while it is visible, so show it before connecting.
        Host.Visibility = Visibility.Visible;
        try
        {
            _session.Connect(Profile, _password, ComputeInitialSize());
            UpdatePlaceholder();
            return true;
        }
        catch (Exception ex)
        {
            SetLog(ex.Message);
            ShowBanner(ex.Message);
            StateChanged?.Invoke(this, EventArgs.Empty);
            return false;
        }
    }

    /// <summary>Disconnects at the user's request; the tab closes rather than offering to retry.</summary>
    public void Disconnect()
    {
        _closing = true;
        try { _session.Disconnect(); } catch { }
    }

    public void SendCtrlAltDel() => _session.SendCtrlAltDel();

    public void FitNow() => _session.UpdateDesktopSize(CurrentHostSize());

    public void SetFullScreen(bool fullScreen) => _session.SetFullScreen(fullScreen);

    /// <summary>Gives keyboard focus to the remote desktop.</summary>
    public void FocusSession()
    {
        try { _session.Focus(); } catch { }
    }

    // ----- Engine events -----

    private void OnStateChanged(SessionStateChangedEventArgs e)
    {
        if (e.NewState == SessionState.Connected)
        {
            HideBanner();
            PushSizeIfFitToWindow();
            if (!_everConnected)
            {
                _everConnected = true;
                FirstConnected?.Invoke(this, EventArgs.Empty);
            }
        }
        UpdatePlaceholder();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnDisconnected(DisconnectInfo info)
    {
        if (_closing || DisconnectReasons.IsUserInitiated(info.Reason, info.ExtendedReason))
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (info.IsError)
        {
            ShowBanner(info.Message);
            StateChanged?.Invoke(this, EventArgs.Empty);
            return;
        }
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    // ----- Error banner -----

    private void ShowBanner(string message)
    {
        HasError = true;
        BannerText.Text = message;
        Banner.Visibility = Visibility.Visible;
        UpdatePlaceholder();
    }

    private void HideBanner()
    {
        HasError = false;
        Banner.Visibility = Visibility.Collapsed;
        UpdatePlaceholder();
    }

    /// <summary>
    /// A dead session's control paints a white rectangle, which reads as a broken tab, so it is
    /// hidden once the session has failed and the placeholder shows through instead. It is only
    /// ever hidden after a connection attempt has ended: hiding it earlier would stop the control
    /// from being created at all.
    /// </summary>
    private void UpdatePlaceholder()
    {
        Host.Visibility = HasError ? Visibility.Hidden : Visibility.Visible;
        Placeholder.Text = HasError ? "Disconnected." : State switch
        {
            SessionState.Connecting => $"Connecting to {Profile.Host}...",
            SessionState.Reconnecting => "Reconnecting...",
            _ => "",
        };
    }

    private void Reconnect_Click(object sender, RoutedEventArgs e)
    {
        HideBanner();
        SetLog("Reconnecting...");
        if (TryConnect())
            StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void CloseTab_Click(object sender, RoutedEventArgs e) => CloseRequested?.Invoke(this, EventArgs.Empty);

    // ----- Sizing -----

    private DesktopSize ComputeInitialSize()
    {
        if (Profile.Display.SizeMode == SizeMode.FitToWindow)
            return CurrentHostSize();
        return DesktopSize.Clamp(Profile.Display.Width, Profile.Display.Height, Scale());
    }

    private void PushSizeIfFitToWindow()
    {
        if (Profile.Display.SizeMode != SizeMode.FitToWindow || _session.State != SessionState.Connected)
            return;
        _session.UpdateDesktopSize(CurrentHostSize());
    }

    private DesktopSize CurrentHostSize()
    {
        var dpi = DpiScale();
        var w = (int)Math.Round(Math.Max(Host.ActualWidth, 200) * dpi);
        var h = (int)Math.Round(Math.Max(Host.ActualHeight, 200) * dpi);
        return DesktopSize.Clamp(w, h, Scale());
    }

    private int Scale() => Profile.Display.DesktopScalePercent ?? (int)Math.Round(DpiScale() * 100);

    private double DpiScale()
    {
        var source = PresentationSource.FromVisual(this);
        return source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
    }

    private void SetLog(string message)
    {
        LastLog = message;
        LogChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _closing = true;
        _resizeDebounce.Stop();
        try { _session.Disconnect(); } catch { }
        try { _session.Dispose(); } catch { }
        GC.SuppressFinalize(this);
    }
}
