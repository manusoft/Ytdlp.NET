using ManuHub.Ytdlp.NET;
using System.Diagnostics;
using YtdlpNetConsoleApp.Helpers;

namespace YtdlpNetConsoleApp.Pages;

internal static class MetadataPage
{
    public static async Task ShowAsync(Ytdlp ytdlp)
    {
        Console.Clear();

        ConsoleTheme.WriteSection("Metadata Explorer");

        var url = ConsoleExtensions.ReadInput("Enter video/playlist URL");

        if (string.IsNullOrWhiteSpace(url))
            return;

        try
        {
            Stopwatch sw = Stopwatch.StartNew();
            sw.Start();

            var metadata = await ytdlp.GetMetadataAsync(url);

            sw.Stop();

            if (metadata == null)
            {
                Console.WriteLine("No metadata found.");
                return;
            }

            Console.WriteLine($"\nTitle   : {metadata.Title}");
            Console.WriteLine($"ID      : {metadata.Id}");
            Console.WriteLine($"Type    : {metadata.Type}");
            Console.WriteLine($"Thumb   : {metadata.Thumbnail}");

            if (metadata.Type == "video")
            {
                Console.WriteLine("\nFormats:");
                TablePrinter.PrintMetadata(metadata.Formats ?? new List<FormatMetadata>());
            }

            if (metadata.Type == "playlist")
            {
                Console.WriteLine($"\nPlaylist entries: {metadata.Entries?.Count ?? 0}");

                foreach (var e in metadata.Entries?.Take(5) ?? [])
                {
                    Console.WriteLine($"- {e.Title}");
                }
            }
            
            Console.WriteLine($"\nTime taken: {sw.ElapsedMilliseconds} ms");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            Console.WriteLine("\nPress any key...");
            Console.ReadKey();
        }
    }    
}