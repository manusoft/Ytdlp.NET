namespace ManuHub.Ytdlp.NET;

/// <summary>
/// Fluent configuration methods for Ytdlp. 
/// These methods return a new instance of Ytdlp with the specified option added, allowing for chaining multiple configuration calls in a fluent manner.
/// </summary>
public sealed partial class Ytdlp
{
    // ==================================================================================================================
    // VIDEO OPTIONS
    // ==================================================================================================================

    /// <summary>
    /// Specify playlist items to download using raw yt-dlp syntax (e.g., "1:3,7,-5::2" or "1-5,8").
    /// </summary>
    /// <param name="items">The raw item specification string.</param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithPlaylistItems(string items)
    {
        if (string.IsNullOrWhiteSpace(items))
            throw new ArgumentException("Playlist items string cannot be empty.", nameof(items));

        return AddOption("--playlist-items", items.Trim());
    }

    /// <summary>
    /// Specify individual playlist item indices to download (e.g., 1, 2, 5, -1).
    /// </summary>
    /// <param name="indices">One or more 1-based playlist indices.</param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithPlaylistItems(params int[] indices)
    {
        if (indices == null || indices.Length == 0) return this;
        return AddOption("--playlist-items", string.Join(",", indices));
    }

    /// <summary>
    /// Specify a playlist item range to download using [start]:[stop][:step].
    /// </summary>
    /// <param name="start">1-based starting index (optional).</param>
    /// <param name="stop">1-based ending index (optional).</param>
    /// <param name="step">Step size/direction (optional, e.g. -1 for reverse).</param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithPlaylistRange(int? start = null, int? stop = null, int? step = null)
    {
        if (!start.HasValue && !stop.HasValue && !step.HasValue) return this;

        string startStr = start?.ToString() ?? "";
        string stopStr = stop?.ToString() ?? "";
        string stepStr = step.HasValue ? $":{step.Value}" : "";

        return AddOption("--playlist-items", $"{startStr}:{stopStr}{stepStr}");
    }

    /// <summary>
    /// Abort download if filesize is smaller than SIZE
    /// </summary>
    /// <param name="size">e.g. 50k or 44.6M</param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    /// <exception cref="ArgumentException"></exception>
    public Ytdlp WithMinFileSize(string size)
    {
        // size examples: 50k, 4.2M, 1G
        if (string.IsNullOrWhiteSpace(size))
            throw new ArgumentException("Size cannot be empty", nameof(size));
        return AddOption("--min-filesize", size.Trim());
    }

    /// <summary>
    /// Abort download if filesize is larger than SIZE
    /// </summary>
    /// <param name="size">e.g. 50k or 44.6M</param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    /// <exception cref="ArgumentException"></exception>
    public Ytdlp WithMaxFileSize(string size)
    {
        if (string.IsNullOrWhiteSpace(size))
            throw new ArgumentException("Size cannot be empty", nameof(size));
        return AddOption("--max-filesize", size.Trim());
    }

    /// <summary>
    /// Download only videos uploaded on this date.
    /// The date can be "YYYYMMDD" or in the format [now|today|yesterday][-N[day|week|month|year]].
    /// E.g. "--date today-2weeks" downloads only videos uploaded on the same day two weeks ago
    /// </summary>
    /// <param name="date">"today-2weeks" or "YYYYMMDD"</param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    /// <exception cref="ArgumentException"></exception>
    public Ytdlp WithDate(string date)
    {
        // formats: YYYYMMDD, today, yesterday, now-2weeks, etc.
        if (string.IsNullOrWhiteSpace(date))
            throw new ArgumentException("Date cannot be empty", nameof(date));
        return AddOption("--date", date.Trim());
    }

    /// <summary>
    /// Download only videos uploaded on or before this date. The date formats accepted are the same as <see cref="WithDate(string)"/>
    /// </summary>
    /// <param name="date">"today-2weeks" or "YYYYMMDD"</param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    /// <exception cref="ArgumentException"></exception>
    public Ytdlp WithDateBefore(string date)
    {
        // formats: YYYYMMDD, today, yesterday, now-2weeks, etc.
        if (string.IsNullOrWhiteSpace(date))
            throw new ArgumentException("Date cannot be empty", nameof(date));
        return AddOption("--datebefore", date.Trim());
    }

    /// <summary>
    /// Download only videos uploaded on or after this date. The date formats accepted are the same as <see cref="WithDate(string)"/>
    /// </summary>
    /// <param name="date">"today-2weeks" or "YYYYMMDD"</param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    /// <exception cref="ArgumentException"></exception>
    public Ytdlp WithDateAfter(string date)
    {
        // formats: YYYYMMDD, today, yesterday, now-2weeks, etc.
        if (string.IsNullOrWhiteSpace(date))
            throw new ArgumentException("Date cannot be empty", nameof(date));
        return AddOption("--dateafter", date.Trim());
    }

    /// <summary>
    /// Generic video filter. Any "OUTPUT TEMPLATE" field can be compared with a number or a string using the operators defined in "Filtering Formats".
    /// </summary>
    /// <param name="filterExpression"></param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    /// <exception cref="ArgumentException"></exception>
    public Ytdlp WithMatchFilter(string filterExpression)
    {
        if (string.IsNullOrWhiteSpace(filterExpression))
            throw new ArgumentException("Match filter expression cannot be empty", nameof(filterExpression));

        return AddOption("--match-filter", filterExpression.Trim());
    }

    /// <summary>
    /// Do not use any match filters (default). Clears any previously set match filters.
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithNoMatchFilters() => AddFlag("--no-match-filters");

    /// <summary>
    /// Same as --match-filter, but stops the download process completely when a video is rejected.
    /// </summary>
    /// <param name="filter">The filter expression (e.g. "like_count &lt; 100", "duration &gt; 600").</param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithBreakMatchFilter(string filter)
    {
        if (string.IsNullOrWhiteSpace(filter)) return this;
        return AddOption("--break-match-filters", filter.Trim());
    }

    /// <summary>
    /// Do not use any break match filters (default). Resets any applied break match filters.
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithNoBreakMatchFilters() => AddFlag("--no-break-match-filters");

    /// <summary>
    /// Download only the video, if the URL refers to a video and a playlist
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithNoPlaylist() => AddFlag("--no-playlist");

    /// <summary>
    /// Download the playlist, if the URL refers to a video and a playlist
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithYesPlaylist() => AddFlag("--yes-playlist");

    /// <summary>
    /// Download only videos suitable for the given age
    /// </summary>
    /// <param name="years"></param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    ///  <exception cref="ArgumentOutOfRangeException"></exception>
    public Ytdlp WithAgeLimit(int years)
    {
        if (years < 0) throw new ArgumentOutOfRangeException(nameof(years));
        return AddOption("--age-limit", years.ToString());
    }

    /// <summary>
    /// Download only videos not listed in the archive file. Record the IDs of all downloaded videos in it
    /// </summary>
    /// <param name="archivePath">Path to the download archive file.</param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    /// <exception cref="ArgumentException"></exception>
    public Ytdlp WithDownloadArchive(string archivePath = "archive.txt")
    {
        if (string.IsNullOrWhiteSpace(archivePath))
            throw new ArgumentException("Archive path cannot be empty", nameof(archivePath));
        return AddOption("--download-archive", Path.GetFullPath(archivePath));
    }

    /// <summary>
    /// Do not use archive file (default)
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithNoDownloadArchive() => AddFlag("--no-download-archive");

    /// <summary>
    /// Abort after downloading number files
    /// </summary>
    /// <param name="count"></param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public Ytdlp WithMaxDownloads(int count)
    {
        if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
        return AddOption("--max-downloads", count.ToString());
    }

    /// <summary>
    /// Stop the download process when encountering a file that is already in the archive supplied with --download-archive.
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithBreakOnExisting() => AddFlag("--break-on-existing");

    /// <summary>
    /// Do not stop the download process when encountering a file that is in the archive (default).
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithNoBreakOnExisting() => AddFlag("--no-break-on-existing");

    /// <summary>
    /// Alters --max-downloads, --break-on-existing, --break-match-filters, and autonumber to reset per input URL.
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithBreakPerInput() => AddFlag("--break-per-input");

    /// <summary>
    /// Causes --break-on-existing and similar options to terminate the entire download queue instead of per input.
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithNoBreakPerInput() => AddFlag("--no-break-per-input");

    /// <summary>
    /// Number of allowed failures until the rest of the playlist is skipped.
    /// </summary>
    /// <param name="allowedFailures">Number of allowed error failures before skipping.</param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithSkipPlaylistAfterErrors(int allowedFailures)
    {
        if (allowedFailures < 0) return this;
        return AddOption("--skip-playlist-after-errors", allowedFailures.ToString());
    }

}
