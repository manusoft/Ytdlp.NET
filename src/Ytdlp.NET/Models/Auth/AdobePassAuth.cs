namespace ManuHub.Ytdlp.NET.Models.Auth;

public sealed record AdobePassAuth
{
    public string Mso { get; }
    public string Username { get; }
    public string Password { get; }

    public AdobePassAuth(string mso, string username, string password)
    {
        if (string.IsNullOrWhiteSpace(mso) ||
            string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Invalid credentials.");
        Mso = mso;
        Username = username;
        Password = password;
    }

}
