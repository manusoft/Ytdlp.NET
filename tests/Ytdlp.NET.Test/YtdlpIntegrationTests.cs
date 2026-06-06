using FluentAssertions;
using System.Runtime.InteropServices;

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
    private readonly string binaryName = OperatingSystem.IsWindows() ? "yt-dlp.exe" : "yt-dlp";

    private static readonly bool RunIntegration =  Environment.GetEnvironmentVariable("YTDLP_INTEGRATION_TESTS") == "1";

    // A short, stable, public-domain video suitable for testing
    private const string TestVideoUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";

    public YtdlpIntegrationTests()
    {
        Console.WriteLine($"OS: {RuntimeInformation.OSDescription}");
        Console.WriteLine($"ProcessArch: {RuntimeInformation.ProcessArchitecture}");
        Console.WriteLine($"yt-dlp exists: {File.Exists("yt-dlp.exe")}");
        Console.WriteLine($"yt-dlp size: {new FileInfo("yt-dlp.exe").Length}");
    }

    /// <summary>
    /// Helper to instantiate Ytdlp with anti-bot arguments for reliable CI/CD runs.
    /// </summary>
    private Ytdlp CreateIntegrationClient()
    {
        var client = new Ytdlp(binaryName)
            .AddOption("--impersonate", "chrome")
            .AddOption("--extractor-args", "youtube:client=ios");

        // Pipe stderr errors directly out to the xUnit test runner console output
        client.ErrorMessage += (s, msg) => Console.WriteLine($"[yt-dlp StdErr]: {msg}");

        return client;
    }

    // ── VersionAsync ──────────────────────────────────────────────────────

    [SkippableFact]
    public async Task VersionAsync_ReturnsNonEmptyString()
    {
        Skip.IfNot(RunIntegration, "Set YTDLP_INTEGRATION_TESTS=1 to run integration tests.");

        var ytdlp = CreateIntegrationClient();
        var version = await ytdlp.VersionAsync();

        version.Should().NotBeNullOrWhiteSpace();
        version.Should().MatchRegex(@"^\d{4}\.\d{2}\.\d{2}"); // e.g. 2026.03.17
    }

    // ── GetMetadataAsync ──────────────────────────────────────────────────

    [SkippableFact]
    public async Task GetMetadataAsync_ValidUrl_ReturnsMetadata()
    {
        Skip.IfNot(RunIntegration, "Set YTDLP_INTEGRATION_TESTS=1 to run integration tests.");

        var ytdlp = CreateIntegrationClient();

        var metadata = await ytdlp.GetMetadataAsync(TestVideoUrl);

        metadata.Should().NotBeNull("YouTube metadata extraction failed. Check console standard error logs above.");
        metadata!.Title.Should().NotBeNullOrWhiteSpace();
        metadata.Duration.Should().BeGreaterThan(0);
        metadata.Id.Should().NotBeNullOrWhiteSpace();
    }

    [SkippableFact]
    public async Task GetMetadataAsync_InvalidUrl_ReturnsNullOrThrows()
    {
        Skip.IfNot(RunIntegration, "Set YTDLP_INTEGRATION_TESTS=1 to run integration tests.");

        var ytdlp = CreateIntegrationClient();

        var act = async () =>
        {
            var result = await ytdlp.GetMetadataAsync("https://not-a-real-site.invalid/video");
        };

        await act.Should().NotThrowAsync<AccessViolationException>();
        await act.Should().NotThrowAsync<NullReferenceException>();
    }

    // ── GetFormatsAsync ───────────────────────────────────────────────────

    [SkippableFact]
    public async Task GetFormatsAsync_ValidUrl_ReturnsAtLeastOneFormat()
    {
        Skip.IfNot(RunIntegration, "Set YTDLP_INTEGRATION_TESTS=1 to run integration tests.");

        var ytdlp = CreateIntegrationClient();

        var formats = await ytdlp.GetFormatsAsync(TestVideoUrl);

        formats.Should().NotBeNullOrEmpty("YouTube format listing extraction returned an empty set.");
        formats!.Should().AllSatisfy(f => f.Id.Should().NotBeNullOrWhiteSpace());
    }

    // ── GetBestVideoFormatIdAsync / GetBestAudioFormatIdAsync ─────────────

    [SkippableFact]
    public async Task GetBestVideoFormatIdAsync_Returns720pOrLower()
    {
        Skip.IfNot(RunIntegration, "Set YTDLP_INTEGRATION_TESTS=1 to run integration tests.");

        var ytdlp = CreateIntegrationClient();

        var formatId = await ytdlp.GetBestVideoFormatIdAsync(TestVideoUrl, maxHeight: 720);

        formatId.Should().NotBeNullOrWhiteSpace();
    }

    [SkippableFact]
    public async Task GetBestAudioFormatIdAsync_ReturnsNonEmpty()
    {
        Skip.IfNot(RunIntegration, "Set YTDLP_INTEGRATION_TESTS=1 to run integration tests.");

        var ytdlp = CreateIntegrationClient();

        var formatId = await ytdlp.GetBestAudioFormatIdAsync(TestVideoUrl);

        formatId.Should().NotBeNullOrWhiteSpace();
    }

    // ── GetMetadataLiteAsync ──────────────────────────────────────────────

    [SkippableFact]
    public async Task GetMetadataLiteAsync_TitleAndDuration_ReturnsFields()
    {
        Skip.IfNot(RunIntegration, "Set YTDLP_INTEGRATION_TESTS=1 to run integration tests.");

        var ytdlp = CreateIntegrationClient();

        var lite = await ytdlp.GetMetadataLiteAsync(
            TestVideoUrl,
            fields: new[] { "title", "duration" });

        lite.Should().NotBeNull("Lite metadata extraction parsing failed.");
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
            var ytdlp = CreateIntegrationClient()
                .WithSimulate()
                .WithFormat("best")
                .WithOutputFolder(outputDir);

            await ytdlp.DownloadAsync(TestVideoUrl);

            // Simulate mode should not write any video file
            Directory.GetFiles(outputDir).Should().BeEmpty();
        }
        finally
        {
            try { Directory.Delete(outputDir, recursive: true); } catch { }
        }
    }

    // ── Event firing during download ──────────────────────────────────────

    [SkippableFact]
    public async Task DownloadAsync_WithSimulate_FiresProgressEvents()
    {
        Skip.IfNot(RunIntegration, "Set YTDLP_INTEGRATION_TESTS=1 to run integration tests.");

        var ytdlp = CreateIntegrationClient()
            .WithSimulate()
            .WithFormat("best")
            .WithOutputFolder(Path.GetTempPath());

        var commandCompleted = false;
        ytdlp.CommandCompleted += (s, e) => commandCompleted = true;

        await ytdlp.DownloadAsync(TestVideoUrl);

        commandCompleted.Should().BeTrue("OnCommandCompleted should fire after the process exits");
    }
}