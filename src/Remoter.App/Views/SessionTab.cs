using System.ComponentModel;
using System.Runtime.CompilerServices;
using Remoter.Core.Sessions;

namespace Remoter.App.Views;

/// <summary>One tab in the session host window: the label, the status dot, and the live view behind it.</summary>
public sealed class SessionTab : INotifyPropertyChanged
{
    public SessionTab(SessionView view)
    {
        View = view;
    }

    public SessionView View { get; }

    public string Title
    {
        get => View.TabTitle;
        set { View.TabTitle = value; Notify(); }
    }

    public string Address => View.Profile.Port == 3389
        ? View.Profile.Host
        : $"{View.Profile.Host}:{View.Profile.Port}";

    /// <summary>
    /// The status as a name the tab template maps to a colour. The colour itself stays in XAML so
    /// that it comes from a theme token and repaints when the theme is swapped; a Brush handed over
    /// from here would be resolved once and then stay whatever it was.
    /// </summary>
    public string StatusKey => View.HasError ? "Failed" : View.State switch
    {
        SessionState.Connecting => "Connecting",
        SessionState.Connected => "Connected",
        SessionState.Reconnecting => "Reconnecting",
        _ => "Idle",
    };

    public string StatusText => View.HasError ? "Disconnected with an error" : View.State switch
    {
        SessionState.Connecting => "Connecting",
        SessionState.Connected => "Connected",
        SessionState.Reconnecting => "Reconnecting",
        _ => "Disconnected",
    };

    /// <summary>Re-reads the state-derived properties after the session changes state.</summary>
    public void RefreshStatus()
    {
        Notify(nameof(StatusKey));
        Notify(nameof(StatusText));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Notify([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
