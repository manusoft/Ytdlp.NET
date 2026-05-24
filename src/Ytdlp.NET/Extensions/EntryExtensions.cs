namespace ManuHub.Ytdlp.NET.Extensions;

public static class EntryExtensions
{
    public static IEnumerable<Entry> Traverse(this Entry entry)
    {
        yield return entry;

        if (entry.Entries == null || entry.Entries.Count == 0)
            yield break;

        foreach (var child in entry.Entries)
        {
            foreach (var sub in child.Traverse())
            {
                yield return sub;
            }
        }
    }
}