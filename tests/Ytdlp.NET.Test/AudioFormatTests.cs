using FluentAssertions;

namespace ManuHub.Ytdlp.NET.Test;

/// <summary>
/// Tests for the AudioFormat enum used in WithExtractAudio().
/// Verifies that all documented formats are present so a rename or removal
/// is caught immediately.
/// </summary>
public class AudioFormatTests : IDisposable
{
    private readonly string _fullFakePath;

    public AudioFormatTests()
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

    public void Dispose()
    {
        // Clean up the dummy file after all tests in this class finish
        //try { File.Delete(_fullFakePath); } catch { }
    }

    [Theory]
    [InlineData(AudioFormat.Mp3)]
    [InlineData(AudioFormat.Aac)]
    [InlineData(AudioFormat.Flac)]
    [InlineData(AudioFormat.M4a)]
    [InlineData(AudioFormat.Opus)]
    [InlineData(AudioFormat.Vorbis)]
    [InlineData(AudioFormat.Wav)]
    public void AudioFormat_AllExpectedValues_Exist(AudioFormat format)
    {
        // If a format is renamed or removed this test fails with a compile error,
        // which is exactly the signal we want.
        Enum.IsDefined(typeof(AudioFormat), format).Should().BeTrue();
    }

    [Theory]
    [InlineData(AudioFormat.Mp3)]
    [InlineData(AudioFormat.Aac)]
    [InlineData(AudioFormat.Flac)]
    [InlineData(AudioFormat.M4a)]
    [InlineData(AudioFormat.Opus)]
    [InlineData(AudioFormat.Vorbis)]
    [InlineData(AudioFormat.Wav)]
    public void WithExtractAudio_EachAudioFormat_ReturnsNewInstance(AudioFormat format)
    {
        var original = new Ytdlp(_fullFakePath);

        var configured = original.WithExtractAudio(format);

        configured.Should().NotBeSameAs(original);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(10)]
    public void WithExtractAudio_QualityRange_DoesNotThrow(int quality)
    {
        var act = () => new Ytdlp(_fullFakePath)
            .WithExtractAudio(AudioFormat.Mp3, quality);

        act.Should().NotThrow();
    }
}