namespace ManuHub.Ytdlp.NET;

/// <summary>
/// Fluent configuration methods for Ytdlp. 
/// These methods return a new instance of Ytdlp with the specified option added, allowing for chaining multiple configuration calls in a fluent manner.
/// </summary>
public sealed partial class Ytdlp
{
    // ==================================================================================================================
    // VERBOSITY AND SIMULATION OPTIONS
    // ==================================================================================================================

    /// <summary>
    /// Activate quiet mode. If used with --verbose, print the log to stderr
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithQuiet() => AddFlag("--quiet");

    /// <summary>
    /// Ignore warnings
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithNoWarnings() => AddFlag("--no-warnings");

    /// <summary>
    /// Do not download the video and do not write anything to disk
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithSimulate() => AddFlag("--simulate");

    /// <summary>
    /// Download the video even if printing/listing options are used
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithNoSimulate() => AddFlag("--no-simulate");

    /// <summary>
    /// Do not download the video but write all related files (Alias: --no-download)
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithSkipDownload() => AddFlag("--skip-download");

    /// <summary>
    /// Print various debugging information
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithVerbose() => AddFlag("--verbose");

}
