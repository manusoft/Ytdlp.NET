using ManuHub.Ytdlp.NET;
using YtdlpNetConsoleApp.Helpers;

namespace YtdlpNetConsoleApp.Pages;

internal static class SystemInfoPage
{
    public static async Task ShowAsync(Ytdlp ytdlp)
    {
        Console.Clear();

        ConsoleTheme.WriteSection("System Information");

        Console.WriteLine("Checking yt-dlp version...");
        var version = await ytdlp.VersionAsync();

        Console.WriteLine($"yt-dlp Version : {version}");

        Console.WriteLine("\nChecking update...");
        var update = await ytdlp.UpdateAsync(UpdateChannel.Stable);

        Console.WriteLine($"Update Status   : {update}");

        Console.WriteLine("\nExtractors...");
        var extractors = await ytdlp.ExtractorsAsync();

        Console.WriteLine($"Available Extractors: {extractors.Count}");

        Console.WriteLine("\nPress any key to return...");
        Console.ReadKey();
    }
}