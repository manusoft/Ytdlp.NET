namespace ManuHub.Ytdlp.NET;

/// <summary>
/// Fluent configuration methods for Ytdlp. 
/// These methods return a new instance of Ytdlp with the specified option added, allowing for chaining multiple configuration calls in a fluent manner.
/// </summary>
public sealed partial class Ytdlp
{
    // ==================================================================================================================
    // VIDEO FORMAT OPTIONS
    // ==================================================================================================================

    /// <summary>
    /// Video format code
    /// </summary>
    /// <param name="format"></param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithFormat(string format) => new Ytdlp(this, format: format.Trim());

    /// <summary>
    /// Containers that may be used when merging formats, separated by "/", e.g. "mp4/mkv" Ignored if no merge is required.
    /// </summary>
    /// <param name="format">(currently supported: avi, flv, mkv, mov, mp4, webm)</param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithMergeOutputFormat(string format)
    {
        // Common values: mp4, mkv, webm, mov, avi, flv
        if (string.IsNullOrWhiteSpace(format))
            throw new ArgumentException("Merge output format cannot be empty", nameof(format));

        return AddOption("--merge-output-format", format.Trim().ToLowerInvariant());
    }    
}
