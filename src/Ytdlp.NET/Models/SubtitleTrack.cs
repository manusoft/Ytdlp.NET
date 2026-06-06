namespace ManuHub.Ytdlp.NET;

/// <summary>
/// Represents a subtitle track available for a media item.
/// </summary>
/// <remarks>
/// A subtitle track may contain multiple formats (e.g., vtt, srt, json3) for the same language.
/// </remarks>
public class SubtitleTrack
{
    /// <summary>
    /// ISO language code of the subtitle track (e.g., "en", "fr", "ar").
    /// </summary>
    public string? LanguageCode { get; set; }

    /// <summary>
    /// Human-readable name of the subtitle track (if provided by the source).
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// List of available subtitle formats for this track (e.g., "vtt", "srt").
    /// </summary>
    public List<string> Formats { get; set; } = new List<string>();
}