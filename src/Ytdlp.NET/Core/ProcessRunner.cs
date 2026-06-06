using ManuHub.Ytdlp.NET.Models.Auth;
using System.Diagnostics;
using System.Text;

namespace ManuHub.Ytdlp.NET.Core;

/// <summary>
/// Responsible for executing yt-dlp processes and managing their lifecycle,
/// including stdout/stderr streaming, cancellation, and completion tracking.
/// </summary>
/// <remarks>
/// This runner acts as the execution layer of the SDK. It creates processes
/// via <see cref="ProcessFactory"/>, streams output asynchronously, and
/// raises events for error handling and command completion.
/// </remarks>
public sealed class ProcessRunner
{
    private readonly ProcessFactory _factory;
    private readonly ILogger _logger;

    /// <summary>
    /// Pre-allocated buffer size used for capturing full JSON or metadata output from stdout.
    /// </summary>
    private const int JsonOutputInitialCapacity = 128 * 1024;

    /// <summary>
    /// Occurs when an error line is received from the process standard error stream.
    /// </summary>
    internal event EventHandler<string>? ErrorReceived;

    /// <summary>
    /// Occurs when the process has completed execution (success or failure).
    /// </summary>
    internal event EventHandler<CommandCompletedEventArgs>? CommandCompleted;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessRunner"/> class.
    /// </summary>
    /// <param name="factory">Factory used to create configured process instances for yt-dlp execution.</param>
    /// <param name="logger">Logger used for diagnostic and runtime execution messages.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="factory"/> or <paramref name="logger"/> is null.
    /// </exception>
    public ProcessRunner(ProcessFactory factory, ILogger logger)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// The universal execution pipe for ALL yt-dlp operations. Combines low-latency streaming callbacks with optional high-performance full output aggregation.
    /// Executes the yt-dlp process asynchronously with the specified arguments and optional authentication.
    /// </summary>
    /// <param name="arguments">
    /// Command-line arguments to pass to yt-dlp. This should NOT include the executable path or URL injection logic.
    /// </param>
    /// <param name="auth">
    /// Optional authentication credentials used for protected resources.
    /// If provided, credentials will be injected securely into the process execution flow.
    /// </param>
    /// <param name="adobePass">
    /// Optional Adobe Pass authentication configuration for DRM-protected content.
    /// </param>
    /// <param name="onLineReceived">
    /// Optional callback invoked for each line of stdout output produced by the process.
    /// </param>
    /// <param name="ct">
    /// Cancellation token used to cancel execution and terminate the process gracefully.
    /// </param>
    /// <param name="tuneProcess">
    /// Indicates whether process priority and performance tuning should be applied before execution.
    /// </param>
    /// <param name="captureFullOutput">
    /// If true, captures the full stdout output into memory and returns it in the result.
    /// </param>
    /// <returns>
    /// A <see cref="ProcessResult"/> containing execution status, exit code, and optional output.
    /// </returns>
    /// <remarks>
    /// This method is the core execution pipeline for yt-dlp commands. It manages:
    /// process creation, authentication injection, streaming output handling,
    /// cancellation, and final result aggregation.
    /// </remarks>
    public async Task<ProcessResult> ExecuteAsync(string arguments,
                                                  YtdlpAuth? auth = null,
                                                  AdobePassAuth? adobePass = null,
                                                  Action<string>? onLineReceived = null,
                                                  CancellationToken ct = default,
                                                  bool tuneProcess = true,
                                                  bool captureFullOutput = false)
    {
        using var process = _factory.Create(arguments);
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        // Single writer (stdoutTask) + one final reader.
        // Lock acts as a memory barrier at ToString() time; kept on AppendLine for safety.
        var outputLock = new object();
        var outputBuilder = captureFullOutput ? new StringBuilder(JsonOutputInitialCapacity) : null;

        int completed = 0;
        void Complete(bool success, string message)
        {
            if (Interlocked.Exchange(ref completed, 1) == 0)
                CommandCompleted?.Invoke(this, new CommandCompletedEventArgs(success, message));
        }

        try
        {
            // Must be attached BEFORE Start() to avoid a race on fast-exiting processes.
            process.Exited += (_, _) => tcs.TrySetResult(true);

            if (!process.Start())
                throw new YtdlpException("Failed to start yt-dlp core engine.");

            // Inject auth immediately after process start to minimize the window where credentials are exposed in the OS process list.
            if (auth is not null)
                await WriteAuthAsync(process, auth);

            if (adobePass is not null)
                await WriteAdobePassAsync(process, adobePass);

            if (tuneProcess)
                ProcessFactory.Tune(process);

            // ── STDOUT pump ──────────────────────────────────────────────────────────────────
            // ct is intentionally NOT passed to ReadLineAsync or Task.Run.
            // Cancellation flows through SafeKill (below) → pipe closes → ReadLineAsync
            // returns null (EOF) naturally. Passing ct here aborts the read mid-stream
            // and silently drops the tail of stdout output.
            // ────────────────────────────────────────────────────────────────────────────────
            var stdoutTask = Task.Run(async () =>
            {
                using var reader = process.StandardOutput;
                string? line;
                while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
                {
                    onLineReceived?.Invoke(line);

                    if (outputBuilder != null)
                    {
                        lock (outputLock)
                            outputBuilder.AppendLine(line);
                    }
                }
            });

            // ── STDERR pump ──────────────────────────────────────────────────────────────────
            // Mirrors stdout exactly. Avoids BeginErrorReadLine + WaitForExitAsync race where
            // the OS can return the process handle before the internal .NET event-pump thread
            // finishes firing the last ErrorDataReceived callback, silently dropping stderr tail.
            // ────────────────────────────────────────────────────────────────────────────────
            var stderrTask = Task.Run(async () =>
            {
                using var reader = process.StandardError;
                string? line;
                while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
                {
                    ErrorReceived?.Invoke(this, line);
                    _logger.Log(LogType.Error, line);
                }
            });

            // ── Cancellation ─────────────────────────────────────────────────────────────────
            // Guard against ct.Register allocation when CancellationToken.None is passed.
            // default(CancellationTokenRegistration) is the BCL sentinel: using-dispose is a no-op.
            // ────────────────────────────────────────────────────────────────────────────────
            using var registration = ct.CanBeCanceled
                ? ct.Register(() =>
                {
                    if (process.HasExited) return;
                    _logger.Log(LogType.Information, "Cancellation requested via token -> Safe killing process tree");
                    ProcessFactory.SafeKill(process, _logger);
                })
                : default(CancellationTokenRegistration);

            // ── Drain ────────────────────────────────────────────────────────────────────────
            // All three must complete:
            //   tcs.Task   — process handle signalled (ExitCode is valid)
            //   stdoutTask — stdout pipe drained to EOF
            //   stderrTask — stderr pipe drained to EOF
            // On cancellation, SafeKill closes the pipes; both pumps reach EOF and finish
            // naturally, so WhenAll still resolves cleanly. Swallow OCE here — Complete()
            // and the result path below handle the cancelled outcome correctly.
            // ────────────────────────────────────────────────────────────────────────────────
            try
            {
                await Task.WhenAll(tcs.Task, stdoutTask, stderrTask).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { /* SafeKill fired; pipes closing; drain best-effort */ }

            // ── Final OS handle flush ────────────────────────────────────────────────────────
            // CancellationToken.None is intentional: the process is already dead at this point,
            // and passing a cancelled ct would throw immediately, bypassing Complete() and
            // the ProcessResult return below.
            // ────────────────────────────────────────────────────────────────────────────────
            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);

            bool success = process.ExitCode == 0 && !ct.IsCancellationRequested;
            string message = success ? "Execution completed successfully"
                           : ct.IsCancellationRequested ? "Execution cancelled by user"
                           : $"Execution failed with exit code {process.ExitCode}";

            Complete(success, message);

            string? finalOutput = null;
            if (outputBuilder != null)
            {
                lock (outputLock)
                    finalOutput = outputBuilder.ToString();
            }

            return new ProcessResult(success, process.ExitCode, message, finalOutput);
        }
        catch (OperationCanceledException)
        {
            Complete(false, "Execution cancelled by user");
            throw;
        }
        catch (Exception ex)
        {
            var msg = $"Fatal exception inside execution wrapper: {ex.Message}";
            _logger.Log(LogType.Error, msg);
            Complete(false, msg);
            throw new YtdlpException(msg, ex);
        }
    }

    private static async Task WriteAuthAsync(Process process, YtdlpAuth auth)
    {
        if (process.HasExited)
            return;

        try
        {
            // yt-dlp expects username first, then password
            if (!string.IsNullOrWhiteSpace(auth.Username))
            {
                await process.StandardInput
                    .WriteLineAsync(auth.Username)
                    .ConfigureAwait(false);
            }

            if (!string.IsNullOrWhiteSpace(auth.Password))
            {
                await process.StandardInput
                    .WriteLineAsync(auth.Password)
                    .ConfigureAwait(false);
            }

            await process.StandardInput.FlushAsync().ConfigureAwait(false);
            process.StandardInput.Close();
        }
        catch
        {
            // Never break execution due to auth injection failure
        }
    }

    private static async Task WriteAdobePassAsync(Process process, AdobePassAuth auth)
    {
        if (process.HasExited)
            return;

        try
        {
            await process.StandardInput
                .WriteLineAsync(auth.Mso)
                .ConfigureAwait(false);

            await process.StandardInput
                .WriteLineAsync(auth.Username)
                .ConfigureAwait(false);

            await process.StandardInput
                .WriteLineAsync(auth.Password)
                .ConfigureAwait(false);

            await process.StandardInput.FlushAsync().ConfigureAwait(false);
            process.StandardInput.Close();
        }
        catch
        {
            // Never break execution due to auth injection failure
        }
    }
}

/// <summary>
/// Represents the result of a completed process execution.
/// </summary>
/// <param name="IsSuccess">Indicates whether the process completed successfully (exit code 0 and no cancellation).</param>
/// <param name="ExitCode">The exit code returned by the process.</param>
/// <param name="Message">A human-readable message describing the execution result.</param>
/// <param name="FullOutput">Optional full captured stdout output from the process, if enabled.</param>
public record ProcessResult(bool IsSuccess, int ExitCode, string Message, string? FullOutput = null);



