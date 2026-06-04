using System;
using System.Threading.Tasks;
using ManuHub.Ytdlp.NET;

class Program
{
    static async Task Main()
    {
        var ytdlp = new Ytdlp()
            .WithFormat("bestaudio")
            .ExtractAudio("mp3")
            .WithOutputFolder("./audio")
            .WithOutputTemplate("%(title)s - %(uploader)s.%(ext)s")
            .EmbedMetadata();

        ytdlp.OnProgressDownload += (s, e) =>
            Console.Write($"\rProgress: {e.Percent:F1}%  ETA: {e.ETA}");

        await ytdlp.DownloadAsync("https://www.youtube.com/watch?v=VIDEO_ID", CancellationToken.None);
    }
}