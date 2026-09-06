namespace Remoter.Core.Models;

/// <summary>Pure helpers for filtering and organizing connections, kept out of the UI so they can be tested.</summary>
public static class ConnectionQuery
{
    /// <summary>
    /// True when the profile matches the search text. Empty text matches everything. Matching is
    /// case-insensitive across name, host, user, folder, tags and notes. Space-separated terms are
    /// ANDed, so "home sql" matches a profile that contains both words somewhere.
    /// </summary>
    public static bool Matches(ConnectionProfile p, string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return true;

        var fields = new[]
        {
            p.Name, p.Host, p.QualifiedUsername, p.Folder, p.Notes,
            string.Join(' ', p.Tags),
        };
        var haystack = string.Join('\n', fields).ToLowerInvariant();

        foreach (var term in query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (!haystack.Contains(term, StringComparison.Ordinal))
                return false;
        }
        return true;
    }

    /// <summary>Normalizes a folder path: trims, collapses slashes, no leading/trailing separators.</summary>
    public static string NormalizeFolder(string? folder)
    {
        if (string.IsNullOrWhiteSpace(folder))
            return "";
        var parts = folder.Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Join('/', parts);
    }

    /// <summary>The label to show a connection under when it has no folder.</summary>
    public static string GroupLabel(ConnectionProfile p) =>
        string.IsNullOrEmpty(p.Folder) ? "Ungrouped" : p.Folder;

    /// <summary>All distinct folder paths in use, plus their parent paths, sorted.</summary>
    public static IReadOnlyList<string> AllFolders(IEnumerable<ConnectionProfile> profiles)
    {
        var set = new SortedSet<string>(StringComparer.CurrentCultureIgnoreCase);
        foreach (var p in profiles)
        {
            var folder = NormalizeFolder(p.Folder);
            if (folder.Length == 0)
                continue;
            var parts = folder.Split('/');
            for (var i = 1; i <= parts.Length; i++)
                set.Add(string.Join('/', parts.Take(i)));
        }
        return set.ToList();
    }
}
