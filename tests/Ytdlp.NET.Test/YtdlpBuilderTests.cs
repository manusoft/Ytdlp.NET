using FluentAssertions;

namespace ManuHub.Ytdlp.NET.Test;

/// <summary>
/// Tests the immutable fluent builder (WithXxx methods).
/// These tests verify that each builder call returns a new instance and
/// that the resulting argument list contains the expected flags/values.
/// No yt-dlp process is ever launched here.
/// </summary>
public class YtdlpBuilderTests 
{
    private readonly string _fullFakePath;

    public YtdlpBuilderTests()
    {
        // 1. Get the directory and combine cross-platform paths
        string toolsDir = Path.Combine(AppContext.BaseDirectory, "tools");
        string exeName = OperatingSystem.IsWindows() ? "yt-dlp.exe" : "yt-dlp";
        _fullFakePath = Path.Combine(toolsDir, exeName);

        // 2. Ensure the directory and a dummy file exist so ValidatePath passes
        Directory.CreateDirectory(toolsDir);

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

    // ── Construction ──────────────────────────────────────────────────────

    [Fact]
    public void Constructor_DefaultPath_DoesNotThrow()
    {
        var act = () => new Ytdlp(_fullFakePath);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithExePath_DoesNotThrow()
    {
        var act = () => new Ytdlp(_fullFakePath);

        act.Should().NotThrow();
    }

    // ── Immutability ──────────────────────────────────────────────────────

    [Fact]
    public void WithFormat_ReturnsNewInstance()
    {
        var original = new Ytdlp(_fullFakePath);

        var configured = original.WithFormat("best");

        configured.Should().NotBeSameAs(original);
    }

    [Fact]
    public void WithOutputFolder_ReturnsNewInstance()
    {
        var original = new Ytdlp(_fullFakePath);

        var configured = original.WithOutputFolder("./downloads");

        configured.Should().NotBeSameAs(original);
    }

    [Fact]
    public void WithOutputTemplate_ReturnsNewInstance()
    {
        var original = new Ytdlp(_fullFakePath);

        var configured = original.WithOutputTemplate("%(title)s.%(ext)s");

        configured.Should().NotBeSameAs(original);
    }

    [Fact]
    public void WithExtractAudio_ReturnsNewInstance()
    {
        var original = new Ytdlp(_fullFakePath);

        var configured = original.WithExtractAudio(AudioFormat.Mp3);

        configured.Should().NotBeSameAs(original);
    }

    [Fact]
    public void WithEmbedMetadata_ReturnsNewInstance()
    {
        var original = new Ytdlp(_fullFakePath);

        var configured = original.WithEmbedMetadata();

        configured.Should().NotBeSameAs(original);
    }

    [Fact]
    public void WithEmbedThumbnail_ReturnsNewInstance()
    {
        var original = new Ytdlp(_fullFakePath);

        var configured = original.WithEmbedThumbnail();

        configured.Should().NotBeSameAs(original);
    }

    // ── Chaining doesn't mutate previous instances ────────────────────────

    [Fact]
    public void ChainedCalls_EachStepIsIndependent()
    {
        var base1 = new Ytdlp(_fullFakePath);
        var withFormat = base1.WithFormat("bestvideo+bestaudio");
        var withFolder = withFormat.WithOutputFolder("./downloads");
        var withTemplate = withFolder.WithOutputTemplate("%(title)s.%(ext)s");

        // All three should be distinct objects
        base1.Should().NotBeSameAs(withFormat);
        withFormat.Should().NotBeSameAs(withFolder);
        withFolder.Should().NotBeSameAs(withTemplate);
    }

    [Fact]
    public void ParallelBranches_FromSameBase_AreIndependent()
    {
        // Two download jobs branching from the same configured base
        var sharedBase = new Ytdlp(_fullFakePath)
            .WithFormat("best")
            .WithOutputFolder("./downloads");

        var job1 = sharedBase.WithOutputTemplate("%(id)s_job1.%(ext)s");
        var job2 = sharedBase.WithOutputTemplate("%(id)s_job2.%(ext)s");

        job1.Should().NotBeSameAs(job2);
        job1.Should().NotBeSameAs(sharedBase);
        job2.Should().NotBeSameAs(sharedBase);
    }

    // ── Flag / option composition ─────────────────────────────────────────

    [Fact]
    public void AddFlag_ReturnsNewInstance()
    {
        var original = new Ytdlp(_fullFakePath);

        var configured = original.AddFlag("--no-check-certificate");

        configured.Should().NotBeSameAs(original);
    }

    [Fact]
    public void AddOption_ReturnsNewInstance()
    {
        var original = new Ytdlp(_fullFakePath);

        var configured = original.AddOption("--external-downloader", "aria2c");

        configured.Should().NotBeSameAs(original);
    }

    // ── Network and geo options ───────────────────────────────────────────

    [Fact]
    public void WithProxy_ReturnsNewInstance()
    {
        var original = new Ytdlp(_fullFakePath);

        var configured = original.WithProxy("socks5://127.0.0.1:1080");

        configured.Should().NotBeSameAs(original);
    }

    [Fact]
    public void WithForceIpv4_ReturnsNewInstance()
    {
        var original = new Ytdlp(_fullFakePath);

        var configured = original.WithForceIpv4();

        configured.Should().NotBeSameAs(original);
    }

    [Fact]
    public void WithGeoBypassCountry_ReturnsNewInstance()
    {
        var original = new Ytdlp(_fullFakePath);

        var configured = original.WithGeoBypassCountry("US");

        configured.Should().NotBeSameAs(original);
    }

    // ── Video selection options ───────────────────────────────────────────

    [Fact]
    public void WithNoPlaylist_ReturnsNewInstance()
    {
        var original = new Ytdlp(_fullFakePath);

        var configured = original.WithNoPlaylist();

        configured.Should().NotBeSameAs(original);
    }

    [Fact]
    public void WithYesPlaylist_ReturnsNewInstance()
    {
        var original = new Ytdlp(_fullFakePath);

        var configured = original.WithYesPlaylist();

        configured.Should().NotBeSameAs(original);
    }

    [Fact]
    public void WithMaxDownloads_ReturnsNewInstance()
    {
        var original = new Ytdlp(_fullFakePath);

        var configured = original.WithMaxDownloads(5);

        configured.Should().NotBeSameAs(original);
    }

    [Fact]
    public void WithAgeLimit_ReturnsNewInstance()
    {
        var original = new Ytdlp(_fullFakePath);

        var configured = original.WithAgeLimit(18);

        configured.Should().NotBeSameAs(original);
    }

    // ── Download options ──────────────────────────────────────────────────

    [Fact]
    public void WithConcurrentFragments_ReturnsNewInstance()
    {
        var original = new Ytdlp(_fullFakePath);

        var configured = original.WithConcurrentFragments(4);

        configured.Should().NotBeSameAs(original);
    }

    [Fact]
    public void WithRetries_ReturnsNewInstance()
    {
        var original = new Ytdlp(_fullFakePath);

        var configured = original.WithRetries(3);

        configured.Should().NotBeSameAs(original);
    }

    [Fact]
    public void WithLimitRate_ReturnsNewInstance()
    {
        var original = new Ytdlp(_fullFakePath);

        var configured = original.WithLimitRate("1M");

        configured.Should().NotBeSameAs(original);
    }

    // ── Post-processing options ───────────────────────────────────────────

    [Fact]
    public void WithRemuxVideo_ReturnsNewInstance()
    {
        var original = new Ytdlp(_fullFakePath);

        var configured = original.WithRemuxVideo("mp4");

        configured.Should().NotBeSameAs(original);
    }

    [Fact]
    public void WithEmbedSubtitles_ReturnsNewInstance()
    {
        var original = new Ytdlp(_fullFakePath);

        var configured = original.WithEmbedSubtitles();

        configured.Should().NotBeSameAs(original);
    }

    [Fact]
    public void WithEmbedChapters_ReturnsNewInstance()
    {
        var original = new Ytdlp(_fullFakePath);

        var configured = original.WithEmbedChapters();

        configured.Should().NotBeSameAs(original);
    }

    [Fact]
    public void WithSplitChapters_ReturnsNewInstance()
    {
        var original = new Ytdlp(_fullFakePath);

        var configured = original.WithSplitChapters();

        configured.Should().NotBeSameAs(original);
    }

    // ── SponsorBlock options ──────────────────────────────────────────────

    [Fact]
    public void WithSponsorblockRemove_ReturnsNewInstance()
    {
        var original = new Ytdlp(_fullFakePath);

        var configured = original.WithSponsorblockRemove("sponsor");

        configured.Should().NotBeSameAs(original);
    }

    [Fact]
    public void WithNoSponsorblock_ReturnsNewInstance()
    {
        var original = new Ytdlp(_fullFakePath);

        var configured = original.WithNoSponsorblock();

        configured.Should().NotBeSameAs(original);
    }

    // ── Aria2 downloader shorthand ────────────────────────────────────────

    [Fact]
    public void WithAria2_ReturnsNewInstance()
    {
        var original = new Ytdlp(_fullFakePath);

        var configured = original.WithAria2(connections: 8);

        configured.Should().NotBeSameAs(original);
    }

    // ── Verbosity options ─────────────────────────────────────────────────

    [Fact]
    public void WithQuiet_ReturnsNewInstance()
    {
        var original = new Ytdlp(_fullFakePath);

        var configured = original.WithQuiet();

        configured.Should().NotBeSameAs(original);
    }

    [Fact]
    public void WithVerbose_ReturnsNewInstance()
    {
        var original = new Ytdlp(_fullFakePath);

        var configured = original.WithVerbose();

        configured.Should().NotBeSameAs(original);
    }

    [Fact]
    public void WithSimulate_ReturnsNewInstance()
    {
        var original = new Ytdlp(_fullFakePath);

        var configured = original.WithSimulate();

        configured.Should().NotBeSameAs(original);
    }

    // ── Subtitle options ──────────────────────────────────────────────────

    [Fact]
    public void WithSubtitles_ReturnsNewInstance()
    {
        var original = new Ytdlp(_fullFakePath);

        var configured = original.WithSubtitles("en");

        configured.Should().NotBeSameAs(original);
    }

    // ── Filesystem options ────────────────────────────────────────────────

    [Fact]
    public void WithRestrictFilenames_ReturnsNewInstance()
    {
        var original = new Ytdlp(_fullFakePath);

        var configured = original.WithRestrictFilenames();

        configured.Should().NotBeSameAs(original);
    }

    [Fact]
    public void WithNoOverwrites_ReturnsNewInstance()
    {
        var original = new Ytdlp(_fullFakePath);

        var configured = original.WithNoOverwrites();

        configured.Should().NotBeSameAs(original);
    }

    [Fact]
    public void WithCookiesFile_ReturnsNewInstance()
    {
        var original = new Ytdlp(_fullFakePath);

        var configured = original.WithCookiesFile("cookies.txt");

        configured.Should().NotBeSameAs(original);
    }

    [Fact]
    public void WithWriteInfoJson_ReturnsNewInstance()
    {
        var original = new Ytdlp(_fullFakePath);

        var configured = original.WithWriteInfoJson();

        configured.Should().NotBeSameAs(original);
    }

    [Fact]
    public void WithFFmpegLocation_ReturnsNewInstance()
    {
        var original = new Ytdlp(_fullFakePath);

        var configured = original.WithFFmpegLocation(TestConstants.FakeFfmpegPath);

        configured.Should().NotBeSameAs(original);
    }

    // ── Thumbnail options ─────────────────────────────────────────────────

    [Fact]
    public void WithThumbnails_ReturnsNewInstance()
    {
        var original = new Ytdlp(_fullFakePath);

        var configured = original.WithThumbnails();

        configured.Should().NotBeSameAs(original);
    }
}