namespace Remoter.Core.Sessions;

public sealed record DisconnectInfo(int Reason, int ExtendedReason, string Message, bool IsError)
{
    public override string ToString() => $"{Message} (reason {Reason}, extended {ExtendedReason})";
}

/// <summary>
/// Turns the RDP control's disconnect / extended disconnect codes into text a person can act on.
/// Unknown codes fall back to the control's own description, then to a generic message.
/// </summary>
public static class DisconnectReasons
{
    // Extended reasons: ExtendedDisconnectReasonCode from mstscax.
    private static readonly Dictionary<int, string> Extended = new()
    {
        [1] = "The session was disconnected by an administrator or another tool.",
        [2] = "The session was logged off by an administrator or another tool.",
        [3] = "The remote computer disconnected the session because it was idle for too long.",
        [4] = "The remote computer disconnected the session because no one logged on in time.",
        [5] = "Another user connected to the remote computer, so your connection was lost.",
        [6] = "The remote computer is out of memory.",
        [7] = "The remote computer refused the connection.",
        [8] = "The remote computer refused the connection because it requires FIPS-compliant encryption.",
        [9] = "Your account does not have permission to sign in remotely to this computer.",
        [10] = "The remote computer requires fresh credentials.",
        [11] = "The session was disconnected by you or another user.",
        [12] = "You logged off from the remote computer.",
        [25] = "The remote computer is shutting down.",
        [26] = "The remote computer is restarting.",
        [768] = "The credentials that were used to connect were rejected.",
    };

    // Disconnect reasons: the well-known subset of IMsTscAxEvents::OnDisconnected codes.
    private static readonly Dictionary<int, (string Message, bool IsError)> Reasons = new()
    {
        [0] = ("Disconnected.", false),
        [1] = ("Disconnected.", false),
        [2] = ("You disconnected or logged off from the remote computer.", false),
        [3] = ("The remote computer ended the session.", false),
        [260] = ("The computer name could not be resolved. Check the name or use its IP address.", true),
        [264] = ("The connection timed out. The computer may be off, asleep, or blocked by a firewall.", true),
        [516] = ("Could not connect. Make sure Remote Desktop is enabled on the remote computer and the port is reachable.", true),
        [520] = ("The remote computer could not be found on the network.", true),
        [772] = ("The connection was lost while sending data.", true),
        [1030] = ("The connection was lost.", true),
        [2308] = ("The connection to the remote computer was lost.", true),
        [2825] = ("The remote computer requires Network Level Authentication and your credentials could not be verified.", true),
    };

    public static DisconnectInfo Describe(int reason, int extendedReason, string? controlDescription)
    {
        string message;
        bool isError;

        if (extendedReason != 0 && Extended.TryGetValue(extendedReason, out var ext))
        {
            message = ext;
            isError = !IsUserInitiated(reason, extendedReason);
        }
        else if (Reasons.TryGetValue(reason, out var known))
        {
            (message, isError) = known;
        }
        else if (!string.IsNullOrWhiteSpace(controlDescription))
        {
            message = controlDescription.Trim();
            isError = true;
        }
        else
        {
            message = $"The connection ended (code {reason}).";
            isError = true;
        }

        return new DisconnectInfo(reason, extendedReason, message, isError);
    }

    /// <summary>Reasons that mean "the user asked for this", where no error UI should appear.</summary>
    public static bool IsUserInitiated(int reason, int extendedReason) =>
        extendedReason is 1 or 2 or 11 or 12 || (extendedReason == 0 && reason is 1 or 2);
}
