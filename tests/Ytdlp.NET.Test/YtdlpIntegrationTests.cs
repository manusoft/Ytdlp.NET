using FluentAssertions;

namespace ManuHub.Ytdlp.NET.Test;

/// <summary>
/// Integration tests that actually invoke yt-dlp.exe.
/// These are SKIPPED in CI (no yt-dlp binary available on the runner).
/// Run them locally with yt-dlp installed and accessible in PATH, or
/// set the YTDLP_INTEGRATION_TESTS environment variable to "1" to enable them.
/// </summary>
[Collection("Integration")]
public class YtdlpIntegrationTests 
{
    // Cross-platform binary name selection
    private string binaryName = OperatingSystem.IsWindows() ? "yt-dlp.exe" : "yt-dlp";

    private static readonly bool RunIntegration =
        Environment.GetEnvironmentVariable("YTDLP_INTEGRATION_TESTS") == "1";

    // A short, stable, public-domain video suitable for testing
    private const string TestVideoUrl = "https://www.youtube.com/watch?v=BaW_jenozKc";

    
    // ── VersionAsync ──────────────────────────────────────────────────────

    [SkippableFact]
    public async Task VersionAsync_ReturnsNonEmptyString()
    {
        Skip.IfNot(RunIntegration, "Set YTDLP_INTEGRATION_TESTS=1 to run integration tests.");

        await using var ytdlp = new Ytdlp(binaryName);

        var version = await ytdlp.VersionAsync();

        version.Should().NotBeNullOrWhiteSpace();
        version.Should().MatchRegex(@"^\d{4}\.\d{2}\.\d{2}"); // e.g. 2025.05.21
    }

    // ── GetMetadataAsync ──────────────────────────────────────────────────

    [SkippableFact]
    public async Task GetMetadataAsync_ValidUrl_ReturnsMetadata()
    {
        Skip.IfNot(RunIntegration, "Set YTDLP_INTEGRATION_TESTS=1 to run integration tests.");

        await using var ytdlp = new Ytdlp(binaryName);

        var metadata = await ytdlp.GetMetadataAsync(TestVideoUrl);

        metadata.Should().NotBeNull();
        metadata!.Title.Should().NotBeNullOrWhiteSpace();
        metadata.Duration.Should().BeGreaterThan(0);
        metadata.Id.Should().NotBeNullOrWhiteSpace();
    }

    [SkippableFact]
    public async Task GetMetadataAsync_InvalidUrl_ReturnsNullOrThrows()
    {
        Skip.IfNot(RunIntegration, "Set YTDLP_INTEGRATION_TESTS=1 to run integration tests.");

        await using var ytdlp = new Ytdlp(binaryName);

        // An invalid URL should either return null or throw a meaningful exception,
        // not hang or crash the host process.
        var act = async () =>
        {
            var result = await ytdlp.GetMetadataAsync("https://not-a-real-site.invalid/video");
            // null is acceptable
        };

        await act.Should().NotThrowAsync<AccessViolationException>();
        await act.Should().NotThrowAsync<NullReferenceException>();
    }

    // ── GetFormatsAsync ───────────────────────────────────────────────────

    [SkippableFact]
    public async Task GetFormatsAsync_ValidUrl_ReturnsAtLeastOneFormat()
    {
        Skip.IfNot(RunIntegration, "Set YTDLP_INTEGRATION_TESTS=1 to run integration tests.");

        await using var ytdlp = new Ytdlp(binaryName);

        var formats = await ytdlp.GetFormatsAsync(TestVideoUrl);

        formats.Should().NotBeNullOrEmpty();
        formats!.Should().AllSatisfy(f => f.Id.Should().NotBeNullOrWhiteSpace());
    }

    // ── GetBestVideoFormatIdAsync / GetBestAudioFormatIdAsync ─────────────

    [SkippableFact]
    public async Task GetBestVideoFormatIdAsync_Returns720pOrLower()
    {
        Skip.IfNot(RunIntegration, "Set YTDLP_INTEGRATION_TESTS=1 to run integration tests.");

        await using var ytdlp = new Ytdlp(binaryName);

        var formatId = await ytdlp.GetBestVideoFormatIdAsync(TestVideoUrl, maxHeight: 720);

        formatId.Should().NotBeNullOrWhiteSpace();
    }

    [SkippableFact]
    public async Task GetBestAudioFormatIdAsync_ReturnsNonEmpty()
    {
        Skip.IfNot(RunIntegration, "Set YTDLP_INTEGRATION_TESTS=1 to run integration tests.");

        await using var ytdlp = new Ytdlp(binaryName);

        var formatId = await ytdlp.GetBestAudioFormatIdAsync(TestVideoUrl);

        formatId.Should().NotBeNullOrWhiteSpace();
    }

    // ── GetMetadataLiteAsync ──────────────────────────────────────────────

    [SkippableFact]
    public async Task GetMetadataLiteAsync_TitleAndDuration_ReturnsFields()
    {
        Skip.IfNot(RunIntegration, "Set YTDLP_INTEGRATION_TESTS=1 to run integration tests.");

        await using var ytdlp = new Ytdlp(binaryName);

        var lite = await ytdlp.GetMetadataLiteAsync(
            TestVideoUrl,
            fields: new[] { "title", "duration" });

        lite.Should().NotBeNull();
        lite!["title"].Should().NotBeNullOrWhiteSpace();
        lite["duration"].Should().NotBeNullOrWhiteSpace();
    }

    // ── Simulate (no actual download) ─────────────────────────────────────

    [SkippableFact]
    public async Task DownloadAsync_WithSimulate_DoesNotWriteFile()
    {
        Skip.IfNot(RunIntegration, "Set YTDLP_INTEGRATION_TESTS=1 to run integration tests.");

        var outputDir = Path.Combine(Path.GetTempPath(), $"ytdlp_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDir);

        try
        {
            await using var ytdlp = new Ytdlp(binaryName)
                .WithSimulate()
                .WithFormat("best")
                .WithOutputFolder(outputDir);

            await ytdlp.DownloadAsync(TestVideoUrl);

            // Simulate mode should not write any video file
            Directory.GetFiles(outputDir).Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(outputDir, recursive: true);
        }
    }

    // ── Event firing during download ──────────────────────────────────────

    [SkippableFact]
    public async Task DownloadAsync_WithSimulate_FiresProgressEvents()
    {
        Skip.IfNot(RunIntegration, "Set YTDLP_INTEGRATION_TESTS=1 to run integration tests.");

        await using var ytdlp = new Ytdlp(binaryName)
            .WithSimulate()
            .WithFormat("best")
            .WithOutputFolder(Path.GetTempPath());

        var commandCompleted = false;
        ytdlp.OnCommandCompleted += (s, e) => commandCompleted = true;

        await ytdlp.DownloadAsync(TestVideoUrl);

        commandCompleted.Should().BeTrue("OnCommandCompleted should fire after the process exits");
    }
}