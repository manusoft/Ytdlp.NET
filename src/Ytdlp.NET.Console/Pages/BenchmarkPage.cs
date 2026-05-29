using ManuHub.Ytdlp.NET;
using System.Diagnostics;
using YtdlpNetConsoleApp.Helpers;

namespace YtdlpNetConsoleApp.Pages;

internal static class BenchmarkPage
{
    public static async Task ShowAsync(Ytdlp ytdlp)
    {
        Console.Clear();
        ConsoleTheme.WriteSection("Performance Benchmark");

        var url = "https://www.youtube.com/watch?v=ZGnQH0LN_98";

        var sw = Stopwatch.StartNew();

        Console.WriteLine("Testing metadata...");
        var meta = await ytdlp.GetMetadataAsync(url);

        var t1 = sw.ElapsedMilliseconds;
        sw.Restart();

        Console.WriteLine("Testing formats...");
        var formats = await ytdlp.GetFormatsAsync(url);

        var t2 = sw.ElapsedMilliseconds;
        sw.Restart();

        Console.WriteLine("Testing deep metadata...");
        var deep = await ytdlp.GetDeepMetadataAsync(url);

        var t3 = sw.ElapsedMilliseconds;

        Console.WriteLine("\n==============================");
        Console.WriteLine(" BENCHMARK RESULTS");
        Console.WriteLine("==============================");
        Console.WriteLine($"Metadata       : {t1} ms");
        Console.WriteLine($"Formats        : {t2} ms");
        Console.WriteLine($"Deep Metadata   : {t3} ms");
        Console.WriteLine("==============================");

        Console.WriteLine("\nPress any key...");
        Console.ReadKey();
    }
}