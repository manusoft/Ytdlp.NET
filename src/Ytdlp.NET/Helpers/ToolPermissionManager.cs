using System.Runtime.Versioning;

namespace ManuHub.Ytdlp.NET.Helpers;

/// <summary>
/// Ensures that the yt-dlp executable has the necessary permissions to run on Unix-like systems.
/// </summary>
internal static class ToolPermissionManager
{
    public static void EnsureExecutableIfFile(string path)
    {
        // Ensure we are explicitly running on a supported platform so the analyzer knows
        // this call site is not reachable on other platforms.
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
            return;

        if (string.IsNullOrWhiteSpace(path))
            return;

        if (!File.Exists(path))
            return;

        TrySetExecutable(path);
    }

    // Mark the helper as intended for non-Windows platforms so the analyzer won't warn about Unix-only APIs
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    private static void TrySetExecutable(string file)
    {
        try
        {
            var mode = File.GetUnixFileMode(file);

            const UnixFileMode exec =
                UnixFileMode.UserRead |
                UnixFileMode.UserWrite |
                UnixFileMode.UserExecute |
                UnixFileMode.GroupRead |
                UnixFileMode.GroupExecute |
                UnixFileMode.OtherRead |
                UnixFileMode.OtherExecute;

            if ((mode & UnixFileMode.UserExecute) == 0)
                File.SetUnixFileMode(file, mode | exec);
        }
        catch
        {
            // never break runtime
        }
    }
}
