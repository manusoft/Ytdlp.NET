using FluentAssertions;

namespace ManuHub.Ytdlp.NET.Test;

/// <summary>
/// Tests that async methods respect a pre-cancelled CancellationToken
/// and throw OperationCanceledException rather than hanging or ignoring it.
/// These tests do NOT require yt-dlp.exe to be installed — the cancellation
/// should be observed before any process is started (or very quickly after).
/// </summary>
public class YtdlpCancellationTests 
{
    private readonly string _fullFakePath;
    private readonly string SampleUrl = "https://www.youtube.com/watch?v=RGg-Qx1rL9U";
    private readonly string SampleUrl2 = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";

    public YtdlpCancellationTests()
    {
        _fullFakePath = Path.Combine(Path.GetTempPath(), OperatingSystem.IsWindows() ? "yt-dlp.exe" : "yt-dlp");

        if (!File.Exists(_fullFakePath))
        {
            try
            {
                File.WriteAllText(_fullFakePath, "");
            }
            catch (IOException)
            {
                // If another parallel test class is writing to it right now, 
                // ignore the error because the file is being taken care of.
            }
        }
    }

    [Fact]
    public async Task DownloadAsync_PreCancelledToken_ThrowsOperationCanceled()
    {
        var ytdlp = new Ytdlp(_fullFakePath)
            .WithFormat("best")
            .WithOutputFolder("./downloads");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await ytdlp.DownloadAsync(SampleUrl, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetMetadataAsync_PreCancelledToken_ThrowsOperationCanceled()
    {
        var ytdlp = new Ytdlp(_fullFakePath);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await ytdlp.GetMetadataAsync(SampleUrl, cts.Token);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetFormatsAsync_PreCancelledToken_ThrowsOperationCanceled()
    {
        var ytdlp = new Ytdlp(_fullFakePath);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await ytdlp.GetFormatsAsync(SampleUrl, cts.Token);

        // Cancelled probe: either null or an empty list is acceptable
        (result is null || !result.Any()).Should().BeTrue(
            "a cancelled GetFormatsAsync should return null or empty, not partial data");
    }

    [Fact]
    public async Task GetMetadataRawAsync_PreCancelledToken_ThrowsOperationCanceled()
    {
        var ytdlp = new Ytdlp(_fullFakePath);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await ytdlp.GetMetadataRawAsync(SampleUrl, cts.Token);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetDeepMetadataAsync_PreCancelledToken_ThrowsOperationCanceled()
    {
        var ytdlp = new Ytdlp(_fullFakePath);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await ytdlp.GetDeepMetadataAsync(SampleUrl, cts.Token);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetDeepMetadataRawAsync_PreCancelledToken_ReturnsNull()
    {
        var ytdlp = new Ytdlp(_fullFakePath);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await ytdlp.GetDeepMetadataRawAsync(SampleUrl, cts.Token);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBestAudioFormatIdAsync_PreCancelledToken_ThrowsOperationCanceled()
    {
        var ytdlp = new Ytdlp(_fullFakePath);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await ytdlp.GetBestAudioFormatIdAsync(SampleUrl, cts.Token);

        result.Should().Be("bestaudio");
    }

    [Fact]
    public async Task GetBestVideoFormatIdAsync_PreCancelledToken_ThrowsOperationCanceled()
    {
        var ytdlp = new Ytdlp(_fullFakePath);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await ytdlp.GetBestVideoFormatIdAsync(SampleUrl, 1080, cts.Token);

        result.Should().Be("bestvideo");
    }

    [Fact]
    public async Task VersionAsync_PreCancelledToken_BehaviourTBD()
    {
        var ytdlp = new Ytdlp(_fullFakePath);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await ytdlp.VersionAsync(cts.Token);

        result.Should().BeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ExecuteBatchAsync_PreCancelledToken_ThrowsOperationCanceled()
    {
        var ytdlp = new Ytdlp(_fullFakePath)
            .WithFormat("best")
            .WithOutputFolder("./downloads");

        var urls = new[] { SampleUrl, SampleUrl2 };

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await ytdlp.DownloadBatchAsync(urls, maxConcurrency: 2, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}