using System.Text.Json;
using Remoter.Core.Models;

namespace Remoter.Core.Storage;

/// <summary>The persisted contents of the store: saved profiles plus the recent-hosts list.</summary>
public sealed class StoreData
{
    public List<ConnectionProfile> Profiles { get; set; } = new();
    public List<RecentConnection> Recents { get; set; } = new();
}

/// <summary>
/// Persists the store as a single JSON document. Writes are atomic (temp file + rename)
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
    /// Loads the store. A missing file yields empty lists. A corrupt file is moved aside
    /// (so the user's data is not destroyed by the next save) and empty lists are returned.
    /// </summary>
    public StoreData Load(out string? warning)
    {
        warning = null;
        if (!File.Exists(FilePath))
            return new StoreData();

        try
        {
            var json = File.ReadAllText(FilePath);
            var doc = JsonSerializer.Deserialize<StoreDocument>(json, JsonDefaults.Options);
            return new StoreData
            {
                Profiles = doc?.Profiles ?? new(),
                Recents = doc?.Recents ?? new(),
            };
        }
        catch (JsonException ex)
        {
            var backup = FilePath + ".corrupt-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            File.Move(FilePath, backup, overwrite: true);
            warning = $"The connection list could not be read ({ex.Message}). It was moved to {backup}.";
            return new StoreData();
        }
    }

    public StoreData Load() => Load(out _);

    public void Save(StoreData data)
    {
        var doc = new StoreDocument { Version = CurrentVersion, Profiles = data.Profiles, Recents = data.Recents };
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
        public List<RecentConnection> Recents { get; set; } = new();
    }
}
