using System.ComponentModel;

namespace Remoter.Core.Sessions;

/// <summary>
/// Which saved connections have a live session open right now, so the list can mark them.
/// A connection can have more than one session at once (two tabs on one host), so each is
/// tracked by the session object itself and the connection counts as live while any is.
/// </summary>
public sealed class LiveSessionTracker : INotifyPropertyChanged
{
    private readonly Dictionary<Guid, HashSet<object>> _live = new();

    /// <summary>
    /// Bumps each time some connection starts or stops being live. It carries no meaning of its
    /// own; a binding that includes it re-evaluates when it changes, which is how the list's rows
    /// learn to update without regenerating themselves.
    /// </summary>
    public int Version { get; private set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsLive(Guid profileId) => _live.ContainsKey(profileId);

    /// <summary>Records whether <paramref name="session"/>, for <paramref name="profileId"/>, is live.</summary>
    public void Update(Guid profileId, object session, bool live)
    {
        var wasLive = IsLive(profileId);
        if (live)
        {
            if (!_live.TryGetValue(profileId, out var sessions))
                _live[profileId] = sessions = new HashSet<object>(ReferenceEqualityComparer.Instance);
            sessions.Add(session);
        }
        else if (_live.TryGetValue(profileId, out var sessions) && sessions.Remove(session) && sessions.Count == 0)
        {
            _live.Remove(profileId);
        }

        if (wasLive != IsLive(profileId))
            Bump();
    }

    /// <summary>Forgets <paramref name="session"/> entirely, whichever connection it belonged to.</summary>
    public void Remove(object session)
    {
        var changed = false;
        foreach (var id in _live.Keys.ToList())
        {
            var sessions = _live[id];
            if (sessions.Remove(session) && sessions.Count == 0)
            {
                _live.Remove(id);
                changed = true;
            }
        }
        if (changed)
            Bump();
    }

    private void Bump()
    {
        Version++;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Version)));
    }
}
