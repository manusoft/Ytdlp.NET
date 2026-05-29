using ManuHub.Ytdlp.NET;

namespace YtdlpNetConsoleApp.Helpers;

internal sealed class DemoLogger : ILogger
{
    public void Log(LogType type, string message)
    {
        Console.ForegroundColor = type switch
        {
            LogType.Error => ConsoleColor.Red,
            LogType.Warning => ConsoleColor.Yellow,
            LogType.Debug => ConsoleColor.DarkGray,
            _ => ConsoleColor.Cyan
        };

        Console.WriteLine($"[{type}] {message}");

        Console.ResetColor();
    }
}


