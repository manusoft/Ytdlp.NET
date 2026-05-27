namespace ManuHub.Ytdlp.NET.Extensions;

public static class MetadataExtensions
{
    public static Metadata Flatten(this Metadata metadata)
    {
        if (metadata.Entries?.All(e => !e.Entries.HasChildren()) ?? true)
            return metadata; // No nested playlists, return as is

        // Select end nodes with Title and (null-coalesced) Url
        var flattenedEntries = metadata.Entries
            .SelectMany(e => e.Traverse())
            .Where(e => !e.Entries.HasChildren() && !string.IsNullOrWhiteSpace(e.Title))
            .Select(e => e with { Url = e.Url ?? e.OriginalUrl ?? e.WebpageUrl })
            .Where(e => !string.IsNullOrWhiteSpace(e.Url))
            .ToList();

        return metadata with
        {
            Type = "playlist",
            Entries = flattenedEntries
        };
    }
}