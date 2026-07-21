namespace ManuHub.Ytdlp.NET;

/// <summary>
/// Fluent configuration methods for Ytdlp. 
/// These methods return a new instance of Ytdlp with the specified option added, allowing for chaining multiple configuration calls in a fluent manner.
/// </summary>
public sealed partial class Ytdlp
{
    // ==================================================================================================================
    // THUMBNAIL OPTIONS
    // ==================================================================================================================

    /// <summary>
    /// Write thumbnail image to disk / Write all thumbnail image formats to disk
    /// </summary>
    /// <param name="allSizes"></param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithThumbnails(bool allSizes = false)
    {
        if (allSizes)
            return AddFlag("--write-all-thumbnails");

        return AddFlag("--write-thumbnail");
    }   
}
