using FluentAssertions;

namespace ManuHub.Ytdlp.NET.Test;

/// <summary>
/// Tests for the VideoFormat model returned by GetFormatsAsync.
/// Verifies field access and that the format list can be filtered for
/// best-format selection logic.
/// </summary>
public class VideoFormatModelTests
{
    [Fact]
    public void VideoFormat_Id_CanBeSet()
    {
        var format = new Format { Id = "137" };

        format.Id.Should().Be("137");
    }

    [Fact]
    public void VideoFormat_Extension_CanBeSet()
    {
        var format = new Format { Extension = "mp4" };

        format.Extension.Should().Be("mp4");
    }

    [Fact]
    public void VideoFormat_Width_CanBeSet()
    {
        var format = new Format { Width = 1920 };

        format.Width.Should().Be(1920);
    }

    [Fact]
    public void VideoFormat_Height_CanBeSet()
    {
        var format = new Format { Height = 1080 };

        format.Height.Should().Be(1080);
    }

    [Fact]
    public void VideoFormat_Vcodec_CanBeSet()
    {
        var format = new Format { VideoCodec = "avc1" };

        format.VideoCodec.Should().Be("avc1");
    }

    [Fact]
    public void VideoFormat_Acodec_CanBeSet()
    {
        var format = new Format { AudioCodec = "mp4a" };

        format.AudioCodec.Should().Be("mp4a");
    }

    [Fact]
    public void VideoFormat_Tbr_CanBeSet()
    {
        var format = new Format { TotalBitrate = "5000" };

        format.TotalBitrate.Should().Be("5000");
    }

    // ── Common filtering scenarios ────────────────────────────────────────

    [Fact]
    public void FormatList_FilterByHeight_ReturnsCorrectFormats()
    {
        var formats = new List<Format>
        {
            new() { Id = "137", Height = 1080, VideoCodec = "avc1" },
            new() { Id = "136", Height = 720,  VideoCodec = "avc1" },
            new() { Id = "135", Height = 480,  VideoCodec = "avc1" },
            new() { Id = "140", Height = 0,    AudioCodec = "mp4a" }, // audio-only
        };

        var hd = formats.Where(f => f.Height <= 1080 && f.Height > 0).ToList();

        hd.Should().HaveCount(3);
        hd.Should().NotContain(f => f.AudioCodec == "mp4a" && f.Height == 0);
    }

    [Fact]
    public void FormatList_SelectBestByTbr_ReturnHighestBitrate()
    {
        var formats = new List<Format>
        {
            new() { Id = "137", Height = 1080, TotalBitrate = "5000", VideoCodec = "avc1" },
            new() { Id = "248", Height = 1080, TotalBitrate = "8000", VideoCodec = "vp9" },
            new() { Id = "136", Height = 720,  TotalBitrate = "2500", VideoCodec = "avc1" },
        };

        var best = formats.OrderByDescending(f => f.TotalBitrate).First();

        best.Id.Should().Be("248");
    }

    [Fact]
    public void FormatList_Empty_SelectBest_ReturnsNull()
    {
        var formats = new List<Format>();

        var best = formats.OrderByDescending(f => f.TotalBitrate).FirstOrDefault();

        best.Should().BeNull();
    }

    [Fact]
    public void FormatList_FilterAudioOnly_CorrectlyIsolates()
    {
        var formats = new List<Format>
        {
            new() { Id = "140", AudioCodec = "mp4a", VideoCodec = "none", Extension = "m4a" },
            new() { Id = "251", AudioCodec = "opus",  VideoCodec = "none", Extension = "webm" },
            new() { Id = "137", AudioCodec = "none",  VideoCodec = "avc1", Extension = "mp4" },
        };

        var audioOnly = formats.Where(f => f.VideoCodec == "none" && f.AudioCodec != "none").ToList();

        audioOnly.Should().HaveCount(2);
        audioOnly.Should().AllSatisfy(f => f.AudioCodec.Should().NotBe("none"));
    }
}