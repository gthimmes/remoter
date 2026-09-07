using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Remoter.App.Theming;

/// <summary>
/// Tells the desktop window manager to paint a window's caption dark or light. Only the main
/// window replaces its caption with our own chrome; every other window keeps the system one, and
/// the system paints that from the Windows theme rather than ours, so a dark app was left with a
/// light title bar.
/// </summary>
internal static class TitleBarTheme
{
    // DWMWA_USE_IMMERSIVE_DARK_MODE. The attribute was renumbered in Windows 10 20H1; try the
    // current value first and fall back for older builds.
    private const int UseImmersiveDarkMode = 20;
    private const int UseImmersiveDarkModeBefore20H1 = 19;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    public static void Apply(Window window, bool dark)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero)
            return;

        var value = dark ? 1 : 0;
        try
        {
            if (DwmSetWindowAttribute(hwnd, UseImmersiveDarkMode, ref value, sizeof(int)) != 0)
                DwmSetWindowAttribute(hwnd, UseImmersiveDarkModeBefore20H1, ref value, sizeof(int));
        }
        catch (DllNotFoundException)
        {
            // No desktop window manager. The caption just stays as the system drew it.
        }
    }

    public static void ApplyToAll(bool dark)
    {
        foreach (Window window in Application.Current.Windows)
            Apply(window, dark);
    }
}
