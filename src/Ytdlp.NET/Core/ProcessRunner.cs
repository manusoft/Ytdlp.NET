using System.Text;

namespace ManuHub.Ytdlp.NET.Core;

public sealed class ProcessRunner
{
    private readonly ProcessFactory _factory;
    private readonly ILogger _logger;

    public event EventHandler<string>? OnErrorReceived;
    public event EventHandler<CommandCompletedEventArgs>? OnCommandCompleted;

    public ProcessRunner(ProcessFactory factory, ILogger logger)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// The universal execution pipe for ALL yt-dlp operations (downloads, probes, format list, etc.)
    /// </summary>
    public async Task<ProcessResult> ExecuteAsync(string arguments,
                                                  Action<string>? onLineReceived = null,
                                                  CancellationToken ct = default,
                                                  bool tuneProcess = true,
                                                  bool captureFullOutput = false)
    {
        using var process = _factory.Create(arguments);
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var outputBuilder = captureFullOutput ? new StringBuilder(capacity: 128 * 1024) : null; // Pre-allocate for large JSON

        int completed = 0;
        void Complete(bool success, string message)
        {
            if (Interlocked.Exchange(ref completed, 1) == 0)
                OnCommandCompleted?.Invoke(this, new CommandCompletedEventArgs(success, message));
        }

        try
        {
            process.Exited += (_, _) => tcs.TrySetResult(true);

            if (!process.Start())
                throw new YtdlpException("Failed to start yt-dlp.");

            if (tuneProcess)
                ProcessFactory.Tune(process);

            // -----------------------------------------------------------------
            // STDOUT Pump: Active high-speed background reader
            // -----------------------------------------------------------------
            var stdoutTask = Task.Run(async () =>
            {
                using var reader = process.StandardOutput;
                while (!ct.IsCancellationRequested)
                {
                    // No Task.WhenAny needed. Pass token directly to avoid allocations.
                    string? line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
                    if (line == null) break;

                    onLineReceived?.Invoke(line);
                    outputBuilder?.AppendLine(line);
                }
            }, ct);

            // -----------------------------------------------------------------
            // STDERR Pump: Active background error logging reader
            // -----------------------------------------------------------------
            var stderrTask = Task.Run(async () =>
            {
                using var reader = process.StandardError;
                while (!ct.IsCancellationRequested)
                {
                    string? line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
                    if (line == null) break;

                    OnErrorReceived?.Invoke(this, line);
                    _logger.Log(LogType.Error, line);
                }
            }, ct);

            // -----------------------------------------------------------------
            // Process Tree Cancellation Control
            // -----------------------------------------------------------------
            using var registration = ct.Register(() =>
            {
                if (!process.HasExited)
                {
                    _logger.Log(LogType.Info, "Cancellation requested -> Safe killing process tree");
                    ProcessFactory.SafeKill(process, _logger);
                }
            });

            // Wait cleanly for streams to flush out and the process to finish
            await Task.WhenAll(stdoutTask, stderrTask, tcs.Task).ConfigureAwait(false);

            if (!process.HasExited)
                ProcessFactory.SafeKill(process, _logger);

            await process.WaitForExitAsync(ct).ConfigureAwait(false);

            bool success = process.ExitCode == 0 && !ct.IsCancellationRequested;
            string message = success ? "Execution completed successfully"
                           : ct.IsCancellationRequested ? "Execution cancelled by user"
                           : $"Execution failed with exit code {process.ExitCode}";

            Complete(success, message);

            return new ProcessResult(
                 success,
                 process.ExitCode,
                 message,
                 outputBuilder?.ToString());
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
}

public record ProcessResult(bool IsSuccess, int ExitCode, string Message, string? FullOutput = null);