namespace Remoter.Core.Models;

/// <summary>How the remote desktop size relates to the local window.</summary>
public enum SizeMode
{
    /// <summary>Remote resolution follows the window size (resolution updates on resize).</summary>
    FitToWindow,
    /// <summary>Fixed remote resolution, scaled to fit the window ("smart sizing").</summary>
    SmartSize,
    /// <summary>Fixed remote resolution, scrollbars if the window is smaller.</summary>
    Fixed,
}

public enum AudioPlaybackMode
{
    /// <summary>Play sounds on this computer (redirect to client).</summary>
    OnThisComputer = 0,
    /// <summary>Play sounds on the remote computer.</summary>
    OnRemoteComputer = 1,
    /// <summary>Do not play sounds.</summary>
    DoNotPlay = 2,
}

/// <summary>Where Windows key combinations (Alt+Tab etc.) are applied.</summary>
public enum KeyboardMode
{
    OnThisComputer = 0,
    OnRemoteComputer = 1,
    OnRemoteComputerInFullScreenOnly = 2,
}

/// <summary>Values match the control's NetworkConnectionType.</summary>
public enum ConnectionSpeed
{
    Modem = 1,
    LowSpeedBroadband = 2,
    Satellite = 3,
    HighSpeedBroadband = 4,
    Wan = 5,
    Lan = 6,
    AutoDetect = 7,
}

/// <summary>
/// What to do when the server's identity cannot be verified. Values match the control's
/// AuthenticationLevel and the ".rdp" "authentication level" field:
/// 0 = connect without warning, 1 = do not connect, 2 = warn and let the user choose.
/// </summary>
public enum ServerAuthenticationPolicy
{
    /// <summary>Connect and don't warn me.</summary>
    AlwaysConnect = 0,
    /// <summary>Do not connect if the server cannot be authenticated.</summary>
    DoNotConnect = 1,
    /// <summary>Warn me and let me choose whether to proceed (the mstsc default).</summary>
    Warn = 2,
}

/// <summary>Values match GatewayUsageMethod.</summary>
public enum GatewayUsage
{
    Never = 0,
    Always = 1,
    AutoDetect = 2,
}

/// <summary>Values match GatewayCredsSource.</summary>
public enum GatewayCredentialSource
{
    AskForPassword = 0,
    SmartCard = 1,
    SelectLater = 4,
}
