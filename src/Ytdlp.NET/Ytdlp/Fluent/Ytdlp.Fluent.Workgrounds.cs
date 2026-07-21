using System.Globalization;

namespace ManuHub.Ytdlp.NET;

/// <summary>
/// Fluent configuration methods for Ytdlp. 
/// These methods return a new instance of Ytdlp with the specified option added, allowing for chaining multiple configuration calls in a fluent manner.
/// </summary>
public sealed partial class Ytdlp
{
    // ==================================================================================================================
    // WORKGROUNDS OPTIONS
    // ==================================================================================================================

    /// <summary>
    /// Force the specified encoding (experimental)
    /// </summary>
    /// <param name="encoding"></param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    /// <exception cref="ArgumentException"></exception>
    public Ytdlp WithEncoding(string encoding)
    {
        if (string.IsNullOrWhiteSpace(encoding))
            throw new ArgumentException(nameof(encoding));
        return this.AddOption("--encoding", encoding);
    }

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
}
