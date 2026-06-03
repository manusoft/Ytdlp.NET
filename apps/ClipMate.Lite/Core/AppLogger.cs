using ManuHub.Ytdlp.NET;

namespace VideoDownloader.Core;

public sealed class AppLogger : ILogger
{
    public void Log(LogType type, string message)
    {
        Console.WriteLine($"[{type}] {message}");
    }
}
