using System.Text.Json;
using Remoter.Core.Models;

namespace Remoter.Core.Storage;

/// <summary>
/// Persists connection profiles as a single JSON document. Writes are atomic (temp file + rename)
/// so a crash mid-save never loses the previous list.
/// </summary>
public sealed class ProfileStore
{
    private const int CurrentVersion = 1;

    public ProfileStore(string filePath)
    {
        FilePath = filePath;
    }

    public string FilePath { get; }

    public static string DefaultPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Remoter", "connections.json");

    /// <summary>
    /// Loads all profiles. A missing file yields an empty list. A corrupt file is moved aside
    /// (so the user's data is not destroyed by the next save) and an empty list is returned.
    /// </summary>
    public IReadOnlyList<ConnectionProfile> Load(out string? warning)
    {
        warning = null;
        if (!File.Exists(FilePath))
            return Array.Empty<ConnectionProfile>();

        try
        {
            var json = File.ReadAllText(FilePath);
            var doc = JsonSerializer.Deserialize<StoreDocument>(json, JsonDefaults.Options);
            return doc?.Profiles ?? new List<ConnectionProfile>();
        }
        catch (JsonException ex)
        {
            var backup = FilePath + ".corrupt-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            File.Move(FilePath, backup, overwrite: true);
            warning = $"The connection list could not be read ({ex.Message}). It was moved to {backup}.";
            return Array.Empty<ConnectionProfile>();
        }
    }

    public IReadOnlyList<ConnectionProfile> Load() => Load(out _);

    public void Save(IEnumerable<ConnectionProfile> profiles)
    {
        var doc = new StoreDocument { Version = CurrentVersion, Profiles = profiles.ToList() };
        var json = JsonSerializer.Serialize(doc, JsonDefaults.Options);

        var dir = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var temp = FilePath + ".tmp";
        File.WriteAllText(temp, json);
        File.Move(temp, FilePath, overwrite: true);
    }

    private sealed class StoreDocument
    {
        public int Version { get; set; } = CurrentVersion;
        public List<ConnectionProfile> Profiles { get; set; } = new();
    }
}
