using ManuHub.Ytdlp.NET;
using YtdlpNetConsoleApp.Helpers;

namespace YtdlpNetConsoleApp.Pages;

internal static class BatchDownloadPage
{
    public static async Task ShowAsync(Ytdlp baseYtdlp)
    {
        Console.Clear();
        ConsoleTheme.WriteSection("Batch Downloader");

        Console.WriteLine("Enter URLs (empty line to finish):");

        var urls = new List<string>();

        while (true)
        {
            var url = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(url))
                break;

            urls.Add(url);
        }

        if (urls.Count == 0)
            return;

        var ytdlp = baseYtdlp
            .WithFormat("best[height<=720]")
            .WithOutputFolder("./downloads/batch");

        Console.WriteLine($"\nStarting batch download ({urls.Count} items)...\n");

        var dashboard = new ProgressDashboard();

        ytdlp.ProgressDownload += (_, p) =>
        {
            dashboard.Update(p.Percent, p.Speed, p.ETA, p.Size);
        };

        await ytdlp.DownloadBatchAsync(urls, maxConcurrency: 3);

        dashboard.Complete();

        ConsoleExtensions.WriteLineColor("Batch completed!", ConsoleColor.Green);

        Console.WriteLine("\nPress any key...");
        Console.ReadKey();
    }
}