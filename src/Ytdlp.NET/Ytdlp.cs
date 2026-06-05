using ManuHub.Ytdlp.NET.Core;
using ManuHub.Ytdlp.NET.Models.Auth;
using System.Collections.Immutable;

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
public sealed partial class Ytdlp
{
    // ==================================================================================================================
    // Immutable configuration fields events and flags and contructors
    // ==================================================================================================================
    #region Frozen configuration
    private readonly string _ytdlpPath;
    private readonly ILogger _logger;

    private readonly YtdlpAuth? _auth;
    private readonly AdobePassAuth? _adobePass;
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
       

    #region Constructors

    public Ytdlp(string ytdlpPath = "yt-dlp", ILogger? logger = null)
    {
        _ytdlpPath = ytdlpPath;
        _logger = logger ?? new DefaultLogger();

        // defaults
        _auth = null; 
        _adobePass = null; 
        _outputFolder = null;
        _tempFolder = null;
        _homeFolder = null;
        _outputTemplate = "%(title)s [%(id)s].%(ext)s";
        _format = "b";
        _concurrentFragments = null;       
        _cookiesFile = null;
        _cookiesFromBrowser = null;
        _proxy = null;
        _ffmpegLocation = null;
        _sponsorblockRemove = null;
        _flags = ImmutableArray<string>.Empty;
        _options = ImmutableArray<(string, string)>.Empty;
    }

    // Private copy constructor – every WithXxx() uses this
    private Ytdlp(Ytdlp other,
        YtdlpAuth? auth = null,
        AdobePassAuth? adobePass = null,
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

        _auth = auth ?? other._auth;
        _adobePass = adobePass ?? other._adobePass;
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
    // Internal Common Helpers and Utilities
    // ==================================================================================================================

    #region Helpers

    /// <summary>
    /// Creates an isolated <see cref="ProcessRunner"/> for a single execution.
    /// Every call gets its own runner — no shared state between concurrent downloads.
    /// </summary>
    private ProcessRunner CreateRunner() => new ProcessRunner(new ProcessFactory(_ytdlpPath), _logger);


    /// <summary>
    /// Builds the command-line arguments for a given URL based on the current configuration.
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    private List<string> BuildArguments(string url)
    {
        var args = new List<string>();

        bool usingAbsoluteOutput = !string.IsNullOrWhiteSpace(_outputFolder);

        if (usingAbsoluteOutput && !string.IsNullOrWhiteSpace(_tempFolder))
        {
            _logger.Log(LogType.Debug, "Temp folder ignored because absolute output template is used.");
        }

        // Authentication
        if (_auth is not null)
        {
            args.Add("--username");
            args.Add(_auth.Username);

            args.Add("--password");
            args.Add("-"); // tells yt-dlp to read from stdin
        }

        // Adobe Pass authentication
        if (_adobePass is not null)
        {
            args.Add("--ap-mso");
            args.Add(_adobePass.Mso);

            args.Add("--ap-username");
            args.Add(_adobePass.Username);

            args.Add("--ap-password");
            args.Add("-"); // tells yt-dlp to read from stdin
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

        // Flags (starting with -) should never be quoted.
        if (arg.StartsWith("-")) return arg;

        // ALWAYS escape, even if it looks "clean". 
        // This ensures consistency and prevents issues with hidden characters.
        string escaped = arg.Replace("\\", "\\\\").Replace("\"", "\\\"");

        return $"\"{escaped}\"";
    }

    #endregion

}


