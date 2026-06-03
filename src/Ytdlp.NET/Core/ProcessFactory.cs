using ManuHub.Ytdlp.NET.Extensions;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ManuHub.Ytdlp.NET.Core;

public sealed class ProcessFactory
{
    private readonly string _ytdlpPath;
    private readonly string _workingDirectory;

    /// <summary>
    /// Initializes the factory with a validated yt-dlp binary path and optional working directory.
    /// Fails fast with a clear diagnostic if either path does not exist.
    /// Note: KillTree() is sourced from Microsoft.Extensions.Process (ProcessExtensions).
    /// </summary>
    public ProcessFactory(string ytdlpPath, string? workingDirectory = null)
    {
        if (string.IsNullOrWhiteSpace(ytdlpPath))
            throw new ArgumentException("yt-dlp path cannot be empty.", nameof(ytdlpPath));

        if (!File.Exists(ytdlpPath))
            throw new FileNotFoundException($"yt-dlp executable not found at: {ytdlpPath}", ytdlpPath);

        _workingDirectory = workingDirectory ?? Environment.CurrentDirectory;

        if (!Directory.Exists(_workingDirectory))
            throw new DirectoryNotFoundException($"Working directory not found: {_workingDirectory}");

        _ytdlpPath = ytdlpPath;
    }

    public Process Create(string arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
            throw new ArgumentException("Arguments cannot be empty.", nameof(arguments));

        var psi = new ProcessStartInfo
        {
            FileName = _ytdlpPath,
            Arguments = arguments,

            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = false, // Never redirect unless actively writing to stdin.
                                           // Leaving it redirected with no writer causes yt-dlp
                                           // (Python) to block on interactive prompts instead
                                           // of defaulting to non-interactive behavior.

            UseShellExecute = false,
            CreateNoWindow = true,

            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,

            WorkingDirectory = _workingDirectory
        };

        // ── Encoding: cross-platform (PYTHONIOENCODING + PYTHONUTF8 are the correct pair) ──
        psi.Environment["PYTHONIOENCODING"] = "utf-8";
        psi.Environment["PYTHONUTF8"] = "1";
        psi.Environment["PYTHONUNBUFFERED"] = "1"; // Disable Python output buffering — ensures
                                                   // stdout/stderr are flushed immediately
                                                   // rather than held in Python's internal buffer

        // ── Unix-only locale vars (harmless on Windows but serve no purpose there) ──────────
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            psi.Environment["LC_ALL"] = "en_US.UTF-8";
            psi.Environment["LANG"] = "en_US.UTF-8";
        }

        return new Process
        {
            StartInfo = psi,
            EnableRaisingEvents = true  // Required for the Exited event in ProcessRunner.
        };
    }

    /// <summary>
    /// Lowers process priority to reduce system impact during long downloads or batch runs.
    /// Failures are logged rather than swallowed — priority tuning is best-effort on all platforms.
    /// </summary>
    public static void Tune(Process process, ILogger? logger = null)
    {
        try
        {
            if (!process.HasExited)
                process.PriorityClass = ProcessPriorityClass.BelowNormal;
        }
        catch (Exception ex)
        {
            logger?.Log(LogType.Info, $"Process priority tuning skipped: {ex.Message}");
        }
    }

    /// <summary>
    /// Kills the entire process tree. Safe to call if the process has already exited.
    /// KillTree() is sourced from Microsoft.Extensions.Process (ProcessExtensions).
    /// </summary>
    public static void SafeKill(Process process, ILogger? logger = null)
    {
        try
        {
            if (process.HasExited)
                return;

            process.KillTree();
            logger?.Log(LogType.Info, "Process tree killed successfully.");
        }
        catch (Exception ex)
        {
            logger?.Log(LogType.Error, $"Failed to kill process tree: {ex.Message}");
        }
    }
}