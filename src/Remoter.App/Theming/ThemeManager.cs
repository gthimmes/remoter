using System.Windows;
using Microsoft.Win32;
using Remoter.Core.Storage;

namespace Remoter.App.Theming;

/// <summary>
/// Owns the app's theme: resolves the chosen mode to a dictionary, swaps it in place, remembers
/// the choice, and keeps following Windows while the mode is <see cref="AppTheme.System"/>.
/// </summary>
public sealed class ThemeManager : IDisposable
{
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsUseLightTheme = "AppsUseLightTheme";

    private readonly SettingsStore _store;
    private readonly AppSettings _settings;
    private bool _watching;
    private bool _disposed;

    public ThemeManager(SettingsStore store, AppSettings settings)
    {
        _store = store;
        _settings = settings;
    }

    public AppTheme Mode => _settings.Theme;

    /// <summary>The theme actually in use: the chosen one, or what Windows asks for under System.</summary>
    public string Resolved => Mode switch
    {
        AppTheme.Dark => "Dark",
        AppTheme.Light => "Light",
        _ => WindowsPrefersLight() ? "Light" : "Dark",
    };

    /// <summary>Raised after the applied theme changes, so any UI showing the choice can refresh.</summary>
    public event EventHandler? Changed;

    /// <summary>Paints the stored choice. Call before the first window is shown to avoid a flash.</summary>
    public void Initialize()
    {
        Apply();
        UpdateWatch();
    }

    /// <summary>Records a new choice, persists it, and repaints.</summary>
    public void SetMode(AppTheme mode)
    {
        if (_settings.Theme == mode)
            return;
        _settings.Theme = mode;
        try { _store.Save(_settings); }
        catch { /* a preference that cannot be written is not worth interrupting the user over */ }
        Apply();
        UpdateWatch();
    }

    private void Apply()
    {
        App.ApplyTheme(Resolved);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Only listen to Windows while we are actually following it.</summary>
    private void UpdateWatch()
    {
        var shouldWatch = Mode == AppTheme.System;
        if (shouldWatch == _watching)
            return;
        if (shouldWatch)
            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        else
            SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        _watching = shouldWatch;
    }

    // Fires on a system thread, so hop to the dispatcher before touching resources.
    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category != UserPreferenceCategory.General)
            return;
        Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            if (Mode == AppTheme.System)
                Apply();
        });
    }

    /// <summary>
    /// Reads the Windows app theme. An unreadable or missing value falls back to dark, which is
    /// what Remoter has always looked like.
    /// </summary>
    private static bool WindowsPrefersLight()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
            return key?.GetValue(AppsUseLightTheme) is int value && value != 0;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        if (_watching)
            SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        _watching = false;
    }
}
