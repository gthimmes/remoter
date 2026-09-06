using System.Runtime.Versioning;
using System.Windows.Forms;
using MSTSCLib;

namespace Remoter.Rdp;

/// <summary>
/// Thin <see cref="AxHost"/> wrapper around the Microsoft RDP client ActiveX control
/// (MsRdpClient11NotSafeForScripting, "version 12"). We host it by hand instead of using the
/// SDK COMReference because the .NET SDK build cannot run the ResolveComReference task.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class AxMsRdpClient : AxHost
{
    // MsRdpClient11NotSafeForScripting. This is the newest control that instantiates on current
    // Windows 11 builds (version 13's class factory returns CLASS_E_CLASSNOTAVAILABLE).
    private const string ControlClsid = "1DF7C823-B2D4-4B54-975A-F2AC5D7CF8B8";

    private IMsTscAxEvents_Event? _events;

    public AxMsRdpClient() : base(ControlClsid)
    {
    }

    /// <summary>The primary client interface (settings, connect/disconnect).</summary>
    public IMsRdpClient10 Client { get; private set; } = null!;

    /// <summary>Password and device-redirection settings that are not scriptable.</summary>
    public IMsRdpClientNonScriptable8 NonScriptable { get; private set; } = null!;

    /// <summary>String-keyed extended settings (Restricted Admin, Cred Guard, DPI, etc.).</summary>
    public IMsRdpExtendedSettings Extended { get; private set; } = null!;

    public event IMsTscAxEvents_OnConnectedEventHandler? Connected;
    public event IMsTscAxEvents_OnDisconnectedEventHandler? Disconnected;
    public event IMsTscAxEvents_OnLoginCompleteEventHandler? LoginComplete;
    public event IMsTscAxEvents_OnLogonErrorEventHandler? LogonError;
    public event IMsTscAxEvents_OnFatalErrorEventHandler? FatalError;
    public event IMsTscAxEvents_OnWarningEventHandler? Warning;
    public event IMsTscAxEvents_OnAutoReconnecting2EventHandler? AutoReconnecting2;
    public event IMsTscAxEvents_OnAutoReconnectedEventHandler? AutoReconnected;
    public event IMsTscAxEvents_OnConnectionBarPullDownEventHandler? ConnectionBarPullDown;
    public event IMsTscAxEvents_OnRequestLeaveFullScreenEventHandler? RequestLeaveFullScreen;
    public event IMsTscAxEvents_OnRequestGoFullScreenEventHandler? RequestGoFullScreen;
    public event IMsTscAxEvents_OnRemoteDesktopSizeChangeEventHandler? RemoteDesktopSizeChange;
    public event IMsTscAxEvents_OnReceivedTSPublicKeyEventHandler? ReceivedPublicKey;
    public event IMsTscAxEvents_OnConfirmCloseEventHandler? ConfirmClose;

    protected override void AttachInterfaces()
    {
        var ocx = GetOcx();
        Client = (IMsRdpClient10)ocx;
        NonScriptable = (IMsRdpClientNonScriptable8)ocx;
        Extended = (IMsRdpExtendedSettings)ocx;

        _events = (IMsTscAxEvents_Event)ocx;
        _events.OnConnected += () => Connected?.Invoke();
        _events.OnDisconnected += reason => Disconnected?.Invoke(reason);
        _events.OnLoginComplete += () => LoginComplete?.Invoke();
        _events.OnLogonError += code => LogonError?.Invoke(code);
        _events.OnFatalError += code => FatalError?.Invoke(code);
        _events.OnWarning += code => Warning?.Invoke(code);
        _events.OnAutoReconnecting2 += (reason, net, attempt, max) => AutoReconnecting2?.Invoke(reason, net, attempt, max);
        _events.OnAutoReconnected += () => AutoReconnected?.Invoke();
        _events.OnConnectionBarPullDown += () => ConnectionBarPullDown?.Invoke();
        _events.OnRequestLeaveFullScreen += () => RequestLeaveFullScreen?.Invoke();
        _events.OnRequestGoFullScreen += () => RequestGoFullScreen?.Invoke();
        _events.OnRemoteDesktopSizeChange += (w, h) => RemoteDesktopSizeChange?.Invoke(w, h);
        _events.OnReceivedTSPublicKey += (string key, out bool cont) =>
        {
            cont = true;
            ReceivedPublicKey?.Invoke(key, out cont);
        };
        _events.OnConfirmClose += (out bool allow) =>
        {
            allow = true;
            ConfirmClose?.Invoke(out allow);
        };
    }
}
