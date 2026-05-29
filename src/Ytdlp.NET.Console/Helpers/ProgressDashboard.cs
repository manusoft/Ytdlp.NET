namespace YtdlpNetConsoleApp.Helpers;

internal sealed class ProgressDashboard
{
    private int _lastPercent = -1;

    public void Update(double percent, string? speed, string? eta, string? size)
    {
        int rounded = (int)Math.Round(percent);

        if (rounded == _lastPercent) return;

        _lastPercent = rounded;

        int width = 40;
        int filled = (int)(width * percent / 100);

        string bar = new string('█', filled) + new string('░', width - filled);

        Console.SetCursorPosition(0, Console.CursorTop);

        Console.WriteLine();
        Console.WriteLine($"[{bar}] {rounded}%");
        Console.WriteLine($"Speed : {speed}");
        Console.WriteLine($"ETA   : {eta}");
        Console.WriteLine($"Size  : {size}");
    }

    public void Complete()
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine();
        Console.WriteLine("Download Complete");
        Console.ResetColor();
    }
}

