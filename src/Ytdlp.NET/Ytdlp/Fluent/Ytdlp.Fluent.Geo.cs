namespace ManuHub.Ytdlp.NET;

/// <summary>
/// Fluent configuration methods for Ytdlp. 
/// These methods return a new instance of Ytdlp with the specified option added, allowing for chaining multiple configuration calls in a fluent manner.
/// </summary>
public sealed partial class Ytdlp
{
    // ==================================================================================================================
    // GEO-RESTRICTION OPTIONS
    // ==================================================================================================================

    /// <summary>
    /// Use this proxy to verify the IP address for some geo-restricted sites. 
    /// The default proxy specified by <see cref="WithProxy(string?)"/> (or none, if the option is not present) is used for the actual downloading
    /// </summary>
    /// <param name="url"></param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    /// <exception cref="ArgumentException"></exception>
    public Ytdlp WithGeoVerificationProxy(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException(nameof(url));
        return AddOption("--geo-verification-proxy", url);
    }

    /// <summary>
    /// How to fake X-Forwarded-For HTTP header to try bypassing geographic restriction. One of "default" (only when known to be useful),
    /// "never", an IP block in CIDR notation, or a two-letter ISO 3166-2 country code
    /// </summary>
    /// <param name="countryCode"></param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    /// <exception cref="ArgumentException"></exception>
    public Ytdlp WithGeoBypassCountry(string countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode) || countryCode.Length != 2) throw new ArgumentException("Country code must be 2 letters.");
        return AddOption("--xff", countryCode.ToUpper());
    }  
}
