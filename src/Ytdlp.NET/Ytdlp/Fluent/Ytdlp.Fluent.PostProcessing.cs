using ManuHub.Ytdlp.NET.Helpers;
using ManuHub.Ytdlp.NET.Models.Auth;

namespace ManuHub.Ytdlp.NET;

/// <summary>
/// Fluent configuration methods for Ytdlp. 
/// These methods return a new instance of Ytdlp with the specified option added, allowing for chaining multiple configuration calls in a fluent manner.
/// </summary>
public sealed partial class Ytdlp
{
    // ==================================================================================================================
    // POST-PROCESSING OPTIONS
    // ==================================================================================================================

    /// <summary>
    /// Convert video files to audio-only files (requires ffmpeg and ffprobe).        
    /// </summary>
    /// <param name="format">Formats currently supported: best (default),aac, alac, flac, m4a, mp3, opus, vorbis, wav).</param>
    /// <param name="quality">Audio quality (0–10, lower = better). Default: 5 (medium)</param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
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
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
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
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
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
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
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
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithKeepVideo() => AddFlag("-k");

    /// <summary>
    /// Do not overwrite post-processed files
    /// </summary>
    public Ytdlp WithNoPostOverwrites() => AddFlag("--no-post-overwrites");

    /// <summary>
    /// Embed subtitles in the video (only for mp4, webm and mkv videos)
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithEmbedSubtitles() => AddFlag("--embed-subs");

    /// <summary>
    /// Embed thumbnail in the video as cover art
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithEmbedThumbnail() => AddFlag("--embed-thumbnail");

    /// <summary>
    /// Embed metadata to the video file
    /// </summary>
    public Ytdlp WithEmbedMetadata() => AddFlag("--embed-metadata");

    /// <summary>
    /// Add chapter markers to the video file
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithEmbedChapters() => AddFlag("--embed-chapters");

    /// <summary>
    /// Embed the infojson as an attachment to mkv/mka video files
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithEmbedInfoJson() => AddFlag("--embed-info-json");

    /// <summary>
    /// Do not embed the infojson as an attachment to the video file
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithNoEmbedInfoJson() => AddFlag("--no-embed-info-json");

    /// <summary>
    /// Replace text in a metadata field using the given regex. This option can be used multiple times.
    /// </summary>
    /// <param name="field"></param>
    /// <param name="regex"></param>
    /// <param name="replacement"></param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
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
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithConcatPlaylist(string policy = "always") => AddOption("--concat-playlist", policy);

    /// <summary>
    /// Location of the ffmpeg binary
    /// </summary>
    /// <param name="ffmpegLocation">Either the path to the binary or its containing directory</param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithFFmpegLocation(string? ffmpegLocation)
    {
        if (string.IsNullOrWhiteSpace(ffmpegLocation))
            throw new ArgumentException("FFmpeg location cannot be null or empty.");

        var resolved = FfmpegResolver.Resolve(ffmpegLocation);
        return new Ytdlp(this, ffmpegLocation: resolved);
    }

    /// <summary>
    /// Convert the subtitles to another format
    /// </summary>
    /// <param name="format">(currently supported: ass, lrc, srt, vtt)</param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
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
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
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
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithSplitChapters() => AddFlag("--split-chapters");

    /// <summary>
    /// Remove chapters whose title matches the given regular expression. The syntax is the same as <see cref="WithDownloadSections(string)"/>. 
    /// This option can be used multiple times to remove multiple sections"/>
    /// </summary>
    /// <param name="regex"></param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
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
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithUsePostProcessor(PostProcessors postProcessor, string? postProcessorArgs = null)
    {
        if (!string.IsNullOrWhiteSpace(postProcessorArgs))
            return AddOption("--use-postprocessor", $"{postProcessor.ToString().Trim()}:{postProcessorArgs.Trim()}");
        return AddOption("--use-postprocessor", postProcessor.ToString().Trim());
    }
}
