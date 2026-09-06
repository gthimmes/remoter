using System.Text.Json;

namespace Remoter.Core.Storage;

/// <summary>Which theme the app paints in. Named AppTheme because WPF now ships its own ThemeMode.</summary>
public enum AppTheme
{
    /// <summary>Follow the Windows app theme, and keep following it when the user changes it.</summary>
    System,
    Dark,
    Light,
}

/// <summary>App preferences that are not part of the connection list.</summary>
public sealed class AppSettings
{
    public AppTheme Theme { get; set; } = AppTheme.System;
}

/// <summary>
/// Persists <see cref="AppSettings"/> as its own small JSON document, next to the connection
/// list. Writes are atomic; a missing or unreadable file falls back to the defaults rather than
/// failing startup, because no preference is worth refusing to open the app over.
/// </summary>
public sealed class SettingsStore
{
    public SettingsStore(string filePath)
    {
        FilePath = filePath;
    }

    public string FilePath { get; }

    public static string DefaultPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Remoter", "settings.json");

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath))
                return new AppSettings();
            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath), JsonDefaults.Options)
                   ?? new AppSettings();
        }
        catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException)
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        var dir = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var temp = FilePath + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(settings, JsonDefaults.Options));
        File.Move(temp, FilePath, overwrite: true);
    }
}
