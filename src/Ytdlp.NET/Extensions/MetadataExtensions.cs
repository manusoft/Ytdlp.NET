namespace ManuHub.Ytdlp.NET.Extensions;

/// <summary>
/// Extension methods for flattens nested metadata entries into a single-level playlist structure <see cref="Metadata"/>. 
/// </summary>
public static class MetadataExtensions
{
    /// <summary>
    /// Flattens nested metadata entries into a single-level playlist structure.
    /// </summary>
    /// <remarks>
    /// This is useful for playlist-type metadata where entries may contain
    /// nested sub-entries (e.g., playlists inside playlists).
    /// Only leaf nodes with valid titles and URLs are preserved.
    /// </remarks>
    public static Metadata Flatten(this Metadata metadata)
    {
        var entries = metadata.Entries;

        if (entries is null || entries.Count == 0) return metadata;

        // Quick check: if there is no nesting at all, return early
        bool hasNested = entries.Any(e => e.Entries is { Count: > 0 });

        if (!hasNested) return metadata;

        var flattenedEntries = entries
            .SelectMany(e => e.Traverse())
            .Where(e => e.Entries is null || e.Entries.Count == 0) // leaf nodes only
            .Where(e => !string.IsNullOrWhiteSpace(e.Title))
            .Select(e => e with
            {
                Url = e.Url ?? e.OriginalUrl ?? e.WebpageUrl
            })
            .Where(e => !string.IsNullOrWhiteSpace(e.Url))
            .ToList();

        return metadata with
        {
            Type = "playlist",
            Entries = flattenedEntries
        };
    }

    /// <summary>
    /// Flattens metadata entries into a depth-aware sequence.
    /// </summary>
    /// <param name="metadata">The metadata containing hierarchical entries.</param>
    /// <returns>
    /// A lazy sequence of (Entry, Depth) where Depth represents the nesting level
    /// starting from 0 at the root level.
    /// </returns>
    public static IEnumerable<(Entry Entry, int Depth)> FlattenWithDepth(this Metadata metadata)
    {
        if (metadata.Entries is null || metadata.Entries.Count == 0)
            yield break;

        var stack = new Stack<(Entry entry, int depth)>();

        // push roots (reverse for stable ordering)
        for (int i = metadata.Entries.Count - 1; i >= 0; i--)
            stack.Push((metadata.Entries[i], 0));

        while (stack.Count > 0)
        {
            var (current, depth) = stack.Pop();

            yield return (current, depth);

            if (current.Entries is null || current.Entries.Count == 0)
                continue;

            // push children in reverse to preserve left-to-right order
            for (int i = current.Entries.Count - 1; i >= 0; i--)
            {
                stack.Push((current.Entries[i], depth + 1));
            }
        }
    }
}