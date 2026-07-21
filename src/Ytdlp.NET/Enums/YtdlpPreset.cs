namespace ManuHub.Ytdlp.NET;

/// <summary>
/// Predefined preset aliases built into yt-dlp.
/// </summary>
public enum YtdlpPreset
{
    /// <summary>
    /// Extracts audio and converts to MP3 format with high quality settings.
    /// </summary>
    Mp3,

    /// <summary>
    /// Extracts audio and converts to AAC format with high quality settings.
    /// </summary>
    Aac,

    /// <summary>
    /// Downloads or remuxes video into an MP4 container format.
    /// </summary>
    Mp4,

    /// <summary>
    /// Matroska video container format (MKV).
    /// </summary>
    Mkv,

    /// <summary>
    /// Introduces random or configured sleep delays between video downloads to prevent rate-limiting or IP bans.
    /// </summary>
    Sleep
}