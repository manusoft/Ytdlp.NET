namespace ManuHub.Ytdlp.NET;

/// <summary>
/// Partial class containing download-related methods and events for the Ytdlp wrapper.
/// </summary>
public sealed partial class Ytdlp
{
    #region Events
    public event EventHandler<DownloadProgressEventArgs>? OnProgressDownload;
    public event EventHandler<string>? OnProgressMessage;
    public event EventHandler<string>? OnOutputMessage;
    public event EventHandler<string>? OnCompleteDownload;
    public event EventHandler<string>? OnPostProcessingStart;
    public event EventHandler<string>? OnPostProcessingComplete;
    public event EventHandler<CommandCompletedEventArgs>? OnCommandCompleted;
    public event EventHandler<string>? OnErrorMessage;
    #endregion

    // ==================================================================================================================
    // Download Functions
    // ==================================================================================================================

    #region Execution & Utility Methods

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

        _logger.Log(LogType.Info, $"Executing: {_ytdlpPath} {arguments}");

        // Isolated per-call — safe for concurrent downloads on the same Ytdlp instance
        var progressParser = new ProgressParser(_logger);
        var runner = CreateRunner();

        // ── Wire progress parser events → Ytdlp public events ─────────────────
        void OnProgressDownloadHandler(object? s, DownloadProgressEventArgs e) => OnProgressDownload?.Invoke(this, e);
        void OnProgressMessageHandler(object? s, string msg) => OnProgressMessage?.Invoke(this, msg);
        void OnCompleteDownloadHandler(object? s, string msg) => OnCompleteDownload?.Invoke(this, msg);
        void OnPostProcessingStartHandler(object? s, string msg) => OnPostProcessingStart?.Invoke(this, msg);
        void OnPostProcessingCompleteHandler(object? s, string msg) => OnPostProcessingComplete?.Invoke(this, msg);

        progressParser.OnProgressDownload += OnProgressDownloadHandler;
        progressParser.OnProgressMessage += OnProgressMessageHandler;
        progressParser.OnCompleteDownload += OnCompleteDownloadHandler;
        progressParser.OnPostProcessingStart += OnPostProcessingStartHandler;
        progressParser.OnPostProcessingComplete += OnPostProcessingCompleteHandler;

        // ── Wire runner events → Ytdlp public events ──────────────────────────
        void OnOutputMessageHandler(object? s, string msg) => OnOutputMessage?.Invoke(this, msg);
        void OnErrorMessageHandler(object? s, string msg) => OnErrorMessage?.Invoke(this, msg);
        void OnCommandCompletedHandler(object? s, CommandCompletedEventArgs e) => OnCommandCompleted?.Invoke(this, e);

        runner.OnErrorReceived += OnErrorMessageHandler;
        runner.OnCommandCompleted += OnCommandCompletedHandler;

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
            progressParser.OnProgressDownload -= OnProgressDownloadHandler;
            progressParser.OnProgressMessage -= OnProgressMessageHandler;
            progressParser.OnCompleteDownload -= OnCompleteDownloadHandler;
            progressParser.OnPostProcessingStart -= OnPostProcessingStartHandler;
            progressParser.OnPostProcessingComplete -= OnPostProcessingCompleteHandler;

            runner.OnErrorReceived -= OnErrorMessageHandler;
            runner.OnCommandCompleted -= OnCommandCompletedHandler;
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
