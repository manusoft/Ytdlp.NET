namespace ManuHub.Ytdlp.NET;

/// <summary>
/// Represents a single subtitle track available for a video from yt-dlp's --list-subs output.
/// </summary>
public class SubtitleTrack
{
    public string? LanguageCode { get; set; }
    public string? Name { get; set; }
    public List<string> Formats { get; set; } = new List<string>();
}