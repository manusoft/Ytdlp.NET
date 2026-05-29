using ManuHub.Ytdlp.NET;

namespace YtdlpNetConsoleApp.Helpers;

internal static class TablePrinter
{
    public static void PrintFormats(List<Format> formats)
    {
        Console.WriteLine();
        Console.WriteLine("ID   EXT   RESOLUTION   FPS   VCODEC        ACODEC      SIZE");
        Console.WriteLine("-------------------------------------------------------------------");

        foreach (var f in formats.Take(20))
        {
            string size =
                f.ApproxFileSizeBytes.HasValue
                    ? $"{f.ApproxFileSizeBytes.Value / 1024 / 1024} MB"
                    : f.ApproxFileSizeBytes.HasValue
                        ? $"~{f.ApproxFileSizeBytes / 1024 / 1024} MB"
                        : "-";

            Console.WriteLine(
                $"{f.Id,-4} " +
                $"{f.Extension,-5} " +
                $"{f.Resolution,-12} " +
                $"{(f.Fps?.ToString() ?? "-"),-5} " +
                $"{(f.VideoCodec ?? "-"),-12} " +
                $"{(f.AudioCodec ?? "-"),-10} " +
                $"{size}"
            );
        }

        if (formats.Count > 20)
            Console.WriteLine($"\n... and {formats.Count - 20} more formats");
    }

    public static void PrintMetadata(List<FormatMetadata> formats)
    {
        Console.WriteLine();
        Console.WriteLine("ID   EXT   RESOLUTION   FPS   VCODEC        ACODEC      SIZE");
        Console.WriteLine("-------------------------------------------------------------------");

        foreach (var f in formats.Take(20))
        {
            string size =
                f.Filesize.HasValue
                    ? $"{f.Filesize / 1024 / 1024} MB"
                    : f.FilesizeApprox.HasValue
                        ? $"~{f.FilesizeApprox / 1024 / 1024} MB"
                        : "-";

            Console.WriteLine(
                $"{f.FormatId,-4} " +
                $"{f.Ext,-5} " +
                $"{f.Resolution,-12} " +
                $"{(f.Fps?.ToString() ?? "-"),-5} " +
                $"{(f.Vcodec ?? "-"),-12} " +
                $"{(f.Acodec ?? "-"),-10} " +
                $"{size}"
            );
        }

        if (formats.Count > 20)
            Console.WriteLine($"\n... and {formats.Count - 20} more formats");
    }
}