namespace ManuHub.Ytdlp.NET.Test;

public class YtdlpAuthTests
{
    [Fact]
    public void Should_Create_Instance_With_Authentication()
    {
        var ytdlp = new Ytdlp()
            .WithAuthentication("user", "pass");

        Assert.NotNull(ytdlp);
    }

    [Fact]
    public void Should_Not_Expose_Password_In_Arguments()
    {
        var ytdlp = new Ytdlp()
            .WithAuthentication("user", "secret123");

        var args = InvokeBuildArguments(ytdlp, "https://video.com");

        Assert.DoesNotContain("secret123", args);
        Assert.Contains("--username", args);
        Assert.Contains("user", args);
    }

    [Fact]
    public void Should_Use_Stdin_Password_Placeholder()
    {
        var ytdlp = new Ytdlp()
            .WithAuthentication("user", "secret");

        var args = InvokeBuildArguments(ytdlp, "https://video.com");

        var passwordIndex = args.IndexOf("--password");

        Assert.True(passwordIndex >= 0);
        Assert.Equal("-", args[passwordIndex + 1]);
    }

    [Fact]
    public void Should_Include_Username()
    {
        var ytdlp = new Ytdlp()
            .WithAuthentication("myUser", "secret");

        var args = InvokeBuildArguments(ytdlp, "https://video.com");

        Assert.Contains("--username", args);
        Assert.Contains("myUser", args);
    }

    [Fact]
    public void Should_Throw_On_Invalid_Authentication()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            new Ytdlp().WithAuthentication("", "");
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