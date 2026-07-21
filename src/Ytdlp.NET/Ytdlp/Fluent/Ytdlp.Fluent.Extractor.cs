namespace ManuHub.Ytdlp.NET;

/// <summary>
/// Fluent configuration methods for Ytdlp. 
/// These methods return a new instance of Ytdlp with the specified option added, allowing for chaining multiple configuration calls in a fluent manner.
/// </summary>
public sealed partial class Ytdlp
{
    // ==================================================================================================================
    // EXTRACTOR AND STREAM OPTIONS
    // ==================================================================================================================

    /// <summary>
    /// Number of retries for known extractor errors (default is 3).
    /// </summary>
    /// <param name="retries">Number of retries.</param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithExtractorRetries(int retries)
    {
        if (retries < 0) return this;
        return AddOption("--extractor-retries", retries.ToString());
    }

    /// <summary>
    /// Set extractor retries to "infinite".
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithInfiniteExtractorRetries() => AddOption("--extractor-retries", "infinite");

    /// <summary>
    /// Process dynamic DASH manifests (default). (Alias: --no-ignore-dynamic-mpd)
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithAllowDynamicMpd() => AddFlag("--allow-dynamic-mpd");

    /// <summary>
    /// Do not process dynamic DASH manifests. (Alias: --no-allow-dynamic-mpd)
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithIgnoreDynamicMpd() => AddFlag("--ignore-dynamic-mpd");

    /// <summary>
    /// Split HLS playlists to different formats at discontinuities such as ad breaks.
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithHlsSplitDiscontinuity() => AddFlag("--hls-split-discontinuity");

    /// <summary>
    /// Do not split HLS playlists into different formats at discontinuities such as ad breaks (default).
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithNoHlsSplitDiscontinuity() => AddFlag("--no-hls-split-discontinuity");

    /// <summary>
    /// Pass arguments to a specific extractor (e.g. key: "youtube", args: "skip=dash,hls").
    /// </summary>
    /// <param name="extractorKey">The extractor key (e.g., "youtube", "twitch").</param>
    /// <param name="args">The arguments to pass to the extractor.</param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithExtractorArgs(string extractorKey, string args)
    {
        if (string.IsNullOrWhiteSpace(extractorKey) || string.IsNullOrWhiteSpace(args)) return this;
        return AddOption("--extractor-args", $"{extractorKey.Trim()}:{args.Trim()}");
    }
}
