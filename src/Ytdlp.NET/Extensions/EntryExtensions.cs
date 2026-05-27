using System.Diagnostics.CodeAnalysis;

namespace ManuHub.Ytdlp.NET.Extensions;

public static class EntryExtensions
{
    public static IEnumerable<Entry> Traverse(this Entry entry)
    {
        yield return entry;

        if (!entry.Entries.HasChildren())
            yield break;

        foreach (var child in entry.Entries)
        {
            foreach (var sub in child.Traverse())
            {
                yield return sub;
            }
        }
    }

    public static bool HasChildren([NotNullWhen(true)]this List<Entry>? entries)
        => entries != null && entries.Count > 0;
}