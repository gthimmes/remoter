using System.Runtime.Versioning;
using System.Windows.Forms;
using MSTSCLib;
using Remoter.Core.Models;
using Remoter.Core.Sessions;

namespace Remoter.Rdp;

/// <summary>
/// A WinForms control that hosts one RDP session and exposes it through <see cref="IRemoteSession"/>.
/// It owns an <see cref="AxMsRdpClient"/>, applies a profile to the control's settings, and translates
/// the control's COM events into the engine-agnostic session events the UI consumes.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class RdpSessionControl : UserControl, IRemoteSession
{
    private readonly AxMsRdpClient _ax;
    private SessionState _state = SessionState.Disconnected;
    private DesktopSize _requestedSize;
    private bool _fullScreen;
    private bool _sawFatalError;
    private int _lastLogonError = int.MinValue;

    public RdpSessionControl()
    {
        _ax = new AxMsRdpClient { Dock = DockStyle.Fill };
        Controls.Add(_ax);
        WireEvents();
    }

    public ConnectionProfile? Profile { get; private set; }

    public SessionState State => _state;

    public bool IsFullScreen => _fullScreen;

    public event EventHandler<SessionStateChangedEventArgs>? StateChanged;
    public event EventHandler<SessionDisconnectedEventArgs>? Disconnected;
    public event EventHandler<SessionReconnectingEventArgs>? Reconnecting;
    public event EventHandler<SessionLogEventArgs>? Log;
    public event EventHandler<bool>? FullScreenChanged;

    public void Connect(ConnectionProfile profile, string? password, DesktopSize size)
    {
        Profile = profile;
        _requestedSize = size;
        _sawFatalError = false;
        _lastLogonError = int.MinValue;

        if (!_ax.Created)
            _ax.CreateControl();

        ApplyProfile(profile, password, size);
        SetState(SessionState.Connecting);
        LogMessage($"Connecting to {profile.Host}:{profile.Port} as {profile.QualifiedUsername}...");
        _ax.Client.Connect();
    }

    public void Disconnect()
    {
        try
        {
            if (_ax.Created && _ax.Client.Connected != 0)
                _ax.Client.Disconnect();
        }
        catch (Exception ex)
        {
            LogMessage("Disconnect failed: " + ex.Message);
        }
    }

    public void UpdateDesktopSize(DesktopSize size)
    {
        _requestedSize = size;
        if (!_ax.Created || _ax.Client.Connected == 0)
            return;
        try
        {
            _ax.Client.UpdateSessionDisplaySettings(
                (uint)size.Width, (uint)size.Height,
                (uint)size.Width, (uint)size.Height,
                0, (uint)size.ScalePercent, 100);
        }
        catch (Exception ex)
        {
            LogMessage("Could not update remote resolution: " + ex.Message);
        }
    }

    public void SendCtrlAltDel()
    {
        // Ctrl(0x11)+Alt(0x12)+Del(0x2E) via the secure attention sequence.
        try { _ax.Client.AdvancedSettings9.SasSequence = 0xAA03; } catch { /* only valid while connected */ }
    }

    public void SetFullScreen(bool fullScreen)
    {
        if (!_ax.Created)
            return;
        try
        {
            _ax.Client.FullScreen = fullScreen;
        }
        catch (Exception ex)
        {
            LogMessage("Could not change full screen: " + ex.Message);
        }
    }

    private void ApplyProfile(ConnectionProfile p, string? password, DesktopSize size)
    {
        var c = _ax.Client;
        var adv = c.AdvancedSettings9;
        var ns = _ax.NonScriptable;
        var secured = c.SecuredSettings3;
        var transport = c.TransportSettings4;

        c.Server = p.Host;
        c.DesktopWidth = size.Width;
        c.DesktopHeight = size.Height;
        c.ColorDepth = p.Display.ColorDepth;

        // Pass the identity as a single "domain\user" string in UserName and leave Domain empty.
        // Setting Domain separately breaks Microsoft-account logon (domain "MicrosoftAccount"): the
        // host rejects it with "The logon attempt failed" even though the password is correct, while
        // the combined form works. Plain local and AD "DOMAIN\user" logins accept this form too.
        var loginName = BuildLoginName(p.Domain, p.Username);
        if (!string.IsNullOrEmpty(loginName))
            c.UserName = loginName;
        c.Domain = "";
        if (!string.IsNullOrEmpty(password))
            ns.ClearTextPassword = password;

        adv.RDPPort = p.Port;
        adv.EnableCredSspSupport = p.Security.NetworkLevelAuthentication;
        adv.AuthenticationLevel = (uint)p.Security.ServerAuthentication;
        adv.NegotiateSecurityLayer = true;
        adv.PublicMode = false;
        adv.GrabFocusOnConnect = true;

        // Reconnect
        adv.EnableAutoReconnect = p.Experience.AutoReconnect;
        adv.MaxReconnectAttempts = p.Experience.AutoReconnect ? 20 : 0;

        // Display
        adv.SmartSizing = p.Display.SizeMode == SizeMode.SmartSize;
        c.FullScreen = false; // go full screen after connect to avoid a mode change mid-negotiation
        adv.DisplayConnectionBar = p.Display.ShowConnectionBar;
        adv.PinConnectionBar = p.Display.PinConnectionBar;
        adv.ConnectionBarShowMinimizeButton = true;
        adv.ConnectionBarShowRestoreButton = true;
        adv.ContainerHandledFullScreen = 1;
        ns.UseMultimon = p.Display.FullScreen && p.Display.UseAllMonitors;

        // DPI scaling
        var scale = p.Display.DesktopScalePercent ?? size.ScalePercent;
        TrySetExtended("DesktopScaleFactor", (uint)scale);
        TrySetExtended("DeviceScaleFactor", (uint)100);

        // Experience / performance
        adv.PerformanceFlags = p.Experience.ToPerformanceFlags();
        adv.NetworkConnectionType = (uint)p.Experience.Speed;
        adv.BandwidthDetection = p.Experience.Speed == ConnectionSpeed.AutoDetect;
        adv.BitmapPersistence = p.Experience.PersistentBitmapCaching ? 1 : 0;

        // Local resources
        adv.AudioRedirectionMode = (uint)p.LocalResources.AudioPlayback;
        adv.AudioCaptureRedirectionMode = p.LocalResources.AudioCapture;
        secured.KeyboardHookMode = (int)p.LocalResources.Keyboard;
        adv.RedirectClipboard = p.LocalResources.Clipboard;
        adv.RedirectPrinters = p.LocalResources.Printers;
        adv.RedirectSmartCards = p.LocalResources.SmartCards;
        adv.RedirectPorts = p.LocalResources.Ports;
        adv.RedirectDevices = p.LocalResources.Devices;
        ns.RedirectDynamicDrives = p.LocalResources.DynamicDrives;
        ApplyDriveRedirection(p, ns);

        // Security modes
        TrySetExtended("RestrictedLogon", p.Security.RestrictedAdmin);
        TrySetExtended("DisableCredentialsDelegation", p.Security.RestrictedAdmin);
        TrySetExtended("RedirectedAuthentication", p.Security.RemoteCredentialGuard);
        adv.ConnectToAdministerServer = p.Security.AdministrativeSession;

        // Gateway
        if (p.Gateway.Usage != GatewayUsage.Never && !string.IsNullOrWhiteSpace(p.Gateway.Hostname))
        {
            transport.GatewayUsageMethod = (uint)p.Gateway.Usage;
            transport.GatewayProfileUsageMethod = 1; // explicit settings
            transport.GatewayHostname = p.Gateway.Hostname;
            transport.GatewayCredsSource = (uint)p.Gateway.CredentialSource;
            transport.GatewayCredSharing = p.Gateway.UseSameCredentials ? 1u : 0u;
        }
        else
        {
            transport.GatewayUsageMethod = 0;
        }

        // Start program
        if (!string.IsNullOrWhiteSpace(p.Program.StartProgram))
        {
            secured.StartProgram = p.Program.StartProgram;
            if (!string.IsNullOrWhiteSpace(p.Program.WorkingDirectory))
                secured.WorkDir = p.Program.WorkingDirectory;
        }

        // We handle warnings ourselves via events; suppress the control's own dialogs where possible.
        ns.AllowCredentialSaving = false;
        adv.EnableWindowsKey = 1;
    }

    /// <summary>
    /// Builds the single username string to hand the control: "domain\user" when a NetBIOS-style
    /// domain is present, otherwise the username as-is (already a UPN, an already-qualified
    /// "domain\user", or a Microsoft-account "MicrosoftAccount\email").
    /// </summary>
    private static string BuildLoginName(string? domain, string? username)
    {
        var user = username?.Trim() ?? "";
        var dom = domain?.Trim() ?? "";
        if (user.Length == 0)
            return "";
        if (user.Contains('\\') || dom.Length == 0)
            return user;
        return $"{dom}\\{user}";
    }

    private static void ApplyDriveRedirection(ConnectionProfile p, IMsRdpClientNonScriptable8 ns)
    {
        var drives = ns.DriveCollection;
        try
        {
            for (uint i = 0; i < drives.DriveCount; i++)
            {
                var drive = drives.DriveByIndex[i];
                bool redirect = p.LocalResources.AllDrives ||
                    p.LocalResources.Drives.Any(d => drive.Name.StartsWith(d.TrimEnd(':'), StringComparison.OrdinalIgnoreCase));
                drive.RedirectionState = redirect;
            }
        }
        catch
        {
            // Drive collection is best-effort; a locked or empty drive must not stop the connection.
        }
    }

    private void TrySetExtended(string name, object value)
    {
        try { _ax.Extended.set_Property(name, ref value); }
        catch (Exception ex) { LogMessage($"Setting '{name}' is not supported by this control ({ex.Message})."); }
    }

    private void WireEvents()
    {
        _ax.Connected += () => { SetState(SessionState.Connected); LogMessage("Connected."); };
        _ax.LoginComplete += () => LogMessage("Login complete.");
        _ax.LogonError += code =>
        {
            _lastLogonError = code;
            // -2 is "credentials were rejected by policy" but not an error we should surface twice;
            // real credential failures come back through OnLogonError with a positive code.
            if (code > 0)
                LogMessage($"Logon error {code}.");
        };
        _ax.FatalError += code => { _sawFatalError = true; LogMessage($"Fatal error {code}."); };
        _ax.Warning += code => LogMessage($"Warning {code}.");
        _ax.AutoReconnecting2 += (reason, net, attempt, max) =>
        {
            SetState(SessionState.Reconnecting);
            Reconnecting?.Invoke(this, new SessionReconnectingEventArgs(attempt, max, net));
            LogMessage($"Reconnecting (attempt {attempt} of {max}, network {(net ? "up" : "down")})...");
        };
        _ax.AutoReconnected += () => { SetState(SessionState.Connected); LogMessage("Reconnected."); };
        _ax.RequestGoFullScreen += () => UpdateFullScreen(true);
        _ax.RequestLeaveFullScreen += () => UpdateFullScreen(false);
        _ax.Disconnected += reason => HandleDisconnected(reason);
    }

    private void HandleDisconnected(int discReason)
    {
        int extended = 0;
        string? description = null;
        try { extended = (int)_ax.Client.ExtendedDisconnectReason; } catch { }
        try { description = _ax.Client.GetErrorDescription((uint)discReason, (uint)extended); } catch { }

        var info = DisconnectReasons.Describe(discReason, extended, description);
        SetState(SessionState.Disconnected);
        LogMessage("Disconnected: " + info.Message);
        Disconnected?.Invoke(this, new SessionDisconnectedEventArgs(info));
    }

    private void UpdateFullScreen(bool value)
    {
        if (_fullScreen == value)
            return;
        _fullScreen = value;
        FullScreenChanged?.Invoke(this, value);
    }

    private void SetState(SessionState next)
    {
        if (_state == next)
            return;
        var old = _state;
        _state = next;
        StateChanged?.Invoke(this, new SessionStateChangedEventArgs(old, next));
    }

    private void LogMessage(string message) => Log?.Invoke(this, new SessionLogEventArgs(message));

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try { Disconnect(); } catch { }
            try { _ax.Dispose(); } catch { }
        }
        base.Dispose(disposing);
    }
}
