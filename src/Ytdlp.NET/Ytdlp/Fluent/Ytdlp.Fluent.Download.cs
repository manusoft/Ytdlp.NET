namespace ManuHub.Ytdlp.NET;

/// <summary>
/// Fluent configuration methods for Ytdlp. 
/// These methods return a new instance of Ytdlp with the specified option added, allowing for chaining multiple configuration calls in a fluent manner.
/// </summary>
public sealed partial class Ytdlp
{
    // ==================================================================================================================
    // DOWNLOAD OPTIONS
    // ==================================================================================================================

    /// <summary>
    /// Number of fragments of a dash/hlsnative video that should be downloaded concurrently (default is 1)
    /// </summary>
    /// <param name="count"></param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithConcurrentFragments(int count = 8) => count > 0 ? new Ytdlp(this, concurrentFragments: count) : this;

    /// <summary>
    /// Maximum download rate in bytes per second
    /// </summary>
    /// <param name="rate">e.g. 50K or 4.2M</param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithLimitRate(string rate) => AddOption("--limit-rate", rate);

    /// <summary>
    /// Minimum download rate in bytes per second below which throttling is assumed and the video data is re-extracted
    /// </summary>
    /// <param name="rate">e.g. 100K</param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithThrottledRate(string rate) => AddOption("--throttled-rate", rate);

    /// <summary>
    /// Number of retries (default is 10), or -1 for "infinite"
    /// </summary>
    /// <param name="maxRetries"></param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithRetries(int maxRetries) => AddOption("--retries", maxRetries < 0 ? "infinite" : maxRetries.ToString());

    /// <summary>
    /// Number of times to retry on file access error (default is 3), or -1 for "infinite"
    /// </summary>
    /// <param name="maxRetries"></param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithFileAccessRetries(int maxRetries) => AddOption("--file-access-retries", maxRetries < 0 ? "infinite" : maxRetries.ToString());

    /// <summary>
    /// Number of retries for a fragment (default is 10), or -1 for "infinite" (DASH, hlsnative and ISM)
    /// </summary>
    /// <param name="retries"></param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithFragmentRetries(int retries)
    {
        // -1 = infinite
        string value = retries < 0 ? "infinite" : retries.ToString();
        return AddOption("--fragment-retries", value);
    }

    /// <summary>
    /// Sets the sleep behavior between retries using raw yt-dlp expression syntax.
    /// E.g. "linear=1::2", "fragment:exp=1:20", or "http:5".
    /// </summary>
    /// <param name="retrySleepExpression">The retry sleep expression.</param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithRetrySleep(string retrySleepExpression)
    {
        if (string.IsNullOrWhiteSpace(retrySleepExpression)) return this;
        return AddOption("--retry-sleep", retrySleepExpression.Trim());
    }

    /// <summary>
    /// Sets a fixed sleep duration between retries for a specific retry type.
    /// </summary>
    /// <param name="seconds">Time to sleep in seconds.</param>
    /// <param name="type">Optional retry type ("http", "fragment", "file_access", "extractor").</param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithRetrySleep(int seconds, string? type = null)
    {
        if (seconds < 0) return this;
        string prefix = string.IsNullOrWhiteSpace(type) ? "" : $"{type.Trim()}:";
        return AddOption("--retry-sleep", $"{prefix}{seconds}");
    }

    /// <summary>
    /// Configures a linear retry sleep expression: START[:END[:STEP=1]].
    /// </summary>
    /// <param name="start">Initial sleep time in seconds.</param>
    /// <param name="end">Maximum sleep time in seconds (optional).</param>
    /// <param name="step">Increment step size (defaults to 1 if end is specified).</param>
    /// <param name="type">Optional retry type ("http", "fragment", "file_access", "extractor").</param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithLinearRetrySleep(int start, int? end = null, int? step = null, string? type = null)
    {
        string prefix = string.IsNullOrWhiteSpace(type) ? "" : $"{type.Trim()}:";
        string endStr = end.HasValue ? $":{end.Value}" : "";
        string stepStr = step.HasValue ? $":{step.Value}" : "";

        string expr = $"{prefix}linear={start}{endStr}{stepStr}";
        return AddOption("--retry-sleep", expr);
    }

    /// <summary>
    /// Configures an exponential retry sleep expression: START[:END[:BASE=2]].
    /// </summary>
    /// <param name="start">Initial sleep time in seconds.</param>
    /// <param name="end">Maximum sleep time in seconds (optional).</param>
    /// <param name="base">Exponential multiplier base (defaults to 2 if specified).</param>
    /// <param name="type">Optional retry type ("http", "fragment", "file_access", "extractor").</param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithExponentialRetrySleep(int start, int? end = null, double? @base = null, string? type = null)
    {
        string prefix = string.IsNullOrWhiteSpace(type) ? "" : $"{type.Trim()}:";
        string endStr = end.HasValue ? $":{end.Value}" : "";
        string baseStr = @base.HasValue ? $":{@base.Value}" : "";

        string expr = $"{prefix}exp={start}{endStr}{baseStr}";
        return AddOption("--retry-sleep", expr);
    }

    /// <summary>
    /// Skip unavailable fragments for DASH, hlsnative and ISM downloads (default)
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithSkipUnavailableFragments() => AddFlag("--skip-unavailable-fragments");

    /// <summary>
    /// Abort download if a fragment is unavailable
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithAbortOnUnavailableFragments() => AddFlag("--abort-on-unavailable-fragments");

    /// <summary>
    /// Keep downloaded fragments on disk after downloading is finished
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithKeepFragments() => AddFlag("--keep-fragments");

    /// <summary>
    /// Delete downloaded fragments after downloading is finished (default)
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithNoKeepFragments() => AddFlag("--no-keep-fragments");

    /// <summary>
    /// Size of download buffer, (default is 1024) 
    /// </summary>
    /// <param name="size">e.g. 1024 or 16K</param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithBufferSize(string size) => AddOption("--buffer-size", size);

    /// <summary>
    /// Do not automatically adjust the buffer size
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithNoResizeBuffer() => AddFlag("--no-resize-buffer");

    /// <summary>
    /// Size of a chunk for chunk-based HTTP downloading (e.g. "10M", "10485760", "500K").
    /// Useful for bypassing bandwidth throttling imposed by a webserver.
    /// </summary>
    /// <param name="size">Chunk size string (e.g., "10M" or "10485760").</param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithHttpChunkSize(string size)
    {
        if (string.IsNullOrWhiteSpace(size)) return this;
        return AddOption("--http-chunk-size", size.Trim());
    }

    /// <summary>
    /// Size of a chunk for chunk-based HTTP downloading in bytes.
    /// </summary>
    /// <param name="bytes">Chunk size in bytes (e.g., 10_485_760 for 10MB).</param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithHttpChunkSize(long bytes)
    {
        if (bytes <= 0) return this;
        return AddOption("--http-chunk-size", bytes.ToString());
    }

    /// <summary>
    /// Download playlist videos in random order
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithPlaylistRandom() => AddFlag("--playlist-random");

    /// <summary>
    /// Process entries in the playlist as they are received.
    /// Disables n_entries, <see cref="WithPlaylistRandom"/>, and --playlist-reverse.
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithLazyPlaylist() => AddFlag("--lazy-playlist");

    /// <summary>
    /// Process videos in the playlist only after the entire playlist is parsed (default)
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithNoLazyPlaylist() => AddFlag("--no-lazy-playlist");

    /// <summary>
    /// Use the mpegts container for HLS videos; allowing some players to play the video while downloading, 
    /// and reducing the chance of file corruption if download is interrupted. This is enabled by default for live streams
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithHlsUseMpegts() => AddFlag("--hls-use-mpegts");

    /// <summary>
    /// Do not use the mpegts container for HLS videos. This is default when not downloading live streams
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithNoHlsUseMpegts() => AddFlag("--no-hls-use-mpegts");

    /// <summary>
    /// Download only chapters that match the regular expression. A "*" prefix denotes time-range instead of chapter.
    /// Negative timestamps are calculated from the end. "*from-url" can be used to download between the "start_time" and "end_time" extracted from the URL.
    /// Needs ffmpeg. This option can be used multiple times to download multiple sections
    /// </summary>
    /// <param name="regex">e.g. "*10:15-inf", "intro"</param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithDownloadSections(string regex)
    {
        if (string.IsNullOrWhiteSpace(regex)) return this;
        return AddOption("--download-sections", regex);
    }

    /// <summary>
    /// Name or path of the external downloader to use (e.g. "aria2c", "ffmpeg", "curl").
    /// Optionally prefix with protocols, e.g. "dash,m3u8:native".
    /// </summary>
    /// <param name="downloader">Downloader name or protocol specification.</param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithDownloader(string downloader)
    {
        if (string.IsNullOrWhiteSpace(downloader)) return this;
        return AddOption("--downloader", downloader.Trim());
    }

    /// <summary>
    /// Specify an external downloader for explicit protocols (e.g., protocols: ["dash", "m3u8"], downloader: "native").
    /// </summary>
    /// <param name="downloader">Name or path of the downloader (e.g. "aria2c", "native").</param>
    /// <param name="protocols">Protocols to apply this downloader to (e.g. "http", "m3u8", "dash").</param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithDownloader(string downloader, params string[] protocols)
    {
        if (string.IsNullOrWhiteSpace(downloader)) return this;
        if (protocols == null || protocols.Length == 0)
            return AddOption("--downloader", downloader.Trim());

        string protocolList = string.Join(",", protocols.Select(p => p.Trim()));
        return AddOption("--downloader", $"{protocolList}:{downloader.Trim()}");
    }

    /// <summary>
    /// Pass custom arguments to an external downloader (e.g. downloader: "aria2c", args: "-c -j 16").
    /// Supported downloaders are: aria2c, axel, curl, ffmpeg, httpie, httpx, pro aria2c, pro axel, pro curl, pro ffmpeg and pro httpie.
    /// </summary>
    /// <param name="downloaderName">The name of the target downloader (e.g. "aria2c", "ffmpeg").</param>
    /// <param name="args">The arguments to pass.</param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    /// <exception cref="ArgumentException"></exception>
    public Ytdlp WithDownloaderArgs(string downloaderName, string? args)
    {
        if (string.IsNullOrWhiteSpace(downloaderName))
            throw new ArgumentException("Downloader name cannot be empty", nameof(downloaderName));

        var opts = new List<(string, string?)> { ("--downloader", downloaderName.Trim()) };

        if (!string.IsNullOrWhiteSpace(args))
        {
            opts.Add(("--downloader-args", args.Trim()));
        }

        return new Ytdlp(this, extraOptions: opts!);
    }   
}
