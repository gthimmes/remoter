using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using Remoter.Core.Sessions;

namespace Remoter.App.Views;

/// <summary>One tab in the session host window: the label, the status dot, and the live view behind it.</summary>
public sealed class SessionTab : INotifyPropertyChanged
{
    private static readonly Brush Connecting = Frozen("#E0A33C");
    private static readonly Brush Connected = Frozen("#4FBF6A");
    private static readonly Brush Reconnecting = Frozen("#5B8CFF");
    private static readonly Brush Failed = Frozen("#E5595E");
    private static readonly Brush Idle = Frozen("#8A8E98");

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

    public Brush StatusBrush => View.HasError ? Failed : View.State switch
    {
        SessionState.Connecting => Connecting,
        SessionState.Connected => Connected,
        SessionState.Reconnecting => Reconnecting,
        _ => Idle,
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
        Notify(nameof(StatusBrush));
        Notify(nameof(StatusText));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Notify([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private static Brush Frozen(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }
}
