namespace ManuHub.Ytdlp.NET.Test;

public class YtdlpAdobeAuthTests
{
    [Fact]
    public void Should_Create_Instance_With_AdobePass()
    {
        var ytdlp = new Ytdlp()
            .WithAdobePassAuthentication("HBO", "user", "pass");

        Assert.NotNull(ytdlp);
    }

    [Fact]
    public void Should_Include_Mso_And_Username()
    {
        var ytdlp = new Ytdlp()
            .WithAdobePassAuthentication("HBO", "myUser", "secret");

        var args = InvokeBuildArguments(ytdlp, "https://video.com");

        Assert.Contains("--ap-mso", args);
        Assert.Contains("HBO", args);

        Assert.Contains("--ap-username", args);
        Assert.Contains("myUser", args);
    }

    [Fact]
    public void Should_Not_Expose_AdobePass_Password()
    {
        var ytdlp = new Ytdlp()
            .WithAdobePassAuthentication("HBO", "user", "secret123");

        var args = InvokeBuildArguments(ytdlp, "https://video.com");

        Assert.DoesNotContain("secret123", args);
    }

    [Fact]
    public void Should_Use_Stdin_Placeholder_For_AdobePass()
    {
        var ytdlp = new Ytdlp()
            .WithAdobePassAuthentication("HBO", "user", "secret");

        var args = InvokeBuildArguments(ytdlp, "https://video.com");

        var index = args.IndexOf("--ap-password");

        Assert.True(index >= 0);
        Assert.Equal("-", args[index + 1]);
    }

    [Fact]
    public void Should_Throw_On_Invalid_AdobePass()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            new Ytdlp().WithAdobePassAuthentication("", "", "");
        });
    }

    private List<string> InvokeBuildArguments(Ytdlp ytdlp, string url)
    {
        var method = typeof(Ytdlp)
            .GetMethod("BuildArguments",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

        return (List<string>)method!.Invoke(ytdlp, new object[] { url })!;
    }
}
