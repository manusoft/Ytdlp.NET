namespace YtdlpNetConsoleApp.Helpers;

internal static class ConsoleExtensions
{
    public static void WriteColor(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ResetColor();
    }

    public static void WriteLineColor(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ResetColor();
    }

    public static string ReadInput(string label)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write($"{label}: ");
        Console.ResetColor();

        return Console.ReadLine() ?? string.Empty;
    }

    public static void ClearLine()
    {
        Console.Write("\r" + new string(' ', Console.BufferWidth - 1) + "\r");
    }
}