using ManuHub.Ytdlp.NET.Models.Auth;

namespace ManuHub.Ytdlp.NET;

/// <summary>
/// Fluent configuration methods for Ytdlp. 
/// These methods return a new instance of Ytdlp with the specified option added, allowing for chaining multiple configuration calls in a fluent manner.
/// </summary>
public sealed partial class Ytdlp
{
    // ==================================================================================================================
    // AUTHENTICATION OPTIONS
    // ==================================================================================================================

    /// <summary>
    /// Login with this account ID and account password.
    /// </summary>
    /// <param name="username">Account ID</param>
    /// <param name="password">Account password</param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    /// <exception cref="ArgumentException"></exception>
    public Ytdlp WithAuthentication(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Username and password cannot be empty.");
        return new Ytdlp(this, auth: new YtdlpAuth(username, password));
    }

    /// <summary>
    /// Two-factor authentication code
    /// </summary>
    /// <param name="code">Two-factor Code</param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithTwoFactor(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Two-factor code cannot be empty.");
        return AddOption("--twofactor", code);
    }

    /// <summary>
    /// Video-specific password
    /// </summary>
    /// <remarks>
    /// <b>Security warning:</b> Credentials are passed as command-line arguments and are
    /// visible in system process listings (e.g. Task Manager, <c>ps aux</c>).
    /// </remarks>
    /// <param name="password"></param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithVideoPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Video password cannot be empty.");
        return AddOption("--video-password", password);
    }

    /// <summary>
    /// Adobe Pass authentication. MSO is the name of the TV provider, e.g. "comcast", "cox", "verizon".
    /// </summary>
    /// <param name="mso"></param>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    /// <exception cref="ArgumentException"></exception>
    public Ytdlp WithAdobePassAuthentication(string mso, string username, string password)
    {
        if (string.IsNullOrWhiteSpace(mso) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("MSO, username, and password are required for Adobe Pass.");

        return new Ytdlp(this, adobePass: new AdobePassAuth(mso, username, password));
    }
}
