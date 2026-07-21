namespace ManuHub.Ytdlp.NET;

/// <summary>
/// Fluent configuration methods for Ytdlp. 
/// These methods return a new instance of Ytdlp with the specified option added, allowing for chaining multiple configuration calls in a fluent manner.
/// </summary>
public sealed partial class Ytdlp
{
    // ==================================================================================================================
    // SUBTITLE OPTIONS
    // ==================================================================================================================

    /// <summary>
    /// Write subtitle file
    /// </summary>
    /// <param name="languages">Languages of the subtitles to download (can be regex) or "all" separated by commas, e.g."en.*,ja"
    /// (where "en.*" is a regex pattern that matches "en" followed by 0 or more of any character).
    /// </param>
    /// <param name="auto">Write automatically generated subtitle file</param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithSubtitles(string languages = "all", bool auto = false)
    {
        var flags = new List<string>();

        if (auto)
            flags.Add("--write-auto-subs");
        else
            flags.Add("--write-subs");

        return new Ytdlp(this, extraFlags: flags, extraOptions: new[] { ("--sub-langs", languages) });
    }

}
