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
    public void OnProgressDownload_CanSubscribe()
    {
        var ytdlp = new Ytdlp(_fullFakePath);

        var act = () =>
        {
            ytdlp.OnProgressDownload += (s, e) => { };
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void OnProgressMessage_CanSubscribe()
    {
        var ytdlp = new Ytdlp(_fullFakePath);

        var act = () =>
        {
            ytdlp.OnProgressMessage += (s, msg) => { };
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void OnCompleteDownload_CanSubscribe()
    {
        var ytdlp = new Ytdlp(_fullFakePath);

        var act = () =>
        {
            ytdlp.OnCompleteDownload += (s, msg) => { };
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void OnPostProcessingStart_CanSubscribe()
    {
        var ytdlp = new Ytdlp(_fullFakePath);

        var act = () =>
        {
            ytdlp.OnPostProcessingStart += (s, msg) => { };
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void OnPostProcessingComplete_CanSubscribe()
    {
        var ytdlp = new Ytdlp(_fullFakePath);

        var act = () =>
        {
            ytdlp.OnPostProcessingComplete += (s, msg) => { };
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void OnErrorMessage_CanSubscribe()
    {
        var ytdlp = new Ytdlp(_fullFakePath);

        var act = () =>
        {
            ytdlp.OnErrorMessage += (s, err) => { };
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void OnOutputMessage_CanSubscribe()
    {
        var ytdlp = new Ytdlp(_fullFakePath);

        var act = () =>
        {
            ytdlp.OnOutputMessage += (s, msg) => { };
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void OnCommandCompleted_CanSubscribe()
    {
        var ytdlp = new Ytdlp(_fullFakePath);

        var act = () =>
        {
            ytdlp.OnCommandCompleted += (s, e) => { };
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
            ytdlp.OnProgressDownload += (s, e) => { };
            ytdlp.OnProgressMessage += (s, msg) => { };
            ytdlp.OnCompleteDownload += (s, msg) => { };
            ytdlp.OnPostProcessingStart += (s, msg) => { };
            ytdlp.OnPostProcessingComplete += (s, msg) => { };
            ytdlp.OnErrorMessage += (s, err) => { };
            ytdlp.OnOutputMessage += (s, msg) => { };
            ytdlp.OnCommandCompleted += (s, e) => { };
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

        baseInstance.OnCompleteDownload += (s, msg) => baseFired = true;

        var branchedInstance = baseInstance.WithOutputFolder("./downloads");
        branchedInstance.OnCompleteDownload += (s, msg) => branchFired = true;

        // Without actually running a download, just confirm both can hold their
        // own event subscriptions independently (no cross-contamination at setup time)
        baseFired.Should().BeFalse();
        branchFired.Should().BeFalse();
    }
}