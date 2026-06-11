namespace ManuHub.Ytdlp.NET;

/// <summary>
/// Partial class containing download-related methods and events for the Ytdlp wrapper.
/// </summary>
public sealed partial class Ytdlp
{
    #region Events
    // Progress events
    /// <summary>Raised to report download progress updates from yt-dlp.</summary>
    /// <remarks>Provides real-time progress information such as percentage, speed, downloaded size, and estimated time remaining.</remarks>
    public event EventHandler<DownloadProgressEventArgs>? ProgressDownload;
    /// <summary>Raised when a progress/status message is produced.</summary>
    public event EventHandler<string>? ProgressMessage;

    // Output events
    /// <summary>Raised when a raw output line is emitted by yt-dlp.</summary>
    public event EventHandler<string>? OutputMessage;
    /// <summary>Raised when an error message is emitted by yt-dlp or the process runner.</summary>
    public event EventHandler<string>? ErrorMessage;

    // Lifecycle events
    /// <summary>Raised when a download operation completes successfully.</summary>
    public event EventHandler<string>? DownloadCompleted;
    /// <summary>Raised when the underlying process completes (success or failure).</summary>
    public event EventHandler<CommandCompletedEventArgs>? CommandCompleted;

    // Post-Processing events
    /// <summary>Raised when post-processing starts (e.g., merging, ffmpeg step).</summary>
    public event EventHandler<string>? PostProcessingStarted;
    /// <summary>Raised when post-processing finishes.</summary>
    public event EventHandler<string>? PostProcessingCompleted;   
    
    #endregion

    // ==================================================================================================================
    // Download Functions
    // ==================================================================================================================

    #region Donnload Methods

    /// <summary>
    /// Executes download processing for a URL.
    /// </summary>
    /// <param name="url">The source URL to download.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to stop the execution.</param>
    /// <param name="tuneProcess">Whether to tune the process for better performance (true by default). If false, the process will use the default buffer size and may have slower output processing.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="YtdlpException"></exception>
    public async Task DownloadAsync(string url, CancellationToken ct = default, bool tuneProcess = true)
    {
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL required", nameof(url));

        try
        {
            if (!string.IsNullOrWhiteSpace(_outputFolder)) Directory.CreateDirectory(_outputFolder);
            if (!string.IsNullOrWhiteSpace(_homeFolder)) Directory.CreateDirectory(_homeFolder);
            if (!string.IsNullOrWhiteSpace(_tempFolder)) Directory.CreateDirectory(_tempFolder);
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Error, $"Failed to create required folders: {ex.Message}");
            throw new YtdlpException("Failed to create required folders", ex);
        }

        var argsList = BuildArguments(url);
        var arguments = string.Join(" ", argsList.Select(EscapeArgument));

        _logger.Log(LogType.Information, $"Executing: {_ytdlpPath} {arguments}");

        // Isolated per-call — safe for concurrent downloads on the same Ytdlp instance
        var progressParser = new ProgressParser(_logger);
        var runner = CreateRunner();

        // ── Wire progress parser events → Ytdlp public events ─────────────────
        void OnProgressDownloadHandler(object? s, DownloadProgressEventArgs e) => ProgressDownload?.Invoke(this, e);
        void OnProgressMessageHandler(object? s, string msg) => ProgressMessage?.Invoke(this, msg);
        void OnCompleteDownloadHandler(object? s, string msg) => DownloadCompleted?.Invoke(this, msg);
        void OnPostProcessingStartHandler(object? s, string msg) => PostProcessingStarted?.Invoke(this, msg);
        void OnPostProcessingCompleteHandler(object? s, string msg) => PostProcessingCompleted?.Invoke(this, msg);

        progressParser.ProgressDownload += OnProgressDownloadHandler;
        progressParser.ProgressMessage += OnProgressMessageHandler;
        progressParser.DownloadCompleted += OnCompleteDownloadHandler;
        progressParser.PostProcessingStarted += OnPostProcessingStartHandler;
        progressParser.PostProcessingCompleted += OnPostProcessingCompleteHandler;

        // ── Wire runner events → Ytdlp public events ──────────────────────────
        void OnOutputMessageHandler(object? s, string msg) => OutputMessage?.Invoke(this, msg);
        void OnErrorMessageHandler(object? s, string msg) => ErrorMessage?.Invoke(this, msg);
        void OnCommandCompletedHandler(object? s, CommandCompletedEventArgs e) => CommandCompleted?.Invoke(this, e);

        runner.ErrorReceived += OnErrorMessageHandler;
        runner.CommandCompleted += OnCommandCompletedHandler;

        try
        {
            await runner.ExecuteAsync(
                arguments: arguments,
                auth: _auth,
                adobePass: _adobePass,
                onLineReceived: line =>
                {
                    // Feed each stdout line through the progress parser
                    try { progressParser.ParseProgress(line); }
                    catch (Exception ex) { _logger.Log(LogType.Error, $"Progress parse error: {ex.Message}"); }

                    OnOutputMessageHandler(null, line);
                },
                ct: ct,
                tuneProcess: tuneProcess,
                captureFullOutput: false);
        }
        finally
        {
            // Always unsubscribe — prevents memory leaks on cancel or exception
            progressParser.ProgressDownload -= OnProgressDownloadHandler;
            progressParser.ProgressMessage -= OnProgressMessageHandler;
            progressParser.DownloadCompleted -= OnCompleteDownloadHandler;
            progressParser.PostProcessingStarted -= OnPostProcessingStartHandler;
            progressParser.PostProcessingCompleted -= OnPostProcessingCompleteHandler;

            runner.ErrorReceived -= OnErrorMessageHandler;
            runner.CommandCompleted -= OnCommandCompletedHandler;
        }
    }

    /// <summary>
    /// Executes batch download processing for a collection of URLs with a specified concurrency limit.
    /// </summary>
    /// <param name="urls">An enumerable collection of source URLs to process.</param>
    /// <param name="maxConcurrency">The maximum number of simultaneous yt-dlp processes (default is 3).</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to stop the batch execution.</param>
    /// <param name="tuneProcess">Whether to tune the processes for better performance (true by default). If false, the processes will use the default buffer size and may have slower output processing.</param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous execution of the process.
    /// </returns>
    /// <exception cref="YtdlpException"></exception>
    public async Task DownloadBatchAsync(IEnumerable<string> urls, int maxConcurrency = 3, CancellationToken ct = default, bool tuneProcess = true)
    {
        var urlList = urls?.ToList();
        if (urlList == null || urlList.Count == 0)
        {
            _logger.Log(LogType.Error, "No URLs provided for batch download.");
            throw new YtdlpException("No URLs provided for batch download.");
        }

        using var throttler = new SemaphoreSlim(maxConcurrency);

        var tasks = urlList.Select(async url =>
        {
            await throttler.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                await DownloadAsync(url, ct, tuneProcess).ConfigureAwait(false);
            }
            catch (YtdlpException ex)
            {
                _logger.Log(LogType.Error, $"Skipping {url}: {ex.Message}");
            }
            finally
            {
                throttler.Release();
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    #endregion
}
