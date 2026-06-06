namespace ManuHub.Ytdlp.NET.Models.Auth;

/// <summary>
/// Represents authentication credentials.
/// </summary>
/// <remarks>
/// These credentials are validated at construction time to ensure they are not null or empty.
/// </remarks>
public sealed record YtdlpAuth
{
    /// <summary>Username used for authentication.</summary>
    internal string Username { get; }

    /// <summary>Password used for authentication.</summary>
    internal string Password { get; }

    internal YtdlpAuth(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Invalid credentials.");

        Username = username;
        Password = password;
    }
}
