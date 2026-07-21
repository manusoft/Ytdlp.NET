namespace ManuHub.Ytdlp.NET;

/// <summary>
/// Represents supported audio output formats for yt-dlp extraction.
/// </summary>
public enum AudioFormat
{
    /// <summary>
    /// Automatically selects the best available audio format.
    /// </summary>
    Best,

    /// <summary>
    /// AAC (Advanced Audio Coding) format.
    /// </summary>
    Aac,

    /// <summary>
    /// ALAC (Apple Lossless Audio Codec) format.
    /// </summary>
    Alac,

    /// <summary>
    /// FLAC (Free Lossless Audio Codec) format.
    /// </summary>
    Flac,

    /// <summary>
    /// M4A container format (typically AAC audio).
    /// </summary>
    M4a,

    /// <summary>
    /// MP3 compressed audio format.
    /// </summary>
    Mp3,

    /// <summary>
    /// Opus audio codec (efficient modern lossy format).
    /// </summary>
    Opus,

    /// <summary>
    /// Vorbis audio codec (commonly used in OGG containers).
    /// </summary>
    Vorbis,

    /// <summary>
    /// WAV uncompressed audio format.
    /// </summary>
    Wav
}
