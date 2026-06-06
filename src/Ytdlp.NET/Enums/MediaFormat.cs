namespace ManuHub.Ytdlp.NET;

/// <summary>
/// Represents supported media container and audio formats for yt-dlp output.
/// </summary>
/// <remarks>
/// This includes both video container formats (e.g., MP4, MKV) and audio formats
/// (e.g., MP3, FLAC). The actual availability depends on yt-dlp and ffmpeg support.
/// </remarks>
public enum MediaFormat
{
    /// <summary>
    /// AVI video container format.
    /// </summary>
    Avi,

    /// <summary>
    /// FLV Flash video format.
    /// </summary>
    Flv,

    /// <summary>
    /// GIF animated image format.
    /// </summary>
    Gif,

    /// <summary>
    /// Matroska video container format (MKV).
    /// </summary>
    Mkv,

    /// <summary>
    /// QuickTime video container format (MOV).
    /// </summary>
    Mov,

    /// <summary>
    /// MP4 video container format.
    /// </summary>
    Mp4,

    /// <summary>
    /// WebM video container format.
    /// </summary>
    Webm,

    /// <summary>
    /// AAC audio format.
    /// </summary>
    Aac,

    /// <summary>
    /// AIFF uncompressed audio format.
    /// </summary>
    Aiff,

    /// <summary>
    /// ALAC lossless audio format.
    /// </summary>
    Alac,

    /// <summary>
    /// FLAC lossless audio format.
    /// </summary>
    Flac,

    /// <summary>
    /// M4A audio container format.
    /// </summary>
    M4a,

    /// <summary>
    /// Matroska audio container format (MKA).
    /// </summary>
    Mka,

    /// <summary>
    /// MP3 compressed audio format.
    /// </summary>
    Mp3,

    /// <summary>
    /// OGG container format.
    /// </summary>
    Ogg,

    /// <summary>
    /// Opus audio codec.
    /// </summary>
    Opus,

    /// <summary>
    /// Vorbis audio codec.
    /// </summary>
    Vorbis,

    /// <summary>
    /// WAV uncompressed audio format.
    /// </summary>
    Wav,
}