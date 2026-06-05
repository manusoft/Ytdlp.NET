using ManuHub.Ytdlp.NET.Models.Auth;
using System.Diagnostics;
using System.Text;

namespace ManuHub.Ytdlp.NET.Core;

public sealed class ProcessRunner
{
    private readonly ProcessFactory _factory;
    private readonly ILogger _logger;

    /// <summary>Pre-allocated capacity for JSON/metadata stdout capture.</summary>
    private const int JsonOutputInitialCapacity = 128 * 1024;

    public event EventHandler<string>? OnErrorReceived;
    public event EventHandler<CommandCompletedEventArgs>? OnCommandCompleted;

    public ProcessRunner(ProcessFactory factory, ILogger logger)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// The universal execution pipe for ALL yt-dlp operations.
    /// Combines low-latency streaming callbacks with optional high-performance full output aggregation.
    /// </summary>
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
                OnCommandCompleted?.Invoke(this, new CommandCompletedEventArgs(success, message));
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
                    OnErrorReceived?.Invoke(this, line);
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
                    _logger.Log(LogType.Info, "Cancellation requested via token -> Safe killing process tree");
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

public record ProcessResult(bool IsSuccess, int ExitCode, string Message, string? FullOutput = null);



