using ManuHub.Ytdlp.NET.Extensions;
using ManuHub.Ytdlp.NET.Helpers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ManuHub.Ytdlp.NET.Core;

/// <summary>
/// Factory responsible for creating configured process instances for yt-dlp execution.
/// </summary>
/// <remarks>
/// Encapsulates process configuration logic including executable resolution,
/// argument injection, and standard stream setup (stdout/stderr).
/// </remarks>
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

        // 1. Resolve to absolute path OR PATH lookup
        _ytdlpPath = ResolveYtDlp(ytdlpPath);

        // 2. Validate working directory
        _workingDirectory = workingDirectory ?? Environment.CurrentDirectory;

        if (!Directory.Exists(_workingDirectory))
            throw new DirectoryNotFoundException($"Working directory not found: {_workingDirectory}");

        // 3. Sanity check (protect against fake/corrupt binaries)
        ValidateBinary(_ytdlpPath);

        // 4. Platform permission fix (safe no-op on Windows)
        ToolPermissionManager.EnsureExecutableIfFile(_ytdlpPath);
    }

    /// <summary>
    /// Creates a configured <see cref="Process"/> instance for executing yt-dlp.
    /// </summary>
    /// <param name="arguments">
    /// The command-line arguments to pass to the yt-dlp executable.
    /// This should not include the executable path itself.
    /// </param>
    /// <returns>A fully configured <see cref="Process"/> ready to be started.</returns>
    /// <remarks>
    /// The process is configured with proper start information including:
    /// executable path, arguments, working directory, and redirected IO streams.
    /// </remarks>
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
            RedirectStandardInput = true, // For potential future use (e.g., username/password)

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
            logger?.Log(LogType.Information, $"Process priority tuning skipped: {ex.Message}");
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
            logger?.Log(LogType.Information, "Process tree killed successfully.");
        }
        catch (Exception ex)
        {
            logger?.Log(LogType.Error, $"Failed to kill process tree: {ex.Message}");
        }
    }

    private static string ResolveYtDlp(string path)
    {
        // If absolute or exists locally → use it
        if (File.Exists(path))
            return Path.GetFullPath(path);

        // Otherwise try PATH lookup
        var fromPath = Environment.GetEnvironmentVariable("PATH")?
            .Split(Path.PathSeparator)
            .Select(p => Path.Combine(p, path))
            .FirstOrDefault(File.Exists);

        if (fromPath != null)
            return Path.GetFullPath(fromPath);

        throw new FileNotFoundException($"yt-dlp executable not found: {path}");
    }

    private static void ValidateBinary(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"yt-dlp not found: {path}");

        var fileInfo = new FileInfo(path);

        // yt-dlp is NEVER tiny
        if (fileInfo.Length < 1024)
            throw new InvalidOperationException($"Invalid yt-dlp binary detected: {fileInfo.FullName} ({fileInfo.Length} bytes).");
    }
}