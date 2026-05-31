using ManuHub.Ytdlp.NET;
using YtdlpNetConsoleApp.Helpers;

namespace YtdlpNetConsoleApp.Pages;

internal static class ExtractorPage
{
    public static async Task ShowAsync(Ytdlp ytdlp)
    {
        Console.Clear();

        ConsoleTheme.WriteSection("Extractor List");

        var extractors = await ytdlp.GetExtractorsAsync();
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

internal static class SubtitlePage
{
    public static async Task ShowAsync(Ytdlp ytdlp)
    {
        Console.Clear();

        ConsoleTheme.WriteSection("Subtitle List");

        var subtitles = await ytdlp.GetSubtitlesAsync("https://www.youtube.com/watch?v=dQw4w9WgXcQ");
        Console.WriteLine($"\nTotal available subtitles: {subtitles.Count}");

        int index = 1;

        foreach (var subtitle in subtitles)
        {
            Console.WriteLine($"{index++}. {subtitle.LanguageCode} {subtitle.Name}");
        }

        Console.WriteLine("\nPress any key...");
        Console.ReadKey();
    }

}