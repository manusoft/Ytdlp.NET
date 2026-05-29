using ManuHub.Ytdlp.NET;
using YtdlpNetConsoleApp.Helpers;

namespace YtdlpNetConsoleApp.Pages;

internal static class FormatsPage
{
    public static async Task ShowAsync(Ytdlp ytdlp)
    {
        Console.Clear();

        ConsoleTheme.WriteSection("Format Inspector");

        var url = ConsoleExtensions.ReadInput("Enter video URL");

        var formats = await ytdlp.GetFormatsAsync(url);

        Console.WriteLine($"\nTotal formats: {formats.Count}");

        TablePrinter.PrintFormats(formats);

        Console.WriteLine("\nPress any key...");
        Console.ReadKey();
    }
}
