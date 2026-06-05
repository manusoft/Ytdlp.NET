namespace ManuHub.Ytdlp.NET.Helpers;

/// <summary>
/// Resolves the FFmpeg path based on user input, supporting direct file paths, directories, and system PATH lookups.
/// </summary>
internal static class FfmpegResolver
{
    private static readonly string FfmpegName =
        OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";

    private static readonly string FfprobeName =
        OperatingSystem.IsWindows() ? "ffprobe.exe" : "ffprobe";

    public static string Resolve(string location)
    {
        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("FFmpeg location cannot be null or empty.");

        location = location.Replace('\\', '/');

        // CASE 1: direct file
        if (File.Exists(location))
        {
            ToolPermissionManager.EnsureExecutableIfFile(location);
            return location;
        }

        // CASE 2: folder
        if (Directory.Exists(location))
        {
            ValidateFolder(location);
            return location;
        }

        // CASE 3: PATH or alias
        return location;
    }

    private static void ValidateFolder(string folder)
    {
        // REQUIRED: ffmpeg
        var ffmpegPath = Path.Combine(folder, FfmpegName);

        if (!File.Exists(ffmpegPath))
            throw new FileNotFoundException($"FFmpeg folder must contain ffmpeg binary: {ffmpegPath}");

        ToolPermissionManager.EnsureExecutableIfFile(ffmpegPath);

        // OPTIONAL: ffprobe
        var ffprobePath = Path.Combine(folder, FfprobeName);

        if (File.Exists(ffprobePath))
        {
            ToolPermissionManager.EnsureExecutableIfFile(ffprobePath);
        }
        // else: silently ignore
    }
}
