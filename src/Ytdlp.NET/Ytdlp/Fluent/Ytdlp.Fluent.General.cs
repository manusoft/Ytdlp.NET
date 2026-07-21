using ManuHub.Ytdlp.NET.Helpers;

namespace ManuHub.Ytdlp.NET;

/// <summary>
/// Fluent configuration methods for Ytdlp. 
/// These methods return a new instance of Ytdlp with the specified option added, allowing for chaining multiple configuration calls in a fluent manner.
/// </summary>
public sealed partial class Ytdlp
{
    // ==================================================================================================================
    // GENERAL OPTIONS
    // ==================================================================================================================

    /// <summary>
    /// Ignore download and postprocessing errors. The download will be considered successful even if the postprocessing fails
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithIgnoreErrors() => AddFlag("--ignore-errors");

    /// <summary>
    /// IgAbort downloading of further videos if an error occurs 
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithAbortOnError() => AddFlag("--abort-on-error");

    /// <summary>
    /// Don't load any more configuration files except those given to <see cref="WithConfigLocations(string)"/>.
    /// For backward compatibility, if this option is found inside the system configuration file, the user configuration is not loaded.
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithIgnoreConfig() => AddFlag("--ignore-config");

    /// <summary>
    /// Location of the main configuration file;either the path to the config or its containing directory ("-" for stdin). 
    /// Can be used multiple times and inside other configuration files.
    /// </summary>
    /// <param name="path"></param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithConfigLocations(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Config folder path required");
        return AddOption("--config-locations", Path.GetFullPath(path));
    }

    /// <summary>
    /// Path to an additional directory to search for plugins. This option can be used multiple times to add multiple directories.
    /// </summary>
    /// <param name="path"></param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    /// <exception cref="ArgumentException"></exception>
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
    /// <param name="runtimeLocation">Either the path to the binary or its containing directory</param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    /// <exception cref="ArgumentException"></exception>
    public Ytdlp WithJsRuntime(Runtime runtime, string runtimeLocation)
    {
        if (string.IsNullOrWhiteSpace(runtimeLocation))
            throw new ArgumentException($"Runtime {runtime} path required");

        var resolved = RuntimeResolver.Resolve(runtime, runtimeLocation);

        var builder = $"{runtime}:{resolved}";
        return AddOption("--js-runtime", builder);
    }

    /// <summary>
    /// Clear JavaScript runtimes to enable, including defaults and those provided by <see cref="WithJsRuntime(Runtime, string)"/>
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithNoJsRuntime() => AddFlag("--no-js-runtime");

    /// <summary>
    /// Allow yt-dlp to fetch specified remote components when required (e.g., "ejs:npm", "ejs:github").
    /// </summary>
    /// <param name="component">The remote component target (e.g. "ejs:github", "ejs:npm").</param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithRemoteComponent(string component)
    {
        if (string.IsNullOrWhiteSpace(component)) return this;
        return AddOption("--remote-components", component.Trim());
    }

    /// <summary>
    /// Allow yt-dlp to fetch multiple remote components when required.
    /// </summary>
    /// <param name="components">Array of remote components to allow.</param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithRemoteComponents(params string[] components)
    {
        if (components == null || components.Length == 0) return this;

        Ytdlp current = this;
        foreach (var component in components)
        {
            if (!string.IsNullOrWhiteSpace(component))
            {
                current = current.AddOption("--remote-components", component.Trim());
            }
        }
        return current;
    }

    /// <summary>
    /// Disallow fetching of all remote components, including any previously allowed components or defaults.
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithNoRemoteComponents() => AddFlag("--no-remote-components");

    /// <summary>
    /// Do not extract a playlist's URL result entries; some entry metadata may be missing and downloading may be bypassed
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithFlatPlaylist() => AddFlag("--flat-playlist");

    /// <summary>
    /// Download livestreams from the start. Currently experimental and only supported for YouTube, Twitch, and TVer.
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithLiveFromStart() => AddFlag("--live-from-start");

    /// <summary>
    /// Wait for scheduled streams to become available.Pass the minimum number of seconds(or range) to wait between retries
    /// </summary>
    /// <param name="maxWait"></param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
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
    /// Mark videos watched (even with <see cref="WithSimulate()"/>)
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithMarkWatched() => AddFlag("--mark-watched");

    /// <summary>
    /// Do not mark videos watched (default)
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithNoMarkWatched() => AddFlag("--no-mark-watched");

    /// <summary>
    /// Controls whether to emit color codes in output.
    /// </summary>
    /// <param name="policy">Color policy: "always", "auto", "never", "no_color", "auto-tty", or "no_color-tty".</param>
    /// <param name="stream">Optional stream target ("stdout" or "stderr").</param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithColor(string policy, string? stream = null)
    {
        if (string.IsNullOrWhiteSpace(policy)) return this;
        string prefix = string.IsNullOrWhiteSpace(stream) ? "" : $"{stream.Trim()}:";
        return AddOption("--color", $"{prefix}{policy.Trim()}");
    }

    /// <summary>
    /// Options that help maintain compatibility with youtube-dl or youtube-dlc configurations.
    /// </summary>
    /// <param name="options">Comma/space-separated compatibility options (e.g. "filename", "no-live-chat").</param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithCompatOptions(string options)
    {
        if (string.IsNullOrWhiteSpace(options)) return this;
        return AddOption("--compat-options", options.Trim());
    }

    /// <summary>
    /// Creates custom CLI aliases for option strings inside yt-dlp.
    /// </summary>
    /// <param name="alias">The alias name or names (e.g. "get-audio,-X").</param>
    /// <param name="options">The option expansion string (e.g. "-S aext:{0},abr -x --audio-format {0}").</param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithAlias(string alias, string options)
    {
        if (string.IsNullOrWhiteSpace(alias) || string.IsNullOrWhiteSpace(options)) return this;
        return AddOption("--alias", $"{alias.Trim()} \"{options.Trim()}\"");
    }

    /// <summary>
    /// Applies a predefined set of options (e.g. "mp3", "aac", "mp4", "mkv", "sleep").
    /// </summary>
    /// <param name="preset">The preset alias string.</param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithPresetAlias(string preset)
    {
        if (string.IsNullOrWhiteSpace(preset)) return this;
        return AddOption("--preset-alias", preset.Trim());
    }

    /// <summary>
    /// Applies a predefined set of options using a strongly-typed preset enum.
    /// </summary>
    /// <param name="preset">The preset to apply.</param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithPresetAlias(YtdlpPreset preset)
    {
        string presetName = preset switch
        {
            YtdlpPreset.Mp3 => "mp3",
            YtdlpPreset.Aac => "aac",
            YtdlpPreset.Mp4 => "mp4",
            YtdlpPreset.Mkv => "mkv",
            YtdlpPreset.Sleep => "sleep",
            _ => preset.ToString().ToLowerInvariant()
        };

        return AddOption("--preset-alias", presetName);
    }

    #region Downloaders

    /// <summary>
    /// Use aria2c as the external downloader with the specified number of connections per download. 
    /// This is a convenient wrapper around <see cref="WithDownloaderArgs(string, string)"/>
    /// </summary>
    /// <param name="connections"></param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithAria2(int connections = 16)
    {
        return new Ytdlp(this, extraOptions: new[]
            {
            ("--downloader", "aria2c"),
            ("--downloader-args", $"aria2c:-x{connections} -k1M")
            });
    }

    /// <summary>
    /// Use the native HLS downloader (requires ffmpeg). 
    /// This is usually faster than the default downloader for HLS streams and can be used as a workaround for certain extraction issues, 
    /// but may cause compatibility issues with some sites
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithHlsNative() => AddOption("--downloader", "hlsnative");

    /// <summary>
    /// Use ffmpeg as the external downloader with the specified extra arguments. 
    /// This is a convenient wrapper around <see cref="WithDownloaderArgs(string, string)"/>
    /// </summary>
    /// <param name="extraFfmpegArgs">Additional arguments to pass to ffmpeg. Can be null.</param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithFFmpegAsLiveDownloader(string? extraFfmpegArgs = null) => WithDownloaderArgs("ffmpeg", extraFfmpegArgs);

    #endregion

    #region Bonus

    /// <summary>
    /// Downloads the best available quality up to 1440p.
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp With1440pOrBest() => new Ytdlp(this, format: "bv*[height<=?1440]+bestaudio/best");

    /// <summary>
    /// Downloads the best available quality up to 1440p.
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp With1080pOrBest() => new Ytdlp(this, format: "bv*[height<=?1080]+bestaudio/best");

    /// <summary>
    /// Downloads the best available quality up to 1440p.
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp With720pOrBest() => new Ytdlp(this, format: "bv*[height<=?720]+bestaudio/best");

    /// <summary>
    /// Preset for remux to mp4 with embed metadata, chapters, thumbnail.
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithMp4PostProcessingPreset()
        => this
            .WithRemuxVideo("mp4")
            .WithEmbedMetadata()
            .WithEmbedChapters()
            .WithEmbedThumbnail();

    /// <summary>
    /// Output to mkv with remux and merge.
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithMkvOutput()
        => this
            .WithRemuxVideo("mkv")
            .WithMergeOutputFormat("mkv");

    /// <summary>
    /// Downloads the best available quality up to the specified height.
    /// </summary>
    /// <param name="height">Maximum video height in pixels.</param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="height"/> is less than or equal to zero.
    /// </exception>
    public Ytdlp WithMaxHeight(int height)
    {
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), "Height must be positive");

        string formatSelector = $"bv*[height<={height}]+bestaudio/best";
        return new Ytdlp(this, format: formatSelector);
    }

    /// <summary>
    /// Downloads the best available quality up to the specified height or best.
    /// </summary>
    /// <param name="height">Maximum video height in pixels.</param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="height"/> is less than or equal to zero.
    /// </exception>
    public Ytdlp WithMaxHeightOrBest(int height)
    {
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), "Height must be positive");

        string formatSelector = $"bv*[height<={height}]+bestaudio/best[height<={height}]/best";
        return new Ytdlp(this, format: formatSelector);
    }

    /// <summary>
    /// Selects the best available video and best available audio streams.
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithBestVideoPlusBestAudio() => new Ytdlp(this, format: "bv*+bestaudio/best");

    /// <summary>
    /// Selects the best available audio-only format.
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithBestAudioOnly() => new Ytdlp(this, format: "bestaudio");

    /// <summary>
    /// Excludes formats above 2160p and selects the best available video and audio.
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithNo4k() => new Ytdlp(this, format: "bv*[height<?2160]+bestaudio/best");

    /// <summary>
    /// Prefers the best available M4A audio format, falling back to the best available audio.
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithBestM4aAudio() => new Ytdlp(this, format: "bestaudio[ext=m4a]/bestaudio/best");
    #endregion
}
