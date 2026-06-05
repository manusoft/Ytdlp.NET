namespace ManuHub.Ytdlp.NET.Models.Auth;

public sealed record YtdlpAuth
{
    public string Username { get; }
    public string Password { get; }

    public YtdlpAuth(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Invalid credentials.");

        Username = username;
        Password = password;
    }
}
