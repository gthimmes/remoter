using Remoter.Core.Models;

namespace Remoter.Core.Sessions;

public enum SessionState
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting,
}

/// <summary>Size of the remote desktop in physical pixels plus the DPI scale to ask for.</summary>
public readonly record struct DesktopSize(int Width, int Height, int ScalePercent)
{
    public static DesktopSize Clamp(int width, int height, int scalePercent) =>
        new(Math.Clamp(width, 200, 8192), Math.Clamp(height, 200, 8192), Math.Clamp(scalePercent, 100, 500));
}

public sealed class SessionStateChangedEventArgs(SessionState oldState, SessionState newState) : EventArgs
{
    public SessionState OldState { get; } = oldState;
    public SessionState NewState { get; } = newState;
}

public sealed class SessionDisconnectedEventArgs(DisconnectInfo info) : EventArgs
{
    public DisconnectInfo Info { get; } = info;
}

public sealed class SessionLogEventArgs(string message) : EventArgs
{
    public DateTimeOffset Timestamp { get; } = DateTimeOffset.Now;
    public string Message { get; } = message;
}

public sealed class SessionReconnectingEventArgs(int attempt, int maxAttempts, bool networkAvailable) : EventArgs
{
    public int Attempt { get; } = attempt;
    public int MaxAttempts { get; } = maxAttempts;
    public bool NetworkAvailable { get; } = networkAvailable;
}

/// <summary>A live remote session. Implementations are UI controls; this interface keeps the UI engine-agnostic.</summary>
public interface IRemoteSession : IDisposable
{
    SessionState State { get; }

    ConnectionProfile? Profile { get; }

    bool IsFullScreen { get; }

    event EventHandler<SessionStateChangedEventArgs>? StateChanged;

    event EventHandler<SessionDisconnectedEventArgs>? Disconnected;

    event EventHandler<SessionReconnectingEventArgs>? Reconnecting;

    event EventHandler<SessionLogEventArgs>? Log;

    event EventHandler<bool>? FullScreenChanged;

    /// <summary>Starts connecting. <paramref name="password"/> is used for this attempt only and not retained.</summary>
    void Connect(ConnectionProfile profile, string? password, DesktopSize size);

    void Disconnect();

    /// <summary>Asks the remote desktop to change resolution without reconnecting where supported.</summary>
    void UpdateDesktopSize(DesktopSize size);

    void SendCtrlAltDel();

    void SetFullScreen(bool fullScreen);
}
