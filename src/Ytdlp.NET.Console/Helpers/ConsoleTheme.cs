namespace YtdlpNetConsoleApp.Helpers;

public static class ConsoleTheme
{
    public static void ShowSplash()
    {
        Console.Clear();
        WriteBanner();
        Console.WriteLine();
        Console.WriteLine("Loading...");
        Thread.Sleep(1000);
    }

    public static void WriteBanner()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;

        Console.WriteLine(@"
            ██╗   ██╗████████╗██████╗ ██╗     ██████╗
            ╚██╗ ██╔╝╚══██╔══╝██╔══██╗██║     ██╔══██╗
             ╚████╔╝    ██║   ██║  ██║██║     ██████╔╝
              ╚██╔╝     ██║   ██║  ██║██║     ██╔═══╝
               ██║      ██║   ██████╔╝███████╗██║
               ╚═╝      ╚═╝   ╚═════╝ ╚══════╝╚═╝
            ");

        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("Ytdlp.NET Interactive Demo");
        Console.WriteLine("Fast • Modern • Async • Cross Platform");
        Console.ResetColor();
        Console.WriteLine();
    }

    public static void WriteSection(string title) 
    { 
        Console.ForegroundColor = ConsoleColor.Yellow; 
        Console.WriteLine(); 
        Console.WriteLine($"==== {title} ===="); 
        Console.WriteLine(); 
        Console.ResetColor(); }
}