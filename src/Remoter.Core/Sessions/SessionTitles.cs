namespace Remoter.Core.Sessions;

/// <summary>
/// Naming for live sessions: unique tab labels when several sessions share a name, and the
/// title of the window that hosts them.
/// </summary>
public static class SessionTitles
{
    /// <summary>
    /// Returns <paramref name="desired"/>, or "name (2)", "name (3)"... when a session with that
    /// label is already open, so two tabs on the same host are still tellable apart.
    /// </summary>
    public static string Unique(string desired, IEnumerable<string> existing)
    {
        var name = string.IsNullOrWhiteSpace(desired) ? "Session" : desired.Trim();
        var taken = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);
        if (!taken.Contains(name))
            return name;

        for (var n = 2; n < int.MaxValue; n++)
        {
            var candidate = $"{name} ({n})";
            if (!taken.Contains(candidate))
                return candidate;
        }
        return name;
    }

    /// <summary>Title for the session host window: the active session, plus a count once there is more than one.</summary>
    public static string WindowTitle(string? activeTitle, int sessionCount) => sessionCount switch
    {
        <= 0 => "Remoter sessions",
        1 => $"{Fallback(activeTitle)} - Remoter",
        _ => $"{Fallback(activeTitle)} - Remoter ({sessionCount} sessions)",
    };

    private static string Fallback(string? title) =>
        string.IsNullOrWhiteSpace(title) ? "Session" : title.Trim();
}
