using FluentAssertions;

namespace ManuHub.Ytdlp.NET.Test;

public class ArgumentTests
{
    private readonly string _fullFakePath;
    private static readonly bool RunIntegration = Environment.GetEnvironmentVariable("YTDLP_INTEGRATION_TESTS") == "1";

    public ArgumentTests()
    {

        _fullFakePath = RunIntegration ? "yt-dlp" : Path.Combine(Path.GetTempPath(), "yt-dlp.exe");

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
    public void BuildArguments_ShouldConstructCorrectArguments_WithComplexOptions()
    {
        // Arrange
        var client = new Ytdlp(_fullFakePath)
            .WithOutputTemplate("%(title)s.%(ext)s")
            .WithConcurrentFragments(8)
            .AddOption("--impersonate", "chrome")
            .AddOption("--extractor-args", "youtube:client=ios")
            .WithFFmpegLocation("C:/tools/ffmpeg");

        var url = "https://www.youtube.com/watch?v=12345";

        // Act
        // Use reflection to call the private method or make it internal
        var args = InvokeBuildArguments(client, url);

        // Assert
        args.Should().Contain("-o");
        args.Should().Contain("%(title)s.%(ext)s");
        args.Should().Contain("--concurrent-fragments");
        args.Should().Contain("8");
        args.Should().Contain("--impersonate");
        args.Should().Contain("chrome");
        args.Should().Contain("--ffmpeg-location");
        args.Should().Contain("C:/tools/ffmpeg");
        args.Last().Should().Be(url);
    }

    [Theory]
    [InlineData("simple", "\"simple\"")] // Updated expected value
    [InlineData("path with space", "\"path with space\"")]
    [InlineData("-flag", "-flag")]
    public void EscapeArgument_ShouldFormatCorrectly(string input, string expected)
    {
        var result = InvokeEscapeArgument(input);
        result.Should().Be(expected);
    }

    // Helpers to access private methods for testing
    private List<string> InvokeBuildArguments(Ytdlp client, string url)
    {
        var method = typeof(Ytdlp).GetMethod("BuildArguments",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (List<string>)method.Invoke(client, new object[] { url });
    }

    private string InvokeEscapeArgument(string arg)
    {
        var method = typeof(Ytdlp).GetMethod("EscapeArgument",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return (string)method.Invoke(null, new object[] { arg });
    }
}
