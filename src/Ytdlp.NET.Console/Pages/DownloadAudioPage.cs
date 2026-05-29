using ManuHub.Ytdlp.NET;
using YtdlpNetConsoleApp.Helpers;

namespace YtdlpNetConsoleApp.Pages;

internal static class DownloadAudioPage
{
    public static async Task ShowAsync(Ytdlp baseYtdlp)
    {
        Console.Clear();
        ConsoleTheme.WriteSection("Audio Extractor");

        var url = ConsoleExtensions.ReadInput("Enter video URL");

        if (string.IsNullOrWhiteSpace(url))
            return;

        var ytdlp = baseYtdlp
            .WithFormat("ba")
            .WithExtractAudio(AudioFormat.Mp3)
            .WithOutputFolder("./downloads/audio")
            .WithOutputTemplate("%(title)s.%(ext)s");

        Console.WriteLine("\nExtracting audio...\n");

        await ytdlp.DownloadAsync(url);

        ConsoleExtensions.WriteLineColor("Audio extraction complete!", ConsoleColor.Green);

        Console.WriteLine("\nPress any key...");
        Console.ReadKey();
    }
}

internal static class SponsorBlockPage
{
    public static async Task ShowAsync(Ytdlp baseYtdlp)
    {
        Console.Clear();
        ConsoleTheme.WriteSection("SponsorBlock Demo");

        var url = ConsoleExtensions.ReadInput("Enter video URL");

        if (string.IsNullOrWhiteSpace(url))
            return;

        var ytdlp = baseYtdlp
            .WithFormat("best")
            .WithSponsorblockRemove("all")  // Removes sponsor, intro, etc.
            .WithOutputFolder("./downloads/sponsorblock");

        Console.WriteLine("\nExtracting video...\n");

        await ytdlp.DownloadAsync(url);

        ConsoleExtensions.WriteLineColor("Video extraction complete!", ConsoleColor.Green);

        Console.WriteLine("\nPress any key...");
        Console.ReadKey();
    }
}