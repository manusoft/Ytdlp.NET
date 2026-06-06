namespace ManuHub.Ytdlp.NET.Models.Auth;

/// <summary>
/// Represents Adobe Pass authentication credentials used for accessing DRM-protected content.
/// </summary>
/// <remarks>
/// These credentials are validated at construction time to ensure they are not null or empty.
/// </remarks>
public sealed record AdobePassAuth
{
    /// <summary>Multichannel video programming distributor (MSO).</summary>
    internal string Mso { get; }

    /// <summary>Username used for Adobe Pass authentication.</summary>
    internal string Username { get; }

    /// <summary>Password used for Adobe Pass authentication.</summary>
    internal string Password { get; }


    internal AdobePassAuth(string mso, string username, string password)
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
