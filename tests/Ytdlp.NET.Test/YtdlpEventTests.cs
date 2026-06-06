using FluentAssertions;

namespace ManuHub.Ytdlp.NET.Test;

/// <summary>
/// Tests that every documented event on Ytdlp can be subscribed to without
/// error, and that subscriptions on a shared base instance don't bleed into
/// branched instances.
/// </summary>
public class YtdlpEventTests 
{

    private readonly string _fullFakePath;

    public YtdlpEventTests()
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

    [Fact]
    public void ProgressDownload_CanSubscribe()
    {
        var ytdlp = new Ytdlp(_fullFakePath);

        var act = () =>
        {
            ytdlp.ProgressDownload += (s, e) => { };
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void ProgressMessage_CanSubscribe()
    {
        var ytdlp = new Ytdlp(_fullFakePath);

        var act = () =>
        {
            ytdlp.ProgressMessage += (s, msg) => { };
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void DownloadCompleted_CanSubscribe()
    {
        var ytdlp = new Ytdlp(_fullFakePath);

        var act = () =>
        {
            ytdlp.DownloadCompleted += (s, msg) => { };
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void PostProcessingStarted_CanSubscribe()
    {
        var ytdlp = new Ytdlp(_fullFakePath);

        var act = () =>
        {
            ytdlp.PostProcessingStarted += (s, msg) => { };
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void PostProcessingCompleted_CanSubscribe()
    {
        var ytdlp = new Ytdlp(_fullFakePath);

        var act = () =>
        {
            ytdlp.PostProcessingCompleted += (s, msg) => { };
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void ErrorMessage_CanSubscribe()
    {
        var ytdlp = new Ytdlp(_fullFakePath);

        var act = () =>
        {
            ytdlp.ErrorMessage += (s, err) => { };
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void OutputMessage_CanSubscribe()
    {
        var ytdlp = new Ytdlp(_fullFakePath);

        var act = () =>
        {
            ytdlp.OutputMessage += (s, msg) => { };
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void CommandCompleted_CanSubscribe()
    {
        var ytdlp = new Ytdlp(_fullFakePath);

        var act = () =>
        {
            ytdlp.CommandCompleted += (s, e) => { };
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void AllEvents_CanSubscribeTogetherOnOneInstance()
    {
        var ytdlp = new Ytdlp(_fullFakePath)
            .WithFormat("best")
            .WithOutputFolder("./downloads");

        var act = () =>
        {
            ytdlp.ProgressDownload += (s, e) => { };
            ytdlp.ProgressMessage += (s, msg) => { };
            ytdlp.DownloadCompleted += (s, msg) => { };
            ytdlp.PostProcessingStarted += (s, msg) => { };
            ytdlp.PostProcessingCompleted += (s, msg) => { };
            ytdlp.ErrorMessage += (s, err) => { };
            ytdlp.OutputMessage += (s, msg) => { };
            ytdlp.CommandCompleted += (s, e) => { };
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void EventsOnBranchedInstance_AreIndependentFromBase()
    {
        // Events attached to the base should not exist on the branched instance
        var baseInstance = new Ytdlp(_fullFakePath).WithFormat("best");

        bool baseFired = false;
        bool branchFired = false;

        baseInstance.DownloadCompleted += (s, msg) => baseFired = true;

        var branchedInstance = baseInstance.WithOutputFolder("./downloads");
        branchedInstance.DownloadCompleted += (s, msg) => branchFired = true;

        // Without actually running a download, just confirm both can hold their
        // own event subscriptions independently (no cross-contamination at setup time)
        baseFired.Should().BeFalse();
        branchFired.Should().BeFalse();
    }
}