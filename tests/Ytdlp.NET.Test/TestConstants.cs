namespace ManuHub.Ytdlp.NET.Test;

/// <summary>
/// Shared constants and helpers used across test classes.
/// </summary>
internal static class TestConstants
{
    // A fake path — unit tests never actually invoke yt-dlp.exe
    public static readonly string FakeExePath = Path.Combine("tools", OperatingSystem.IsWindows() ? "yt-dlp.exe" : "yt-dlp");
    public static readonly string FakeFfmpegPath = Path.Combine("tools", OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg");
    public const string SampleUrl = "https://www.youtube.com/watch?v=RGg-Qx1rL9U";
    public const string SampleUrl2 = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
}
