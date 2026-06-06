namespace ManuHub.Ytdlp.NET;

/// <summary>
/// Represents available yt-dlp post-processing operations.
/// </summary>
/// <remarks>
/// Post-processors are executed after download completion to modify, fix,
/// merge, or enhance media files using yt-dlp and ffmpeg.
/// </remarks>
public enum PostProcessors
{
    /// <summary>
    /// Merges separate audio and video streams into a single file.
    /// </summary>
    Merger,

    /// <summary>
    /// Modifies chapter metadata in the media file.
    /// </summary>
    ModifyChapters,

    /// <summary>
    /// Splits media into multiple files based on chapter markers.
    /// </summary>
    SplitChapters,

    /// <summary>
    /// Extracts audio from the video stream.
    /// </summary>
    ExtractAudio,

    /// <summary>
    /// Remuxes video into a different container without re-encoding.
    /// </summary>
    VideoRemuxer,

    /// <summary>
    /// Converts video into another format using re-encoding.
    /// </summary>
    VideoConvertor,

    /// <summary>
    /// Embeds or updates metadata in the media file.
    /// </summary>
    Metadata,

    /// <summary>
    /// Embeds subtitle tracks into the media file.
    /// </summary>
    EmbedSubtitle,

    /// <summary>
    /// Embeds thumbnail image into the media file.
    /// </summary>
    EmbedThumbnail,

    /// <summary>
    /// Converts subtitle formats.
    /// </summary>
    SubtitlesConvertor,

    /// <summary>
    /// Converts thumbnail image formats.
    /// </summary>
    ThumbnailsConvertor,

    /// <summary>
    /// Fixes stretched video aspect ratio issues.
    /// </summary>
    FixupStretched,

    /// <summary>
    /// Fixes broken or invalid M4A files.
    /// </summary>
    FixupM4a,

    /// <summary>
    /// Fixes M3U8/HLS related issues.
    /// </summary>
    FixupM3u8,

    /// <summary>
    /// Fixes timestamp inconsistencies in media files.
    /// </summary>
    FixupTimestamp,

    /// <summary>
    /// Fixes incorrect duration metadata.
    /// </summary>
    FixupDuration
}