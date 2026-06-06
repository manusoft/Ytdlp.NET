using ManuHub.Ytdlp.NET;

namespace VideoDownloader.Core;

public sealed class YtdlpService
{
    private readonly Ytdlp _root;
    private readonly ILogger _logger;
    private readonly string _path;

    public event Action<DownloadProgressEventArgs>? Progress;
    public event Action<string>? ProgressMessage;
    public event Action<string>? ErrorMessage;
    public event Action? DownloadCompleted;
    public event Action? PostProcessStarted;
    public event Action? PostProcessCompleted;
    public event Action? ProcessCompleted;

    public YtdlpService(string path, ILogger logger)
    {
        _path = path;
        _logger = logger;
        _root = new Ytdlp(path, logger);
    }

    public async Task<string> GetVersionAsync()
        => await _root.VersionAsync() ?? "unknown";

    public async Task<List<Format>> GetFormatsAsync(string url)
        => await _root.GetFormatsAsync(url) ?? [];

    public async Task DownloadAsync(
        string url,
        string format,
        string outputFolder,
        string ffmpeg,
        string outputTemplate)
    {
        try
        {
            var ytdlp = _root
               .WithFormat(format)
               .WithConcurrentFragments(8)
               .WithOutputFolder(outputFolder)
               .WithFFmpegLocation(ffmpeg)
               .WithOutputTemplate(outputTemplate)
               .WithWindowsFilenames();

            Subscribe(ytdlp);

            await ytdlp.DownloadAsync(url);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private void Subscribe(Ytdlp ytdlp)
    {
        ytdlp.ProgressMessage += (s, e) => ProgressMessage?.Invoke(e);

        ytdlp.ErrorMessage += (s, e) => ErrorMessage?.Invoke(e);

        ytdlp.ProgressDownload += (s, e) => Progress?.Invoke(e);

        ytdlp.PostProcessingStarted += (s, e) => PostProcessStarted?.Invoke();

        ytdlp.PostProcessingCompleted += (s, e) =>  PostProcessCompleted?.Invoke();

        ytdlp.DownloadCompleted += (s, e) => DownloadCompleted?.Invoke();
    }
}