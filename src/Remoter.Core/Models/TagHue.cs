namespace Remoter.Core.Models;

/// <summary>
/// Picks a tag's colour from its text, so a tag keeps the same colour in every row and between
/// runs. String.GetHashCode is randomised per process and would repaint the list on every launch,
/// so this uses FNV-1a over the folded text instead.
/// </summary>
public static class TagHue
{
    /// <summary>How many hues the palette cycles through.</summary>
    public const int Count = 4;

    /// <summary>The hue index, 0 to <see cref="Count"/> - 1, for a tag.</summary>
    public static int IndexFor(string? tag)
    {
        var text = tag?.Trim() ?? "";
        if (text.Length == 0)
            return 0;

        unchecked
        {
            const uint offsetBasis = 2166136261;
            const uint prime = 16777619;
            var hash = offsetBasis;
            foreach (var c in text)
            {
                hash ^= char.ToLowerInvariant(c);
                hash *= prime;
            }
            return (int)(hash % Count);
        }
    }
}
