using System.Diagnostics.CodeAnalysis;

namespace ManuHub.Ytdlp.NET.Extensions;

/// <summary>
/// Extension methods for traversing hierarchical <see cref="Entry"/> structures.
/// </summary>
public static class EntryExtensions
{
    /// <summary>
    /// Traverses the entire tree starting from the specified entry in depth-first order.
    /// </summary>
    /// <param name="entry">The root entry to start traversal from.</param>
    /// <returns>An enumeration of all entries in the hierarchy including the root.</returns>
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

    /// <summary>
    /// Traverses the entry tree using an iterative depth-first traversal (stack-based).
    /// </summary>
    /// <remarks>
    /// Recommended for very deep or large hierarchies to avoid stack overflow.
    /// </remarks>
    public static IEnumerable<Entry> TraverseIterative(this Entry entry)
    {
        var stack = new Stack<Entry>();
        stack.Push(entry);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            yield return current;

            var children = current.Entries;
            if (children is null || children.Count == 0)
                continue;

            // reverse order to preserve left-to-right traversal order
            for (int i = children.Count - 1; i >= 0; i--)
            {
                stack.Push(children[i]);
            }
        }
    }

    /// <summary>
    /// Determines whether the collection contains at least one entry.
    /// </summary>
    /// <param name="entries">The list of entries to check.</param>
    /// <returns><see langword="true"/> if the list is not null and contains items; otherwise, false.</returns>
    public static bool HasChildren([NotNullWhen(true)]this List<Entry>? entries)
        => entries != null && entries.Count > 0;
}