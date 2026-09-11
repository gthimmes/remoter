using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Remoter.App.Theming;

/// <summary>
/// Overlay scrollbars float over the content and stay out of sight until wanted: while the
/// pointer is over the scrolling area, and for a moment after it actually scrolls. The template
/// handles the pointer; this handles the scrolling, which a template trigger cannot tell apart
/// from the content merely changing size.
/// </summary>
public static class OverlayScroll
{
    private static readonly TimeSpan Linger = TimeSpan.FromSeconds(1);
    private static readonly ConditionalWeakTable<ScrollViewer, DispatcherTimer> Timers = new();

    /// <summary>True for a moment after the ScrollViewer's offset moves.</summary>
    public static readonly DependencyProperty IsScrollingProperty = DependencyProperty.RegisterAttached(
        "IsScrolling", typeof(bool), typeof(OverlayScroll), new PropertyMetadata(false));

    public static bool GetIsScrolling(DependencyObject element) => (bool)element.GetValue(IsScrollingProperty);

    public static void SetIsScrolling(DependencyObject element, bool value) => element.SetValue(IsScrollingProperty, value);

    /// <summary>Starts listening to every ScrollViewer in the app. Call once at startup.</summary>
    public static void Register() =>
        EventManager.RegisterClassHandler(typeof(ScrollViewer), ScrollViewer.ScrollChangedEvent,
            new ScrollChangedEventHandler(OnScrollChanged));

    private static void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        // Only this viewer's own scrolling, and only a real move. ScrollChanged also bubbles up
        // from nested viewers and fires when the extent changes, such as while typing in a box.
        if (sender is not ScrollViewer viewer || !ReferenceEquals(e.OriginalSource, viewer))
            return;
        if (e.VerticalChange == 0 && e.HorizontalChange == 0)
            return;

        SetIsScrolling(viewer, true);
        var timer = Timers.GetValue(viewer, v =>
        {
            var t = new DispatcherTimer { Interval = Linger };
            t.Tick += (_, _) => { t.Stop(); SetIsScrolling(v, false); };
            return t;
        });
        timer.Stop();
        timer.Start();
    }
}
