using ManuHub.Ytdlp.NET.Core;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace ManuHub.Ytdlp.NET;

/// <summary>
/// Fluent, immutable wrapper for yt-dlp providing methods to build commands,
/// fetch metadata, and execute downloads with progress tracking and event support.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Immutable fluent builder:</strong>
/// Every configuration method (e.g. <see cref="WithOutputFolder"/>, <see cref="WithFormat"/>,
/// <see cref="WithExtractAudio"/>) returns a new <see cref="Ytdlp"/> instance.
/// The original instance is never modified. This makes it safe to branch configurations
/// from a shared base without any side effects:
/// </para>
/// <code>
/// var base    = new Ytdlp("yt-dlp").WithOutputFolder(@"D:\Downloads");
/// var audioOnly = base.WithExtractAudio(AudioFormat.Mp3);
/// var videoHD   = base.With1080pOrBest().WithEmbedThumbnail();
/// // base, audioOnly and videoHD are fully independent instances
/// </code>
///
/// <para>
/// <strong>Thread-safe:</strong>
/// A single <see cref="Ytdlp"/> instance can be shared across threads and called
/// concurrently without synchronization. Each execution method creates its own
/// isolated <see cref="ProcessRunner"/> and <see cref="ProgressParser"/> internally,
/// with no shared mutable state between calls:
/// </para>
/// <code>
/// var ytdlp = new Ytdlp("yt-dlp").WithOutputFolder(@"D:\Downloads").With1080pOrBest();
///
/// // All three run concurrently against the same configured instance — safe
/// await Task.WhenAll(urls.Select(url => ytdlp.DownloadAsync(url, ct)));
/// </code>
///
/// <para>
/// <strong>No disposal required:</strong>
/// <see cref="Ytdlp"/> holds no unmanaged resources and does not implement
/// <see cref="IDisposable"/> or <see cref="IAsyncDisposable"/>. Instances are plain
/// configuration objects — create them, share them freely, and let the GC collect
/// them when they go out of scope. All internal runners and parsers are created
/// per-call and cleaned up automatically after each execution.
/// </para>
///
/// <para>
/// <strong>Event forwarding:</strong>
/// Progress, output, and error events are forwarded from the internal runners and
/// parsers for each execution. Subscriptions are established before each call and
/// unsubscribed in a <c>finally</c> block afterwards, preventing memory leaks even
/// if the download is cancelled or throws.
/// </para>
///
/// <para>
/// <strong>Typical usage:</strong>
/// </para>
/// <code>
/// var ytdlp = new Ytdlp("yt-dlp")
///     .WithOutputFolder(@"D:\Downloads")
///     .With1080pOrBest()
///     .WithEmbedMetadata()
///     .WithEmbedChapters();
///
/// ytdlp.OnProgressDownload += (_, e) => Console.WriteLine($"{e.Percent}%");
/// ytdlp.OnErrorMessage     += (_, msg) => Console.WriteLine($"Error: {msg}");
///
/// await ytdlp.DownloadAsync("https://youtube.com/watch?v=xxx", ct);
/// </code>
/// </remarks>
public sealed class Ytdlp
{
    // ==================================================================================================================
    // Immutable configuration fields events and flags and contructors
    // ==================================================================================================================
    #region Frozen configuration
    private readonly string _ytdlpPath;
    private readonly ILogger _logger;

    private readonly string? _outputFolder;
    private readonly string? _homeFolder;
    private readonly string? _tempFolder;
    private readonly string _outputTemplate;
    private readonly string _format;
    private readonly string? _cookiesFile;
    private readonly string? _cookiesFromBrowser;
    private readonly string? _proxy;
    private readonly string? _ffmpegLocation;
    private readonly string? _sponsorblockRemove;
    private readonly int? _concurrentFragments;

    private readonly ImmutableArray<string> _flags;
    private readonly ImmutableArray<(string Key, string Value)> _options;
    #endregion

    #region Static JSON options for metadata parsing
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    #endregion

    #region Events
    public event EventHandler<DownloadProgressEventArgs>? OnProgressDownload;
    public event EventHandler<string>? OnProgressMessage;
    public event EventHandler<string>? OnOutputMessage;
    public event EventHandler<string>? OnCompleteDownload;
    public event EventHandler<string>? OnPostProcessingStart;
    public event EventHandler<string>? OnPostProcessingComplete;
    public event EventHandler<CommandCompletedEventArgs>? OnCommandCompleted;
    public event EventHandler<string>? OnErrorMessage;
    #endregion

    #region Constructors

    public Ytdlp(string ytdlpPath = "yt-dlp", ILogger? logger = null)
    {
        _ytdlpPath = ValidatePath(ytdlpPath);
        _logger = logger ?? new DefaultLogger();

        // defaults
        _outputFolder = null;
        _tempFolder = null;
        _homeFolder = null;
        _outputTemplate = "%(title)s [%(id)s].%(ext)s";
        _format = "b";
        _concurrentFragments = null;
        _flags = ImmutableArray<string>.Empty;
        _options = ImmutableArray<(string, string)>.Empty;
        _cookiesFile = null;
        _cookiesFromBrowser = null;
        _proxy = null;
        _ffmpegLocation = null;
        _sponsorblockRemove = null;
    }

    // Private copy constructor – every WithXxx() uses this
    private Ytdlp(Ytdlp other,
        string? outputFolder = null,
        string? homeFolder = null,
        string? tempFolder = null,
        string? outputTemplate = null,
        string? format = null,
        int? concurrentFragments = null,
        string? cookiesFile = null,
        string? cookiesFromBrowser = null,
        string? proxy = null,
        string? ffmpegLocation = null,
        string? sponsorblockRemove = null,
        IEnumerable<string>? extraFlags = null,
        IEnumerable<(string, string)>? extraOptions = null)
    {
        _ytdlpPath = other._ytdlpPath;
        _logger = other._logger;

        _outputFolder = outputFolder ?? other._outputFolder;
        _homeFolder = homeFolder ?? other._homeFolder;
        _tempFolder = tempFolder ?? other._tempFolder;
        _outputTemplate = outputTemplate ?? other._outputTemplate;

        _format = format ?? other._format;
        _concurrentFragments = concurrentFragments ?? other._concurrentFragments;
        _cookiesFile = cookiesFile ?? other._cookiesFile;
        _cookiesFromBrowser = cookiesFromBrowser ?? other._cookiesFromBrowser;
        _proxy = proxy ?? other._proxy;
        _ffmpegLocation = ffmpegLocation ?? other._ffmpegLocation;
        _sponsorblockRemove = sponsorblockRemove ?? other._sponsorblockRemove;

        _flags = extraFlags is null ? other._flags : other._flags.AddRange(extraFlags);
        _options = extraOptions is null ? other._options : other._options.AddRange(extraOptions);
    }

    #endregion

    // ==================================================================================================================
    // Fluent configuration methods
    // ==================================================================================================================

    #region General Options

    /// <summary>
    /// Ignore download and postprocessing errors. The download will be considered successful even if the postprocessing fails
    /// </summary>
    public Ytdlp WithIgnoreErrors() => AddFlag("--ignore-errors");

    /// <summary>
    /// IgAbort downloading of further videos if an error occurs 
    /// </summary>
    public Ytdlp WithAbortOnError() => AddFlag("--abort-on-error");

    /// <summary>
    /// Don't load any more configuration files except those given to <see cref="WithConfigLocations(string)"/>.
    /// For backward compatibility, if this option is found inside the system configuration file, the user configuration is not loaded.
    /// </summary>
    public Ytdlp WithIgnoreConfig() => AddFlag("--ignore-config");

    /// <summary>
    /// Location of the main configuration file;either the path to the config or its containing directory ("-" for stdin). 
    /// Can be used multiple times and inside other configuration files.
    /// </summary>
    /// <param name="path"></param>
    public Ytdlp WithConfigLocations(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Config folder path required");
        return AddOption("--config-locations", Path.GetFullPath(path));
    }

    /// <summary>
    /// Path to an additional directory to search for plugins. This option can be used multiple times to add multiple directories.
    /// </summary>
    /// <param name="path"></param>
    public Ytdlp WithPluginDirs(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("plugin folder path required");
        return AddOption("--plugin-dirs", path);
    }

    /// <summary>
    /// Clear plugin directories to search, including defaults and those provided by previous <see cref="WithPluginDirs(string)"/>
    /// </summary>
    public Ytdlp WithNoPluginDirs() => AddFlag("--no-plugin-dirs");

    /// <summary>
    /// Additional JavaScript runtime to enable, with an optional location for the runtime (either the path to the binary or its containing directory).
    /// This option can be used multiple times to enable multiple runtimes. Supported runtimes are (in order of priority, from highest to lowest): deno, node, quickjs, bun.
    /// Only "deno" is enabled by default. The highest priority runtime that is both enabled and available will be used. 
    /// In order to use a lower priority runtime when "deno" is available, <see cref="WithNoJsRuntime"/> needs to be passed before enabling other runtimes
    /// </summary>
    /// <param name="runtime">Supported runtimes are deno, node, quickjs, bun</param>
    /// <param name="runtimePath"></param>
    public Ytdlp WithJsRuntime(Runtime runtime, string runtimePath)
    {
        var builder = $"{runtime}:{runtimePath}";
        return AddOption("--js-runtime", builder);
    }

    /// <summary>
    /// Clear JavaScript runtimes to enable, including defaults and those provided by <see cref="WithJsRuntime(Runtime, string)"/>
    /// </summary>
    public Ytdlp WithNoJsRuntime() => AddFlag("--no-js-runtime");

    /// <summary>
    /// Do not extract a playlist's URL result entries; some entry metadata may be missing and downloading may be bypassed
    /// </summary>
    public Ytdlp WithFlatPlaylist() => AddFlag("--flat-playlist");

    /// <summary>
    /// Download livestreams from the start. Currently experimental and only supported for YouTube, Twitch, and TVer.
    /// </summary>
    public Ytdlp WithLiveFromStart() => AddFlag("--live-from-start");

    /// <summary>
    /// Wait for scheduled streams to become available.Pass the minimum number of seconds(or range) to wait between retries
    /// </summary>
    /// <param name="maxWait"></param>
    public Ytdlp WithWaitForVideo(TimeSpan? maxWait = null)
    {
        var opts = new List<(string Key, string? Value)>();

        var waitValue = maxWait.HasValue && maxWait.Value.TotalSeconds > 0
            ? maxWait.Value.TotalSeconds.ToString("F0")
            : "any";   // "any" = wait indefinitely or until timeout

        opts.Add(("--wait-for-video", waitValue));

        return new Ytdlp(this, extraOptions: opts!);
    }

    /// <summary>
    /// Mark videos watched (even with Simulate())
    /// </summary>
    public Ytdlp WithMarkWatched() => AddFlag("--mark-watched");

    #endregion

    #region Network Options

    /// <summary>
    /// Use the specified HTTP/HTTPS/SOCKS proxy. To enable SOCKS proxy, specify a proper scheme, e.g. socks5://user:pass@127.0.0.1:1080/.
    /// </summary>
    /// <param name="url">Pass in an empty string for direct connection</param>
    public Ytdlp WithProxy(string? proxy) => string.IsNullOrWhiteSpace(proxy) ? this : new Ytdlp(this, proxy: proxy);

    /// <summary>
    /// Time to wait before giving up, in seconds
    /// </summary>
    /// <param name="timeout"></param>
    public Ytdlp WithSocketTimeout(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero) return this;
        double seconds = timeout.TotalSeconds;
        return AddOption("--socket-timeout", seconds.ToString("F0"));
    }

    /// <summary>
    /// Make all connections via IPv4
    /// </summary>
    public Ytdlp WithForceIpv4() => AddFlag("--force-ipv4");

    /// <summary>
    /// Make all connections via IPv6
    /// </summary>
    public Ytdlp WithForceIpv6() => AddFlag("--force-ipv6");

    /// <summary>
    /// Enable file:// URLs. This is disabled by default for security reasons.
    /// </summary>
    public Ytdlp WithEnableFileUrls() => AddFlag("--enable-file-urls");

    #endregion

    #region Geo-restriction

    /// <summary>
    /// Use this proxy to verify the IP address for some geo-restricted sites. 
    /// The default proxy specified by <see cref="WithProxy(string?)"/> (or none, if the option is not present) is used for the actual downloading
    /// </summary>
    /// <param name="url"></param>
    public Ytdlp WithGeoVerificationProxy(string url) => AddOption("--geo-verification-proxy", url);

    /// <summary>
    /// How to fake X-Forwarded-For HTTP header to try bypassing geographic restriction. One of "default" (only when known to be useful),
    /// "never", an IP block in CIDR notation, or a two-letter ISO 3166-2 country code
    /// </summary>
    /// <param name="countryCode"></param>
    /// <exception cref="ArgumentException"></exception>
    public Ytdlp WithGeoBypassCountry(string countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode) || countryCode.Length != 2) throw new ArgumentException("Country code must be 2 letters.");
        return AddOption("--xff", countryCode.ToUpper());
    }

    #endregion

    #region Video Selection

    /// <summary>
    /// Comma-separated playlist_index of the items to download. You can specify a range using "[START]:[STOP][:STEP]".
    /// For backward compatibility, START-STOP is also supported. Use negative indices to count from the right and negative STEP to download in reverse order.
    /// E.g. "1:3,7,-5::2" used on a playlist of size 15 will download the items at index 1,2,3,7,11,13,15
    /// </summary>
    /// <param name="items"></param>
    /// <exception cref="ArgumentException"></exception>
    public Ytdlp WithPlaylistItems(string items)
    {
        if (string.IsNullOrWhiteSpace(items))
            throw new ArgumentException("Playlist items string cannot be empty.", nameof(items));
        return AddOption("--playlist-items", items.Trim());
    }

    /// <summary>
    /// Abort download if filesize is smaller than SIZE
    /// </summary>
    /// <param name="size">e.g. 50k or 44.6M</param>
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
    /// <exception cref="ArgumentException"></exception>
    public Ytdlp WithMatchFilter(string filterExpression)
    {
        if (string.IsNullOrWhiteSpace(filterExpression))
            throw new ArgumentException("Match filter expression cannot be empty", nameof(filterExpression));

        return AddOption("--match-filter", filterExpression.Trim());
    }

    /// <summary>
    /// Download only the video, if the URL refers to a video and a playlist
    /// </summary>
    public Ytdlp WithNoPlaylist() => AddFlag("--no-playlist");

    /// <summary>
    /// Download the playlist, if the URL refers to a video and a playlist
    /// </summary>
    public Ytdlp WithYesPlaylist() => AddFlag("--yes-playlist");

    /// <summary>
    /// Download only videos suitable for the given age
    /// </summary>
    /// <param name="years"></param>
    public Ytdlp WithAgeLimit(int years)
    {
        if (years < 0) throw new ArgumentOutOfRangeException(nameof(years));
        return AddOption("--age-limit", years.ToString());
    }

    /// <summary>
    /// Download only videos not listed in the archive file. Record the IDs of all downloaded videos in it
    /// </summary>
    /// <param name="archivePath"></param>
    /// <exception cref="ArgumentException"></exception>
    public Ytdlp WithDownloadArchive(string archivePath = "archive.txt")
    {
        if (string.IsNullOrWhiteSpace(archivePath))
            throw new ArgumentException("Archive path cannot be empty", nameof(archivePath));
        return AddOption("--download-archive", Path.GetFullPath(archivePath));
    }

    /// <summary>
    /// Abort after downloading number files
    /// </summary>
    /// <param name="count"></param>
    public Ytdlp WithMaxDownloads(int count)
    {
        if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
        return AddOption("--max-downloads", count.ToString());
    }

    /// <summary>
    /// Stop the download process when encountering a file that is in the archive supplied with the <see cref="WithDownloadArchive(string)" /> option
    /// </summary>
    public Ytdlp WithBreakOnExisting() => AddFlag("--break-on-existing");

    #endregion

    #region Download Options

    /// <summary>
    /// Number of fragments of a dash/hlsnative video that should be downloaded concurrently (default is 1)
    /// </summary>
    /// <param name="count"></param>
    public Ytdlp WithConcurrentFragments(int count = 8) => count > 0 ? new Ytdlp(this, concurrentFragments: count) : this;

    /// <summary>
    /// Maximum download rate in bytes per second
    /// </summary>
    /// <param name="rate">e.g. 50K or 4.2M</param>
    public Ytdlp WithLimitRate(string rate) => AddOption("--limit-rate", rate);

    /// <summary>
    /// Minimum download rate in bytes per second below which throttling is assumed and the video data is re-extracted
    /// </summary>
    /// <param name="rate">e.g. 100K</param>
    public Ytdlp WithThrottledRate(string rate) => AddOption("--throttled-rate", rate);

    /// <summary>
    /// Number of retries (default is 10), or -1 for "infinite"
    /// </summary>
    /// <param name="maxRetries"></param>
    public Ytdlp WithRetries(int maxRetries) => AddOption("--retries", maxRetries < 0 ? "infinite" : maxRetries.ToString());

    /// <summary>
    /// Number of times to retry on file access error (default is 3), or -1 for "infinite"
    /// </summary>
    /// <param name="maxRetries"></param>
    public Ytdlp WithFileAccessRetries(int maxRetries) => AddOption("--file-access-retries", maxRetries < 0 ? "infinite" : maxRetries.ToString());

    /// <summary>
    /// Number of retries for a fragment (default is 10), or -1 for "infinite" (DASH, hlsnative and ISM)
    /// </summary>
    /// <param name="maxRetries"></param>
    public Ytdlp WithFragmentRetries(int retries)
    {
        // -1 = infinite
        string value = retries < 0 ? "infinite" : retries.ToString();
        return AddOption("--fragment-retries", value);
    }

    /// <summary>
    /// Skip unavailable fragments for DASH, hlsnative and ISM downloads (default)
    /// </summary>
    public Ytdlp WithSkipUnavailableFragments() => AddFlag("--skip-unavailable-fragments");

    /// <summary>
    /// Abort download if a fragment is unavailable
    /// </summary>
    public Ytdlp WithAbortOnUnavailableFragments() => AddFlag("--abort-on-unavailable-fragments");

    /// <summary>
    /// Keep downloaded fragments on disk after downloading is finished
    /// </summary>
    public Ytdlp WithKeepFragments() => AddFlag("--keep-fragments");

    /// <summary>
    /// Size of download buffer, (default is 1024) 
    /// </summary>
    /// <param name="size">e.g. 1024 or 16K</param>
    public Ytdlp WithBufferSize(string size) => AddOption("--buffer-size", size);

    /// <summary>
    /// Do not automatically adjust the buffer size
    /// </summary>
    public Ytdlp WithNoResizeBuffer() => AddFlag("--no-resize-buffer");

    /// <summary>
    /// Download playlist videos in random order
    /// </summary>
    public Ytdlp WithPlaylistRandom() => AddFlag("--playlist-random");

    /// <summary>
    /// Use the mpegts container for HLS videos; allowing some players to play the video while downloading, 
    /// and reducing the chance of file corruption if download is interrupted. This is enabled by default for live streams
    /// </summary>
    public Ytdlp WithHlsUseMpegts() => AddFlag("--hls-use-mpegts");

    /// <summary>
    /// Do not use the mpegts container for HLS videos. This is default when not downloading live streams
    /// </summary>
    public Ytdlp WithNoHlsUseMpegts() => AddFlag("--no-hls-use-mpegts");


    /// <summary>
    /// Download only chapters that match the regular expression. A "*" prefix denotes time-range instead of chapter.
    /// Negative timestamps are calculated from the end. "*from-url" can be used to download between the "start_time" and "end_time" extracted from the URL.
    /// Needs ffmpeg. This option can be used multiple times to download multiple sections
    /// </summary>
    /// <param name="regex">e.g. "*10:15-inf", "intro"</param>
    public Ytdlp WithDownloadSections(string regex)
    {
        if (string.IsNullOrWhiteSpace(regex)) return this;
        return AddOption("--download-sections", regex);
    }


    #endregion

    #region Filesystem Options

    /// <summary>
    /// Sets the home folder for yt-dlp (used for config or as base directory).
    /// Path is automatically normalized and quoted.
    /// </summary>
    /// <param name="path"></param>
    /// <exception cref="ArgumentException"></exception>
    public Ytdlp WithHomeFolder(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Home folder path required");
        return new Ytdlp(this, homeFolder: Path.GetFullPath(path).Replace('\\', '/'));
    }

    /// <summary>
    /// Sets the temporary folder for yt-dlp intermediate files (fragments, etc.).
    /// Path is automatically normalized and quoted.
    /// </summary>
    /// <param name="path"></param>
    /// <exception cref="ArgumentException"></exception>
    public Ytdlp WithTempFolder(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Temp folder path required");
        return new Ytdlp(this, tempFolder: Path.GetFullPath(path).Replace('\\', '/'));
    }

    /// <summary>
    /// Sets the output folder
    /// </summary>
    /// <param name="path"></param>
    /// <exception cref="ArgumentException"></exception>
    public Ytdlp WithOutputFolder(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Output folder path required");
        return new Ytdlp(this, outputFolder: Path.GetFullPath(path).Replace('\\', '/'));
    }

    /// <summary>
    /// Output filename template
    /// </summary>
    /// <param name="template"></param>
    public Ytdlp WithOutputTemplate(string template)
    {
        if (string.IsNullOrWhiteSpace(template)) throw new ArgumentException("Template required");
        return new Ytdlp(this, outputTemplate: template.Trim());
    }

    /// <summary>
    /// Restrict filenames to only ASCII characters, and avoid "&" and spaces in filenames
    /// </summary>
    public Ytdlp WithRestrictFilenames() => AddFlag("--restrict-filenames");

    /// <summary>
    /// Force filenames to be Windows-compatible
    /// </summary>
    public Ytdlp WithWindowsFilenames() => AddFlag("--windows-filenames");

    /// <summary>
    /// Limit the filename length (excluding extension) to the specified number of characters
    /// </summary>
    /// <param name="length"></param>
    public Ytdlp WithTrimFilenames(int length)
    {
        if (length < 10)
            throw new ArgumentOutOfRangeException(nameof(length), "Length should be at least 10 characters");

        return AddOption("--trim-filenames", length.ToString());
    }

    /// <summary>
    /// Do not overwrite any files
    /// </summary>
    public Ytdlp WithNoOverwrites() => AddFlag("--no-overwrites");

    /// <summary>
    /// Overwrite all video and metadata files. This option includes <see cref="WithNoContinue" />
    /// </summary>
    public Ytdlp WithForceOverwrites() => AddFlag("--force-overwrites");

    /// <summary>
    /// Do not resume partially downloaded fragments. If the file is not fragmented, restart download of the entire file
    /// </summary>
    public Ytdlp WithNoContinue() => AddFlag("--no-continue");

    /// <summary>
    /// Do not use .part files - write directly into output file
    /// </summary>
    public Ytdlp WithNoPart() => AddFlag("--no-part");

    /// <summary>
    /// Use the Last-modified header to set the file modification time
    /// </summary>
    public Ytdlp WithMtime() => AddFlag("--mtime");

    /// <summary>
    /// Write video description to a .description file
    /// </summary>
    public Ytdlp WithWriteDescription() => AddFlag("--write-description");

    /// <summary>
    /// Write video metadata to a .info.json file (this may contain personal information)
    /// </summary>
    public Ytdlp WithWriteInfoJson() => AddFlag("--write-info-json");

    /// <summary>
    /// Do not write playlist metadata when using <see cref="WithWriteInfoJson"/>, <see cref="WithWriteDescription"/>
    /// </summary>
    public Ytdlp WithNoWritePlaylistMetafiles() => AddFlag("--no-write-playlist-metafiles");

    /// <summary>
    /// Write all fields to the infojson
    /// </summary>
    public Ytdlp WithNoCleanInfoJson() => AddFlag("--no-clean-info-json");

    /// <summary>
    /// Retrieve video comments to be placed in the infojson. The comments are fetched even without this option if the extraction is known to be quick
    /// </summary>
    public Ytdlp WithWriteComments() => AddFlag("--write-comments");

    /// <summary>
    /// Do not retrieve video comments unless the extraction is known to be quick
    /// </summary>
    public Ytdlp WithNoWriteComments() => AddFlag("--no-write-comments");

    /// <summary>
    /// JSON file containing the video information (created with the WriteVideoMetadata() option)
    /// </summary>
    /// <param name="path">*.json</param>
    public Ytdlp WithLoadInfoJson(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Json file path cannot be empty.", nameof(path));
        return AddOption("--load-info-json", path);
    }

    /// <summary>
    /// Netscape formatted file to read cookies from and dump cookie jar in
    /// </summary>
    /// <param name="path"></param>
    /// <exception cref="ArgumentException"></exception>
    public Ytdlp WithCookiesFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Cookie file path cannot be empty.", nameof(path));
        return new Ytdlp(this, cookiesFile: Path.GetFullPath(path));
    }

    /// <summary>
    /// The name of the browser to load cookies from. Currently supported browsers are: brave, chrome, chromium, edge, firefox, opera, safari, vivaldi, whale.
    /// Optionally, the KEYRING used for decrypting Chromium cookies on Linux, the name/path of the PROFILE to load cookies from, and the CONTAINER name (if Firefox) 
    /// ("none" for no container) can be given with their respective separators. By default, all containers of the most recently accessed profile are used.
    /// keyrings are: basictext, gnomekeyring, kwallet, kwallet5, kwallet6
    /// </summary>
    /// <param name="browser"></param>
    public Ytdlp WithCookiesFromBrowser(string browser) => new Ytdlp(this, cookiesFromBrowser: browser);

    /// <summary>
    /// Disable filesystem caching
    /// </summary>
    public Ytdlp WithNoCacheDir() => AddFlag("--no-cache-dir");

    /// <summary>
    /// Delete all filesystem cache files
    /// </summary>
    public Ytdlp WithRemoveCacheDir() => AddFlag("--rm-cache-dir");

    #endregion

    #region Thumbnail Options

    /// <summary>
    /// Write thumbnail image to disk / Write all thumbnail image formats to disk
    /// </summary>
    /// <param name="allSizes"></param>
    public Ytdlp WithThumbnails(bool allSizes = false)
    {
        if (allSizes)
            return AddFlag("--write-all-thumbnails");

        return AddFlag("--write-thumbnail");
    }


    #endregion

    #region Verbosity and Simulation Options

    /// <summary>
    /// Activate quiet mode. If used with --verbose, print the log to stderr
    /// </summary>
    public Ytdlp WithQuiet() => AddFlag("--quiet");

    /// <summary>
    /// Ignore warnings
    /// </summary>
    public Ytdlp WithNoWarnings() => AddFlag("--no-warnings");

    /// <summary>
    /// Do not download the video and do not write anything to disk
    /// </summary>
    public Ytdlp WithSimulate() => AddFlag("--simulate");

    /// <summary>
    /// Download the video even if printing/listing options are used
    /// </summary>
    public Ytdlp WithNoSimulate() => AddFlag("--no-simulate");

    /// <summary>
    /// Do not download the video but write all related files (Alias: --no-download)
    /// </summary>
    public Ytdlp WithSkipDownload() => AddFlag("--skip-download");

    /// <summary>
    /// Print various debugging information
    /// </summary>
    public Ytdlp WithVerbose() => AddFlag("--verbose");

    #endregion

    #region Workgrounds

    /// <summary>
    /// Force the specified encoding (experimental)
    /// </summary>
    /// <param name="encoding"></param>
    public Ytdlp WithEncoding(string encoding) => AddOption("--encoding", encoding);

    /// <summary>
    /// Explicitly allow HTTPS connection to servers that do not support RFC 5746 secure renegotiation
    /// </summary>
    public Ytdlp WithLegacyServerConnect() => AddFlag("--legacy-server-connect");

    /// <summary>
    /// Suppress HTTPS certificate validation
    /// </summary>
    public Ytdlp WithNoCheckCertificate() => AddFlag("--no-check-certificate");

    /// <summary>
    /// Use an unencrypted connection to retrieve information about the video (Currently supported only for YouTube)
    /// </summary>
    public Ytdlp WithPreferInsecure() => AddFlag("--prefer-insecure");

    /// <summary>
    /// Specify a custom HTTP header and its value. You can use this option multiple times
    /// </summary>
    /// <param name="header">"Referer" "User-Agent"</param>
    /// <param name="value">"URL", "UA"</param>
    /// <exception cref="ArgumentException"></exception>
    public Ytdlp WithAddHeader(string header, string value)
    {
        if (string.IsNullOrWhiteSpace(header) || string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Header and value cannot be empty.");
        return AddOption("--add-headers", $"{header}:{value}");
    }

    /// <summary>
    /// Work around terminals that lack bidirectional text support. Requires bidiv or fribidi executable in PATH
    /// </summary>
    public Ytdlp WithBidiWorkaround() => AddFlag("--bidi-workaround");

    /// <summary>
    /// Number of seconds to sleep between requests during data extraction
    /// </summary>
    /// <param name="seconds"></param>
    public Ytdlp WithSleepRequest(double seconds)
    {
        if (seconds <= 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        return AddOption("--sleep-request", seconds.ToString("F2", CultureInfo.InvariantCulture));
    }

    /// <summary>
    ///  Number of seconds to sleep between requests during data extraction, Maximum number of seconds to sleep. 
    /// </summary>
    /// <param name="seconds"></param>
    /// <param name="maxSeconds"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public Ytdlp WithSleepInterval(double seconds, double? maxSeconds = null)
    {
        if (seconds <= 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        var opts = new List<(string, string?)> { ("--sleep-interval", seconds.ToString("F2", CultureInfo.InvariantCulture)) };
        if (maxSeconds.HasValue && maxSeconds > seconds)
        {
            opts.Add(("--max-sleep-interval", maxSeconds.Value.ToString("F2", CultureInfo.InvariantCulture)));
        }
        return new Ytdlp(this, extraOptions: opts!);
    }

    /// <summary>
    /// Number of seconds to sleep before each subtitle download
    /// </summary>
    /// <param name="seconds"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public Ytdlp WithSleepSubtitles(double seconds)
    {
        if (seconds <= 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        return AddOption("--sleep-subtitles", seconds.ToString("F2", CultureInfo.InvariantCulture));
    }

    #endregion

    #region Video Format Options

    /// <summary>
    /// Video format code
    /// </summary>
    /// <param name="format"></param>
    public Ytdlp WithFormat(string format) => new Ytdlp(this, format: format.Trim());

    /// <summary>
    /// Containers that may be used when merging formats, separated by "/", e.g. "mp4/mkv" Ignored if no merge is required.
    /// </summary>
    /// <param name="format">(currently supported: avi, flv, mkv, mov, mp4, webm)</param>
    public Ytdlp WithMergeOutputFormat(string format)
    {
        // Common values: mp4, mkv, webm, mov, avi, flv
        if (string.IsNullOrWhiteSpace(format))
            throw new ArgumentException("Merge output format cannot be empty", nameof(format));

        return AddOption("--merge-output-format", format.Trim().ToLowerInvariant());
    }

    #endregion

    #region Subtitle Options 

    /// <summary>
    /// Write subtitle file
    /// </summary>
    /// <param name="languages">Languages of the subtitles to download (can be regex) or "all" separated by commas, e.g."en.*,ja"
    /// (where "en.*" is a regex pattern that matches "en" followed by 0 or more of any character).
    /// </param>
    /// <param name="auto">Write automatically generated subtitle file</param>
    public Ytdlp WithSubtitles(string languages = "all", bool auto = false)
    {
        var flags = new List<string>();

        if (auto)
            flags.Add("--write-auto-subs");
        else
            flags.Add("--write-subs");

        return new Ytdlp(this, extraFlags: flags, extraOptions: new[] { ("--sub-langs", languages) });
    }

    #endregion

    #region Authentication Options

    /// <summary>
    /// Login with this account ID and account password.
    /// </summary>
    /// <param name="username">Account ID</param>
    /// <param name="password">Account password</param>
    /// <remarks>
    /// <b>Security warning:</b> Credentials are passed as command-line arguments and are
    /// visible in system process listings (e.g. Task Manager, <c>ps aux</c>).
    /// Prefer <see cref="WithCookiesFile"/> or <see cref="WithCookiesFromBrowser"/> where possible.
    /// </remarks>
    /// <exception cref="ArgumentException"></exception>
    public Ytdlp WithAuthentication(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Username and password cannot be empty.");
        return this.AddOption("--username", username).AddOption("--password", password);
    }

    /// <summary>
    /// Two-factor authentication code
    /// </summary>
    /// <param name="code">Two-factor Code</param>
    public Ytdlp WithTwoFactor(string code) => AddOption("--twofactor", code);

    /// <summary>
    /// Video-specific password
    /// </summary>
    /// <remarks>
    /// <b>Security warning:</b> Credentials are passed as command-line arguments and are
    /// visible in system process listings (e.g. Task Manager, <c>ps aux</c>).
    /// </remarks>
    /// <param name="password"></param>
    public Ytdlp WithVideoPassword(string password) => AddOption("--video-password", password);

    /// <summary>
    /// Adobe Pass authentication. MSO is the name of the TV provider, e.g. "comcast", "cox", "verizon".
    /// </summary>
    /// <param name="mso"></param>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <remarks>
    /// <b>Security warning:</b> Credentials are passed as command-line arguments and are
    /// visible in system process listings (e.g. Task Manager, <c>ps aux</c>).
    /// </remarks>
    /// <exception cref="ArgumentException"></exception>
    public Ytdlp WithAdobePassAuthentication(string mso, string username, string password)
    {
        if (string.IsNullOrWhiteSpace(mso) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("MSO, username, and password are required for Adobe Pass.");

        // Store these in your internal state to be passed during the execution phase
        return this.AddOption("--ap-mso", mso)
                   .AddOption("--ap-username", username)
                   .AddOption("--ap-password", password);

        // TODO: Implement the logic to handle Adobe Pass authentication during the execution phase,
        // as it may require additional steps such as fetching tokens or handling redirects.
        // Pass --ap-password as '-' tells yt-dlp to read from stdin
    }

    #endregion

    #region Post-Processing Options

    /// <summary>
    /// Convert video files to audio-only files (requires ffmpeg and ffprobe).        
    /// </summary>
    /// <param name="format">Formats currently supported: best (default),aac, alac, flac, m4a, mp3, opus, vorbis, wav).</param>
    /// <param name="quality">Audio quality (0–10, lower = better). Default: 5 (medium)</param>
    public Ytdlp WithExtractAudio(AudioFormat format = AudioFormat.Best, int quality = 5)
    {
        return this
            .AddFlag("--extract-audio")
            .AddOption("--audio-format", format.ToString().ToLowerInvariant())
            .AddOption("--audio-quality", quality.ToString());
    }

    /// <summary>
    /// Remux the video into another container if necessary (requires ffmpeg and ffprobe)
    /// If the target container does not support the video/audio codec, remuxing will fail. You can specify multiple rules; 
    /// e.g. "aac>m4a/mov>mp4/mkv" will remux aac to m4a, mov to mp4 and anything else to mkv
    /// </summary>
    /// <param name="format">(currently supported: avi, flv, gif, mkv, mov, mp4, webm, aac, aiff, alac, flac, m4a, mka, mp3, ogg, opus, vorbis, wav).</param>
    public Ytdlp WithRemuxVideo(string format)
    {
        if (string.IsNullOrWhiteSpace(format))
            throw new ArgumentException("Remux format cannot be empty", nameof(format));
        return this.AddOption("--remux-video", format.ToLowerInvariant());
    }

    /// <summary>
    /// Re-encode the video into another format if necessary. The syntax and supported formats are the same as <see cref="WithRemuxVideo"/>
    /// </summary>
    /// <param name="format">(currently supported: avi, flv, gif, mkv, mov, mp4, webm, aac, aiff, alac, flac, m4a, mka, mp3, ogg, opus, vorbis, wav).</param>
    /// <param name="videoCodec"></param>
    /// <param name="audioCodec"></param>
    public Ytdlp WithRecodeVideo(string format, string? videoCodec = null, string? audioCodec = null)
    {
        if (string.IsNullOrWhiteSpace(format))
            throw new ArgumentException("Recode format cannot be empty", nameof(format));
        var builder = AddOption("--recode-video", format.ToLowerInvariant());
        if (!string.IsNullOrWhiteSpace(videoCodec))
            builder = builder.AddOption("--video-codec", videoCodec);
        if (!string.IsNullOrWhiteSpace(audioCodec))
            builder = builder.AddOption("--audio-codec", audioCodec);
        return builder;
    }

    /// <summary>
    /// Give these arguments to the postprocessors. Specify the postprocessor/executable name and to give the argument to the specified
    /// </summary>
    /// <param name="postprocessor">Supported PP are: Merger, ModifyChapters, SplitChapters, ExtractAudio, 
    /// VideoRemuxer, VideoConvertor, Metadata, EmbedSubtitle, EmbedThumbnail, SubtitlesConvertor, ThumbnailsConvertor, 
    /// FixupStretched, FixupM4a, FixupM3u8, FixupTimestamp and FixupDuration.</param>
    /// <param name="args"></param>
    public Ytdlp WithPostprocessorArgs(PostProcessors postprocessor, string args)
    {
        if (string.IsNullOrWhiteSpace(args))
            throw new ArgumentException("Both postprocessor name and arguments are required");

        string combined = $"{postprocessor.ToString().Trim()}:{args.Trim()}";
        return AddOption("--postprocessor-args", combined);
    }

    /// <summary>
    /// Keep the intermediate video file on disk after post-processing
    /// </summary>
    public Ytdlp WithKeepVideo() => AddFlag("-k");

    /// <summary>
    /// Do not overwrite post-processed files
    /// </summary>
    public Ytdlp WithNoPostOverwrites() => AddFlag("--no-post-overwrites");

    /// <summary>
    /// Embed subtitles in the video (only for mp4, webm and mkv videos)
    /// </summary>
    public Ytdlp WithEmbedSubtitles() => AddFlag("--embed-subs");

    /// <summary>
    /// Embed thumbnail in the video as cover art
    /// </summary>
    public Ytdlp WithEmbedThumbnail() => AddFlag("--embed-thumbnail");

    /// <summary>
    /// Embed metadata to the video file
    /// </summary>
    public Ytdlp WithEmbedMetadata() => AddFlag("--embed-metadata");

    /// <summary>
    /// Add chapter markers to the video file
    /// </summary>
    public Ytdlp WithEmbedChapters() => AddFlag("--embed-chapters");

    /// <summary>
    /// Embed the infojson as an attachment to mkv/mka video files
    /// </summary>
    public Ytdlp WithEmbedInfoJson() => AddFlag("--embed-info-json");

    /// <summary>
    /// Do not embed the infojson as an attachment to the video file
    /// </summary>
    public Ytdlp WithNoEmbedInfoJson() => AddFlag("--no-embed-info-json");

    /// <summary>
    /// Replace text in a metadata field using the given regex. This option can be used multiple times.
    /// </summary>
    /// <param name="field"></param>
    /// <param name="regex"></param>
    /// <param name="replacement"></param>
    /// <exception cref="ArgumentException"></exception>
    public Ytdlp WithReplaceInMetadata(string field, string regex, string replacement)
    {
        if (string.IsNullOrWhiteSpace(field) || string.IsNullOrWhiteSpace(regex) || replacement == null)
            throw new ArgumentException("Metadata field, regex, and replacement cannot be empty.");
        return AddOption("--replace-in-metadata", $"{field} {regex} {replacement}");
    }

    /// <summary>
    /// Concatenate videos in a playlist. All the video files must have the same codecs and number of streams to be concatenable
    /// </summary>
    /// <param name="policy">never, always, multi_video (default; only when the videos form a single show)</param>
    public Ytdlp WithConcatPlaylist(string policy = "always") => AddOption("--concat-playlist", policy);

    /// <summary>
    /// Location of the ffmpeg binary
    /// </summary>
    /// <param name="ffmpegPath">Either the path to the binary or its containing directory</param>
    public Ytdlp WithFFmpegLocation(string? ffmpegPath)
    {
        if (string.IsNullOrWhiteSpace(ffmpegPath)) return this;
        return new Ytdlp(this, ffmpegLocation: ffmpegPath.Replace('\\', '/'));
    }

    /// <summary>
    /// Convert the subtitles to another format
    /// </summary>
    /// <param name="format">(currently supported: ass, lrc, srt, vtt)</param>
    /// <exception cref="ArgumentException"></exception>
    public Ytdlp WithConvertSubtitles(string format = "none")
    {
        if (string.IsNullOrWhiteSpace(format))
            throw new ArgumentException("Subtitle format cannot be empty", nameof(format));

        return AddOption("--convert-subs", format.Trim().ToLowerInvariant());
    }

    /// <summary>
    /// Convert the thumbnails to another format. You can specify multiple rules using similar WithRemuxVideo().
    /// </summary>
    /// <param name="format">(currently supported: jpg, png, webp)</param>
    /// <exception cref="ArgumentException"></exception>
    public Ytdlp WithConvertThumbnails(string format = "jpg")
    {
        // Supported: jpg, png, webp
        if (string.IsNullOrWhiteSpace(format))
            throw new ArgumentException("Thumbnail format cannot be empty", nameof(format));

        return AddOption("--convert-thumbnails", format.Trim().ToLowerInvariant());
    }

    /// <summary>
    /// Split video into multiple files based on internal chapters. The "chapter:" prefix can be used with the output filename for the split files.
    /// </summary>
    public Ytdlp WithSplitChapters() => AddFlag("--split-chapters");

    /// <summary>
    /// Remove chapters whose title matches the given regular expression. The syntax is the same as <see cref="WithDownloadSections(string)"/>. 
    /// This option can be used multiple times to remove multiple sections"/>
    /// </summary>
    /// <param name="regex"></param>
    /// <exception cref="ArgumentException"></exception>
    public Ytdlp WithRemoveChapters(string regex)
    {
        if (string.IsNullOrWhiteSpace(regex))
            throw new ArgumentException("Regex cannot be empty", nameof(regex));
        return AddOption("--remove-chapters", regex);
    }

    /// <summary>
    /// Force keyframes at cuts when downloading/splitting/removing sections. 
    /// This is slow due to needing a re-encode, but the resulting video may have fewer artifacts around the cuts
    /// </summary>
    public Ytdlp WithForceKeyframesAtCuts() => AddFlag("--force-keyframes-at-cuts");

    /// <summary>
    /// The (case-sensitive) name of plugin postprocessors to be enabled
    /// This option can be used multiple times to add different postprocessors
    /// </summary>
    /// <param name="postProcessor"></param>
    /// <param name="postProcessorArgs"></param>
    public Ytdlp WithUsePostProcessor(PostProcessors postProcessor, string? postProcessorArgs = null)
    {
        if (!string.IsNullOrWhiteSpace(postProcessorArgs))
            return AddOption("--use-postprocessor", $"{postProcessor.ToString().Trim()}:{postProcessorArgs.Trim()}");
        return AddOption("--use-postprocessor", postProcessor.ToString().Trim());
    }

    #endregion

    #region SponsorBlock Options

    /// <summary>
    /// SponsorBlock categories to create chapters for, separated by commas. 
    /// Available categories are sponsor, intro, outro, selfpromo, preview, filler, interaction, music_offtopic, hook, poi_highlight, chapter, all and default (=all).
    /// You can prefix the category with a "-" to exclude it. E.g. SponsorBlockMark("all,-preview)
    /// </summary>
    /// <param name="categories"></param>
    public Ytdlp WithSponsorblockMark(string categories = "all") => AddOption("--sponsorblock-mark", categories);

    /// <summary>
    /// SponsorBlock categories to be removed from the video file, separated by commas. 
    /// If a category is present in both mark and remove, remove takes precedence. Working and available categories are the same as for WithSponsorblockMark()
    /// </summary>
    /// <param name="categories"></param>
    public Ytdlp WithSponsorblockRemove(string categories = "all") => new Ytdlp(this, sponsorblockRemove: categories);

    /// <summary>
    /// Disable both WithSponsorblockMark() and WithSponsorblockRemove() options and do not use any sponsorblock features
    /// </summary>
    public Ytdlp WithNoSponsorblock() => AddFlag("--no-sponsorblock");

    #endregion

    #region Core
    /// <summary>
    /// Returns a new instance of the Ytdlp class with the specified command-line flag added.
    /// </summary>
    /// <remarks>Use this method to add a single custom flag to the Ytdlp command invocation. This does not
    /// modify the current instance, but returns a new one with the additional flag applied.</remarks>
    /// <param name="flag">The command-line flag to add. Leading and trailing whitespace is ignored.</param>
    /// <returns>A new Ytdlp instance that includes the specified flag in its configuration.</returns>
    public Ytdlp AddFlag(string flag) => new Ytdlp(this, extraFlags: new[] { flag.Trim() });

    /// <summary>
    /// Returns a new instance of the Ytdlp class with an additional command-line option specified by the given key and
    /// value.
    /// </summary>
    /// <remarks>This method does not modify the current instance. Use this method to fluently add options
    /// when constructing command-line arguments.</remarks>
    /// <param name="key">The name of the command-line option to add. Leading and trailing whitespace is ignored. Cannot be null or empty.</param>
    /// <param name="value">The value to assign to the specified command-line option. Cannot be null.</param>
    /// <returns>A new Ytdlp instance that includes the specified option in addition to any existing options.</returns>
    public Ytdlp AddOption(string key, string value) => new Ytdlp(this, extraOptions: new[] { (key.Trim(), value) });
    #endregion

    #region Downloaders

    /// <summary>
    /// Use an external downloader for downloading videos. Supported downloaders are: aria2c, axel, curl, ffmpeg, httpie, httpx, pro aria2c, pro axel, pro curl, pro ffmpeg and pro httpie.
    /// </summary>
    /// <param name="downloaderName"></param>
    /// <param name="downloaderArgs"></param>
    /// <exception cref="ArgumentException"></exception>
    public Ytdlp WithExternalDownloader(string downloaderName, string? downloaderArgs = null)
    {
        if (string.IsNullOrWhiteSpace(downloaderName))
            throw new ArgumentException("Downloader name cannot be empty", nameof(downloaderName));

        var opts = new List<(string, string?)> { ("--downloader", downloaderName.Trim()) };

        if (!string.IsNullOrWhiteSpace(downloaderArgs))
        {
            opts.Add(("--downloader-args", downloaderArgs.Trim()));
        }

        return new Ytdlp(this, extraOptions: opts!);
    }

    /// <summary>
    /// Use aria2c as the external downloader with the specified number of connections per download. This is a convenient wrapper around <see cref="WithExternalDownloader(string, string)"/>
    /// </summary>
    /// <param name="connections"></param>
    public Ytdlp WithAria2(int connections = 16)
    {
        return new Ytdlp(this, extraOptions: new[]
            {
            ("--downloader", "aria2c"),
            ("--downloader-args", $"aria2c:-x{connections} -k1M")
            });
    }

    /// <summary>
    /// Use the native HLS downloader (requires ffmpeg). This is usually faster than the default downloader for HLS streams and can be used as a workaround for certain extraction issues, but may cause compatibility issues with some sites
    /// </summary>
    /// <returns></returns>
    public Ytdlp WithHlsNative() => AddOption("--downloader", "hlsnative");

    /// <summary>
    /// Use ffmpeg as the external downloader with the specified extra arguments. This is a convenient wrapper around <see cref="WithExternalDownloader(string, string)"/>
    /// </summary>
    /// <param name="extraFfmpegArgs">Additional arguments to pass to ffmpeg. Can be null.</param>
    /// <returns>A new instance of the Ytdlp class with ffmpeg as the external downloader and the specified extra arguments applied.</returns>
    public Ytdlp WithFfmpegAsLiveDownloader(string? extraFfmpegArgs = null) => WithExternalDownloader("ffmpeg", extraFfmpegArgs);

    #endregion

    #region Redundant options

    /// <summary>
    /// Playlist start index
    /// </summary>
    /// <param name="index"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public Ytdlp WithPlaylistStart(int index)
    {
        if (index < 1) throw new ArgumentOutOfRangeException(nameof(index), "Must be >= 1");
        return AddOption("--playlist-start", index.ToString());
    }

    /// <summary>
    /// Playlist end index
    /// </summary>
    /// <param name="index"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public Ytdlp WithPlaylistEnd(int index)
    {
        if (index < 1) throw new ArgumentOutOfRangeException(nameof(index), "Must be >= 1");
        return AddOption("--playlist-end", index.ToString());
    }

    /// <summary>
    /// Specifies a custom User-Agent string to be used for HTTP requests.
    /// </summary>
    /// <param name="userAgent">The User-Agent string to send with HTTP requests. Cannot be null, empty, or consist only of white-space
    /// characters.</param>
    /// <returns>A new instance of the Ytdlp class with the specified User-Agent option applied.</returns>
    /// <exception cref="ArgumentException">Thrown if userAgent is null, empty, or consists only of white-space characters.</exception>
    public Ytdlp WithUserAgent(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            throw new ArgumentException("User-Agent cannot be empty", nameof(userAgent));
        return AddOption("--user-agent", userAgent.Trim());
    }

    /// <summary>
    /// Sets the HTTP referer header to use for subsequent requests.
    /// </summary>
    /// <remarks>Use this method when the target resource requires a specific referer header for access or
    /// authentication.</remarks>
    /// <param name="referer">The referer URL to include in the HTTP header. Cannot be null, empty, or whitespace.</param>
    /// <returns>A new instance of the <see cref="Ytdlp"/> class with the referer option applied.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="referer"/> is null, empty, or consists only of whitespace.</exception>
    public Ytdlp WithReferer(string referer)
    {
        if (string.IsNullOrWhiteSpace(referer))
            throw new ArgumentException("Referer cannot be empty", nameof(referer));
        return AddOption("--referer", referer.Trim());
    }

    /// <summary>
    /// Adds a filter to include only videos whose titles match the specified regular expression.
    /// </summary>
    /// <remarks>Use this method to restrict downloads to videos with titles that match the given pattern.
    /// This option corresponds to the '--match-title' command-line argument in yt-dlp.</remarks>
    /// <param name="regex">A regular expression pattern used to match video titles. Cannot be null, empty, or consist only of white-space
    /// characters.</param>
    /// <returns>The current <see cref="Ytdlp"/> instance with the match title filter applied.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="regex"/> is null, empty, or consists only of white-space characters.</exception>
    public Ytdlp WithMatchTitle(string regex)
    {
        if (string.IsNullOrWhiteSpace(regex))
            throw new ArgumentException("Regex cannot be empty", nameof(regex));
        return AddOption("--match-title", regex.Trim());
    }

    /// <summary>
    /// Adds an option to reject downloads with titles matching the specified regular expression.
    /// </summary>
    /// <param name="regex">A regular expression pattern used to filter out downloads by title. Titles matching this pattern will be
    /// excluded.</param>
    /// <returns>The current <see cref="Ytdlp"/> instance with the reject title option applied.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="regex"/> is null, empty, or consists only of white-space characters.</exception>
    public Ytdlp WithRejectTitle(string regex)
    {
        if (string.IsNullOrWhiteSpace(regex))
            throw new ArgumentException("Regex cannot be empty", nameof(regex));
        return AddOption("--reject-title", regex.Trim());
    }

    /// <summary>
    /// Enables the break-on-reject option, causing the process to stop when a download is rejected.
    /// </summary>
    /// <remarks>Use this method to configure the process to halt immediately if any download is rejected,
    /// rather than continuing with subsequent downloads.</remarks>
    /// <returns>A new instance of the current object with the break-on-reject flag applied.</returns>
    public Ytdlp WithBreakOnReject() => AddFlag("--break-on-reject");
    #endregion

    #region Bonus

    public Ytdlp With1440pOrBest() => new Ytdlp(this, format: "bv*[height<=?1440]+bestaudio/best");
    public Ytdlp With1080pOrBest() => new Ytdlp(this, format: "bv*[height<=?1080]+bestaudio/best");
    public Ytdlp With720pOrBest() => new Ytdlp(this, format: "bv*[height<=?720]+bestaudio/best");

    public Ytdlp WithMp4PostProcessingPreset()
        => this
            .WithRemuxVideo("mp4")
            .WithEmbedMetadata()
            .WithEmbedChapters()
            .WithEmbedThumbnail();

    public Ytdlp WithMkvOutput()
        => this
            .WithRemuxVideo("mkv")
            .WithMergeOutputFormat("mkv");

    public Ytdlp WithMaxHeight(int height)
    {
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), "Height must be positive");

        string formatSelector = $"bv*[height<={height}]+bestaudio/best";
        return new Ytdlp(this, format: formatSelector);
    }

    public Ytdlp WithMaxHeightOrBest(int height)
    {
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), "Height must be positive");

        string formatSelector = $"bv*[height<={height}]+bestaudio/best[height<={height}]/best";
        return new Ytdlp(this, format: formatSelector);
    }

    public Ytdlp WithBestVideoPlusBestAudio() => new Ytdlp(this, format: "bv*+bestaudio/best");
    public Ytdlp WithBestAudioOnly() => new Ytdlp(this, format: "bestaudio");

    public Ytdlp WithNo4k() => new Ytdlp(this, format: "bv*[height<=?2160]+bestaudio/best");

    public Ytdlp WithBestM4aAudio() => new Ytdlp(this, format: "bestaudio[ext=m4a]/bestaudio/best");
    #endregion

    // ==================================================================================================================
    // Probe and Download Functions
    // ==================================================================================================================

    #region Execution & Utility Methods

    // ── Probe-based public methods ────────────────────────────────────────────

    /// <summary>
    /// Command preview ofr debug operatons
    /// </summary>
    /// <param name="url">The URL of the video to preview.</param>
    /// <returns>A string representing the command that would be executed for the given URL.</returns>
    public string Preview(string url)
    {
        var argsList = BuildArguments(url);
        return string.Join(" ", argsList.Select(EscapeArgument));
    }

    /// <summary>
    /// Retrieves the current version string of the underlying yt-dlp executable.
    /// </summary>
    /// <param name="ct">A <see cref="CancellationToken"/> to abort the version check process.</param>
    /// <returns>
    /// A <see cref="string"/> representing the yt-dlp version (e.g., "2023.03.04"); 
    /// returns an empty string or throws if the binary cannot be found.
    /// </returns>
    public async Task<string> VersionAsync(CancellationToken ct = default)
    {
        var output = await RunProbeAsync("--version", ct);
        return output?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Updates the underlying yt-dlp binary to the latest version on the specified release channel.
    /// </summary>
    /// <param name="channel">The release channel to pull updates from (Master, Nightly, Stable.).</param>
    /// <param name="specificVersion">The specific version to update to. e.g., "2026.03.17" or "latest"</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to abort the download and installation process.</param>
    /// <returns>
    /// A <see cref="string"/> containing the update log or the new version number; 
    /// returns an empty string or throws if the update process fails.
    /// </returns>
    public async Task<string> UpdateAsync(UpdateChannel channel = UpdateChannel.Stable, string? specificVersion = null, CancellationToken ct = default)
    {
        string target = channel.ToString().ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(specificVersion))
            target += $"@{specificVersion.ToLowerInvariant()}";

        var output = await RunProbeAsync($"--update-to {target}", ct);
        if (string.IsNullOrWhiteSpace(output)) return string.Empty;

        // Analyze output for professional messages
        if (output.Contains("Updated", StringComparison.OrdinalIgnoreCase))
            return $"yt-dlp was successfully updated to the latest {target}.";

        if (output.Contains("up to date", StringComparison.OrdinalIgnoreCase))
            return $"yt-dlp is already up to date on {target}.";

        return output;
    }

    /// <summary>
    /// List all supported extractors and exit
    /// </summary>
    /// <param name="ct"></param>    
    /// <param name="tuneProcess">Whether to tune the process for better performance (true by default). If false, the process will use the default buffer size and may have slower output processing.</param>
    /// <returns>List of extractor names</returns>
    public async Task<List<string>> GetExtractorsAsync(CancellationToken ct = default, bool tuneProcess = true)
    {
        try
        {
            var result = await RunProbeAsync("--list-extractors", ct, tuneProcess);
            if (string.IsNullOrWhiteSpace(result)) return new List<string>();

            return result
               .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
               .ToList();
        }
        catch (OperationCanceledException)
        {
            _logger.Log(LogType.Warning, "Extractors fetch cancelled.");
            return new List<string>();
        }
    }

    /// <summary>
    /// List all supported MSOs for Adobe Pass authentication and exit
    /// </summary>
    /// <param name="ct"></param>
    /// <param name="tuneProcess">Whether to tune the process for better performance (true by default). If false, the process will use the default buffer size and may have slower output processing.</param>
    /// <returns>List of Adobe Pass MSOs</returns>
    public async Task<List<string>> GetAdobePassListAsync(CancellationToken ct = default, bool tuneProcess = true)
    {
        try
        {
            var result = await RunProbeAsync("--ap-list-mso", ct, tuneProcess);
            if (string.IsNullOrWhiteSpace(result)) return new List<string>();

            return result
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }
        catch (OperationCanceledException)
        {
            _logger.Log(LogType.Warning, "Adobe Pass list fetch cancelled.");
            return new List<string>();
        }
    }

    /// <summary>
    ///  Fetches video metadata from the specified URL.
    /// </summary>
    /// <param name = "url">The source URL(video or playlist) to probe.</param>
    /// <param name="ct">The <see cref="CancellationToken"/> to abort the process.</param>
    /// <param name="tuneProcess">Whether to tune the process for better performance (true by default). If false, the process will use the default buffer size and may have slower output processing.</param>
    /// <returns>
    /// A <see cref="Metadata"/> object containing the parsed metadata output; 
    /// returns <see langword="null"/> if the process fails, returns empty, or is cancelled.
    /// </returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<Metadata?> GetMetadataAsync(string url, CancellationToken ct = default, bool tuneProcess = true)
    {
        var json = await GetMetadataInternalAsync(url, flat: true, ct, tuneProcess);
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            return JsonSerializer.Deserialize<Metadata>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Error, $"Metadata deserialize failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Fetches raw JSON metadata the specified URL.
    /// </summary>
    /// <param name="url">The source URL (video or playlist) to probe.</param>
    /// <param name="ct">The <see cref="CancellationToken"/> to abort the process.</param>
    /// <param name="tuneProcess">Whether to tune the process for better performance (true by default). If false, the process will use the default buffer size and may have slower output processing.</param>
    /// <returns>
    /// A raw JSON <see cref="string"/> containing the parsed metadata output; 
    /// returns <see langword="null"/> if the process fails, returns empty, or is cancelled.
    /// </returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<string?> GetMetadataRawAsync(string url, CancellationToken ct = default, bool tuneProcess = true)
        => await GetMetadataInternalAsync(url, flat: true, ct, tuneProcess);

    /// <summary>
    /// Gets deep metadata for the specified URL by requesting non-flat JSON and deserializing it into a Metadata object.
    /// </summary>
    /// <remarks>Requests non-flat JSON via GetMetadataInternalAsync. Logs a warning when the JSON output is
    /// empty. Deserialization uses case-insensitive property names and allows numeric values provided as strings; any
    /// deserialization error results in a null return.</remarks>
    /// <param name="url">The resource URL to retrieve metadata from.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <param name="tuneProcess">True to enable process tuning for metadata retrieval; otherwise false.</param>
    /// <returns>A Metadata instance if JSON is present and deserialization succeeds; otherwise null.</returns>
    public async Task<Metadata?> GetDeepMetadataAsync(string url, CancellationToken ct = default, bool tuneProcess = true)
    {
        var json = await GetMetadataInternalAsync(url, flat: false, ct, tuneProcess);
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            return JsonSerializer.Deserialize<Metadata>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Error, $"Deep metadata deserialize failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Asynchronously retrieves deep (non-flattened) metadata for the specified URL as raw JSON.
    /// </summary>
    /// <remarks>Invokes GetMetadataInternalAsync with flat set to false. Logs a warning when the returned
    /// JSON is null or consists only of whitespace.</remarks>
    /// <param name="url">The URL from which to retrieve metadata.</param>
    /// <param name="ct">A cancellation token to cancel the asynchronous operation.</param>
    /// <param name="tuneProcess">Whether to apply process tuning for the metadata retrieval.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is a JSON string containing deep
    /// (non-flattened) metadata, or null if the output is empty.</returns>
    public async Task<string?> GetDeepMetadataRawAsync(string url, CancellationToken ct = default, bool tuneProcess = true)
        => await GetMetadataInternalAsync(url, flat: false, ct, tuneProcess);

    private async Task<string?> GetMetadataInternalAsync(string url, bool flat, CancellationToken ct, bool tuneProcess)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be empty.", nameof(url));

        var arguments =
            "--dump-single-json " +
            "--simulate " +
            "--skip-download " +
            (flat ? "--flat-playlist " : "") +
            "--lazy-playlist " +
            "--quiet " +
            "--no-warnings " +
            $"{Quote(url)}";

        return await RunProbeAsync(arguments, ct, tuneProcess);
    }

    /// <summary>
    /// Retrieves a list of all available stream formats for a given URL.
    /// </summary>
    /// <param name="url">The video or playlist URL to probe.</param>
    /// <param name="ct">The <see cref="CancellationToken"/> to abort the process.</param>
    /// <param name="tuneProcess">Whether to tune the process for better performance (true by default). If false, the process will use the default buffer size and may have slower output processing.</param>
    /// <returns>
    /// A <see cref="List{Format}"/> containing all available streams; 
    /// returns an empty list or <see langword="null"/> if the probe fails or is cancelled.
    /// </returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<List<Format>> GetFormatsAsync(string url, CancellationToken ct = default, bool tuneProcess = true)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Video URL cannot be empty.", nameof(url));

        var output = await RunProbeAsync($"-F {Quote(url)}", ct, tuneProcess);
        return string.IsNullOrWhiteSpace(output)
            ? new List<Format>()
            : ParseFormats(output);
    }

    /// <summary>
    /// Fetches a lightweight version of video metadata.
    /// </summary>
    /// <param name="url">The video or playlist URL to probe.</param>
    /// <param name="ct">The <see cref="CancellationToken"/> to abort the process.</param>
    /// <param name="tuneProcess">Whether to tune the process for better performance (true by default). If false, the process will use the default buffer size and may have slower output processing.</param>
    /// <returns>
    /// A <see cref="MetadataLight"/> object if successful; 
    /// returns <see langword="null"/> if the process fails or is cancelled.
    /// </returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<MetadataLight?> GetMetadataLiteAsync(string url, CancellationToken ct = default, bool tuneProcess = true)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be empty.", nameof(url));

        try
        {
            var separator = $"|||{Guid.NewGuid():N}|||";

            var fields = new[]
            {
                "%(id)s",
                "%(title)s",
                "%(duration)s",
                "%(thumbnail)s",
                "%(view_count)s",
                "%(filesize,filesize_approx)s",
                "%(description).500s"
            };

            var arguments =
                $"--print \"{string.Join(separator, fields)}\" " +
                $"--skip-download --no-playlist --quiet {Quote(url)}";

            var output = await RunProbeAsync(arguments, ct, tuneProcess);
            if (string.IsNullOrWhiteSpace(output)) return null;

            var parts = output.Trim().Split(separator);
            if (parts.Length < 6) return null;

            return new MetadataLight
            {
                Id = parts[0].Trim(),
                Title = parts[1].Trim(),
                Duration = double.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var dur) ? dur : null,
                Thumbnail = parts[3].Trim(),
                ViewCount = long.TryParse(parts[4], NumberStyles.Any, CultureInfo.InvariantCulture, out var views) ? views : null,
                FileSize = long.TryParse(parts[5], NumberStyles.Any, CultureInfo.InvariantCulture, out var size) ? size : null,
                Description = parts.Length > 6 ? parts[6].Trim() : null
            };
        }
        catch (OperationCanceledException)
        {
            _logger.Log(LogType.Warning, "Metadata lite fetch cancelled.");
            return null;
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Error, $"Metadata lite failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Fetches a specific subset of metadata fields from the specified URL.
    /// </summary>
    /// <param name="url">The source URL to probe.</param>
    /// <param name="fields">A collection of field names to extract (e.g., "title", "uploader").</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to abort the yt-dlp process.</param>
    /// <param name="tuneProcess">Whether to tune the process for better performance (true by default). If false, the process will use the default buffer size and may have slower output processing.</param>
    /// <returns>
    /// A <see cref="Dictionary{TKey, TValue}"/> containing the requested fields and their values; 
    /// returns <see langword="null"/> if the process fails, returns no data, or is cancelled.
    /// </returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<Dictionary<string, string>?> GetMetadataLiteAsync(string url, IEnumerable<string> fields, CancellationToken ct = default, bool tuneProcess = true)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be empty.", nameof(url));

        var fieldList = fields?.ToList()
            ?? throw new ArgumentNullException(nameof(fields));

        if (fieldList.Count == 0)
            throw new ArgumentException("At least one field must be requested.", nameof(fields));

        try
        {
            var separator = $"|||{Guid.NewGuid():N}|||";
            var printFormat = string.Join(separator, fieldList.Select(f => $"%({f})s"));
            var arguments =
                $"--print \"{printFormat}\" " +
                $"--skip-download --no-playlist --quiet {Quote(url)}";

            var rawOutput = await RunProbeAsync(arguments, ct, tuneProcess);
            if (string.IsNullOrWhiteSpace(rawOutput)) return null;

            var parts = rawOutput.Trim().Split(separator);
            if (parts.Length != fieldList.Count) return null;

            var result = new Dictionary<string, string>(
                fieldList.Count,
                StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < fieldList.Count; i++)
                result[fieldList[i]] = parts[i].Trim();

            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.Log(LogType.Warning, "Metadata lite (fields) fetch cancelled.");
            return null;
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Error, $"Metadata lite (fields) failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Probes the specified URL to find the ID of the best available audio format.
    /// </summary>
    /// <param name="url">The video or playlist URL to probe.</param>
    /// <param name="ct">The <see cref="CancellationToken"/> to abort the process.</param>
    /// <param name="tuneProcess">Whether to tune the process for better performance (true by default). If false, the process will use the default buffer size and may have slower output processing.</param>
    /// <returns>
    /// A <see cref="string"/> representing the best audio format ID (e.g., "140"); 
    /// returns an empty string or throws if no suitable audio is found.
    /// </returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<string> GetBestAudioFormatIdAsync(string url, CancellationToken ct = default, bool tuneProcess = true)
    {
        var meta = await GetMetadataAsync(url, ct, tuneProcess);
        var best = meta?.Formats?
            .Where(f => f.IsAudio && (f.Abr > 0 || f.Tbr > 0))
            .OrderByDescending(f => f.Abr ?? f.Tbr ?? 0)
            .FirstOrDefault();

        return best?.FormatId ?? "bestaudio";
    }

    /// <summary>
    /// Probes the specified URL to find the ID of the best available video format within the specified height.
    /// </summary>
    /// <param name="url">The source URL to probe for video formats.</param>
    /// <param name="maxHeight">The maximum vertical resolution allowed (default 1080p).</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to cancel the underlying yt-dlp process.</param>
    /// <param name="tuneProcess">Whether to tune the process for better performance (true by default). If false, the process will use the default buffer size and may have slower output processing.</param>
    /// <returns>
    /// A <see cref="string"/> representing the best video format ID (e.g., "137" or "248"); 
    /// returns an empty string or <see langword="null"/> if no suitable format is found.
    /// </returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<string> GetBestVideoFormatIdAsync(string url, int maxHeight = 1080, CancellationToken ct = default, bool tuneProcess = true)
    {
        var meta = await GetMetadataAsync(url, ct, tuneProcess);
        var best = meta?.Formats?
            .Where(f => !f.IsAudio && f.Height.HasValue && f.Height <= maxHeight)
            .OrderByDescending(f => f.Height)
            .ThenByDescending(f => f.Fps ?? 0)
            .FirstOrDefault();

        return best?.FormatId ?? "bestvideo";
    }

    /// <summary>
    /// Probes the specified URL to retrieve a list of available subtitle tracks.
    /// </summary>
    /// <param name="url">The video URL to probe for subtitle tracks.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to cancel the underlying yt-dlp process.</param>
    /// <param name="tuneProcess">Whether to tune the process for better performance (true by default). If false, the process will use the default buffer size and may have slower output processing.</param>
    /// <returns>A list of available subtitle tracks.</returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<List<SubtitleTrack>> GetSubtitlesAsync(string url, CancellationToken ct = default, bool tuneProcess = true)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Video URL cannot be empty.", nameof(url));

        var output = await RunProbeAsync($"--list-subs {Quote(url)}", ct, tuneProcess);

        return string.IsNullOrWhiteSpace(output)
            ? new List<SubtitleTrack>()
            : ParseSubtitles(output);
    }

    // ── Download ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Executes download processing for a URL.
    /// </summary>
    /// <param name="url">The source URL to download.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to stop the execution.</param>
    /// <param name="tuneProcess">Whether to tune the process for better performance (true by default). If false, the process will use the default buffer size and may have slower output processing.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="YtdlpException"></exception>
    public async Task DownloadAsync(string url, CancellationToken ct = default, bool tuneProcess = true)
    {
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL required", nameof(url));

        try
        {
            if (!string.IsNullOrWhiteSpace(_outputFolder)) Directory.CreateDirectory(_outputFolder);
            if (!string.IsNullOrWhiteSpace(_homeFolder)) Directory.CreateDirectory(_homeFolder);
            if (!string.IsNullOrWhiteSpace(_tempFolder)) Directory.CreateDirectory(_tempFolder);
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Error, $"Failed to create required folders: {ex.Message}");
            throw new YtdlpException("Failed to create required folders", ex);
        }

        var argsList = BuildArguments(url);
        var arguments = string.Join(" ", argsList.Select(EscapeArgument));

        _logger.Log(LogType.Info, $"Executing: {_ytdlpPath} {arguments}");

        // Isolated per-call — safe for concurrent downloads on the same Ytdlp instance
        var progressParser = new ProgressParser(_logger);
        var runner = CreateRunner();

        // ── Wire progress parser events → Ytdlp public events ─────────────────
        void OnProgressDownloadHandler(object? s, DownloadProgressEventArgs e) => OnProgressDownload?.Invoke(this, e);
        void OnProgressMessageHandler(object? s, string msg) => OnProgressMessage?.Invoke(this, msg);
        void OnCompleteDownloadHandler(object? s, string msg) => OnCompleteDownload?.Invoke(this, msg);
        void OnPostProcessingStartHandler(object? s, string msg) => OnPostProcessingStart?.Invoke(this, msg);
        void OnPostProcessingCompleteHandler(object? s, string msg) => OnPostProcessingComplete?.Invoke(this, msg);

        progressParser.OnProgressDownload += OnProgressDownloadHandler;
        progressParser.OnProgressMessage += OnProgressMessageHandler;
        progressParser.OnCompleteDownload += OnCompleteDownloadHandler;
        progressParser.OnPostProcessingStart += OnPostProcessingStartHandler;
        progressParser.OnPostProcessingComplete += OnPostProcessingCompleteHandler;

        // ── Wire runner events → Ytdlp public events ──────────────────────────
        void OnOutputMessageHandler(object? s, string msg) => OnOutputMessage?.Invoke(this, msg);
        void OnErrorMessageHandler(object? s, string msg) => OnErrorMessage?.Invoke(this, msg);
        void OnCommandCompletedHandler(object? s, CommandCompletedEventArgs e) => OnCommandCompleted?.Invoke(this, e);

        runner.OnErrorReceived += OnErrorMessageHandler;
        runner.OnCommandCompleted += OnCommandCompletedHandler;

        try
        {
            await runner.ExecuteAsync(
                arguments,
                onLineReceived: line =>
                {
                    // Feed each stdout line through the progress parser
                    try { progressParser.ParseProgress(line); }
                    catch (Exception ex) { _logger.Log(LogType.Error, $"Progress parse error: {ex.Message}"); }

                    OnOutputMessageHandler(null, line);
                },
                ct: ct,
                tuneProcess: tuneProcess,
                captureFullOutput: false);
        }
        finally
        {
            // Always unsubscribe — prevents memory leaks on cancel or exception
            progressParser.OnProgressDownload -= OnProgressDownloadHandler;
            progressParser.OnProgressMessage -= OnProgressMessageHandler;
            progressParser.OnCompleteDownload -= OnCompleteDownloadHandler;
            progressParser.OnPostProcessingStart -= OnPostProcessingStartHandler;
            progressParser.OnPostProcessingComplete -= OnPostProcessingCompleteHandler;

            runner.OnErrorReceived -= OnErrorMessageHandler;
            runner.OnCommandCompleted -= OnCommandCompletedHandler;
        }
    }

    /// <summary>
    /// Executes batch download processing for a collection of URLs with a specified concurrency limit.
    /// </summary>
    /// <param name="urls">An enumerable collection of source URLs to process.</param>
    /// <param name="maxConcurrency">The maximum number of simultaneous yt-dlp processes (default is 3).</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to stop the batch execution.</param>
    /// <param name="tuneProcess">Whether to tune the processes for better performance (true by default). If false, the processes will use the default buffer size and may have slower output processing.</param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous execution of the process.
    /// </returns>
    /// <exception cref="YtdlpException"></exception>
    public async Task DownloadBatchAsync(IEnumerable<string> urls, int maxConcurrency = 3, CancellationToken ct = default, bool tuneProcess = true)
    {
        var urlList = urls?.ToList();
        if (urlList == null || urlList.Count == 0)
        {
            _logger.Log(LogType.Error, "No URLs provided for batch download.");
            throw new YtdlpException("No URLs provided for batch download.");
        }

        using var throttler = new SemaphoreSlim(maxConcurrency);

        var tasks = urlList.Select(async url =>
        {
            await throttler.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                await DownloadAsync(url, ct, tuneProcess).ConfigureAwait(false);
            }
            catch (YtdlpException ex)
            {
                _logger.Log(LogType.Error, $"Skipping {url}: {ex.Message}");
            }
            finally
            {
                throttler.Release();
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }



    #endregion

    // ==================================================================================================================
    // Internal Helpers and Utilities
    // ==================================================================================================================

    #region Helpers

    /// <summary>
    /// Creates an isolated <see cref="ProcessRunner"/> for a single execution.
    /// Every call gets its own runner — no shared state between concurrent downloads.
    /// </summary>
    private ProcessRunner CreateRunner() => new ProcessRunner(new ProcessFactory(_ytdlpPath), _logger);

    /// <summary>
    /// Executes any yt-dlp probe command (metadata, version, format list, etc.)
    /// and returns the full captured stdout as a string.
    /// Returns <see langword="null"/> if the process fails, is cancelled, or produces no output.
    /// </summary>
    private async Task<string?> RunProbeAsync(string arguments, CancellationToken ct = default, bool tuneProcess = true)
    {
        try
        {
            var result = await CreateRunner().ExecuteAsync(
                arguments,
                onLineReceived: null,
                ct: ct,
                tuneProcess: tuneProcess,
                captureFullOutput: true);

            return result.IsSuccess && !string.IsNullOrWhiteSpace(result.FullOutput)
                ? result.FullOutput.Trim()
                : null;
        }
        catch (OperationCanceledException)
        {
            _logger.Log(LogType.Warning, "Probe cancelled.");
            return null;
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Error, $"Probe failed: {ex.Message}");
            return null;
        }
    }

    private List<string> BuildArguments(string url)
    {
        var args = new List<string>();

        bool usingAbsoluteOutput = !string.IsNullOrWhiteSpace(_outputFolder);

        if (usingAbsoluteOutput && !string.IsNullOrWhiteSpace(_tempFolder))
        {
            _logger.Log(LogType.Debug, "Temp folder ignored because absolute output template is used.");
        }

        // temp folder
        if (!usingAbsoluteOutput && !string.IsNullOrWhiteSpace(_tempFolder))
        {
            args.Add("--paths");
            args.Add($"temp:{_tempFolder}");
        }

        // home folder only if NOT using absolute output
        if (!usingAbsoluteOutput && !string.IsNullOrWhiteSpace(_homeFolder))
        {
            args.Add("--paths");
            args.Add($"home:{_homeFolder}");
        }

        // Output template
        if (!string.IsNullOrWhiteSpace(_outputTemplate))
        {
            args.Add("-o");

            if (usingAbsoluteOutput)
            {
                var full = Path.Combine(_outputFolder!, _outputTemplate).Replace("\\", "/");
                args.Add(full);
            }
            else
            {
                args.Add(_outputTemplate);
            }
        }

        // Format
        if (!string.IsNullOrWhiteSpace(_format))
        {
            args.Add("-f");
            args.Add(_format);
        }

        // Concurrent fragments
        if (_concurrentFragments > 1)
        {
            args.Add("--concurrent-fragments");
            args.Add(_concurrentFragments.Value.ToString());
        }

        // Flags
        if (_flags.Length > 0)
            args.AddRange(_flags);

        // Options
        if (_options.Length > 0)
        {
            foreach (var kv in _options)
            {
                args.Add(kv.Key);
                if (kv.Value != null)
                    args.Add(kv.Value);
            }
        }

        // Special single-value options
        if (_cookiesFile is not null) { args.Add("--cookies"); args.Add(_cookiesFile); }
        if (_cookiesFromBrowser is not null) { args.Add("--cookies-from-browser"); args.Add(_cookiesFromBrowser); }
        if (_proxy is not null) { args.Add("--proxy"); args.Add(_proxy); }
        if (_ffmpegLocation is not null) { args.Add("--ffmpeg-location"); args.Add(_ffmpegLocation); }
        if (_sponsorblockRemove is not null) { args.Add("--sponsorblock-remove"); args.Add(_sponsorblockRemove); }

        // URL last
        args.Add(url);

        return args;
    }

    private static string ValidatePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("yt-dlp path cannot be empty");

        if (!File.Exists(path) && !IsExecutableInPath(path))
            throw new FileNotFoundException($"yt-dlp executable not found: {path}");

        return path;
    }

    private static bool IsExecutableInPath(string name)
    {
        return Environment.GetEnvironmentVariable("PATH")?
            .Split(Path.PathSeparator)
            .Any(p => File.Exists(Path.Combine(p, name))) ?? false;
    }

    private static string Quote(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "\"\"";
        // Escape " and \
        string escaped = value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        return $"\"{escaped}\"";
    }

    /// <summary>
    /// Escapes a single argument token for ProcessStartInfo.Arguments.
    /// Flags (starting with "-") are never quoted.
    /// Values containing spaces, quotes, or backslashes are wrapped in double-quotes.
    /// </summary>
    private static string EscapeArgument(string arg)
    {
        if (string.IsNullOrEmpty(arg)) return "\"\"";
        if (arg.StartsWith("-")) return arg;  // flags never quoted
        if (!arg.Contains(' ') &&
            !arg.Contains('"') &&
            !arg.Contains('\\') &&
            !arg.Contains('\t')) return arg;  // clean value, no quoting needed

        string escaped = arg.Replace("\\", "\\\\").Replace("\"", "\\\"");
        return $"\"{escaped}\"";
    }

    private List<Format> ParseFormats(string result)
    {
        var formats = new List<Format>();
        if (string.IsNullOrWhiteSpace(result)) return formats;

        var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        bool inFormatSection = false;

        foreach (var line in lines)
        {
            if (line.Contains("[info] Available formats")) { inFormatSection = true; continue; }
            if (!inFormatSection || line.Contains("RESOLUTION") || line.StartsWith("---")) continue;
            if (string.IsNullOrWhiteSpace(line) || !Regex.IsMatch(line, @"^\S+\s+\S+")) break;

            try
            {
                var format = Format.FromParsedLine(line);
                if (!string.IsNullOrEmpty(format.Id) && !formats.Exists(f => f.Id == format.Id))
                    formats.Add(format);
            }
            catch (Exception ex)
            {
                _logger.Log(LogType.Warning, $"Failed parsing format line: {line} → {ex.Message}");
            }
        }

        _logger.Log(LogType.Info, $"Parsed {formats.Count} formats");
        return formats;
    }

    private List<SubtitleTrack> ParseSubtitles(string output)
    {
        var subtitles = new List<SubtitleTrack>();
        var lines = output.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        var rowRegex = new System.Text.RegularExpressions.Regex(@"^(\S+)\s+(.+?)\s{2,}([a-z0-9, ]+)$");

        bool foundStartPoint = false;
        bool foundHeader = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // 1. Look for the specific trigger point
            if (trimmed.Contains("[info] Available subtitles for"))
            {
                foundStartPoint = true;
                continue;
            }

            // 2. We only start parsing rows after the trigger AND the header
            if (foundStartPoint && trimmed.StartsWith("Language"))
            {
                foundHeader = true;
                continue;
            }

            if (foundHeader)
            {
                var match = rowRegex.Match(trimmed);
                if (match.Success)
                {
                    subtitles.Add(new SubtitleTrack
                    {
                        LanguageCode = match.Groups[1].Value,
                        Name = match.Groups[2].Value.Trim(),
                        Formats = match.Groups[3].Value.Split(',').Select(f => f.Trim()).ToList()
                    });
                }
            }
        }
        return subtitles;
    }

    #endregion

}


