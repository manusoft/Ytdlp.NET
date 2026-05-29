using ManuHub.Ytdlp.NET;
using YtdlpNetConsoleApp.Helpers;

namespace YtdlpNetConsoleApp.Pages;

internal static class ExtractorPage
{
    public static async Task ShowAsync(Ytdlp ytdlp)
    {
        Console.Clear();

        ConsoleTheme.WriteSection("Extractor List");

        var extractors = await ytdlp.ExtractorsAsync();
        Console.WriteLine($"\nTotal extractors: {extractors.Count}");

        int index = 1;

        foreach (var extractor in extractors)
        {
            Console.WriteLine($"{index++}. {extractor}");
        }

        Console.WriteLine("\nPress any key...");
        Console.ReadKey();
    }

}