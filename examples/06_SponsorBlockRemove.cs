using System;
using System.Threading.Tasks;
using ManuHub.Ytdlp.NET;

class Program
{
    static async Task Main()
    {
        var url = "https://www.youtube.com/watch?v=VIDEO_ID";

        var ytdlp = new Ytdlp()
            .RemoveSponsorBlock("all")
            .WithFormat("best")
            .WithOutputFolder("./sponsor-free");

        await ytdlp.DownloadAsync(url, CancellationToken.None);
    }
}