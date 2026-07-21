namespace ManuHub.Ytdlp.NET;

/// <summary>
/// Fluent configuration methods for Ytdlp. 
/// These methods return a new instance of Ytdlp with the specified option added, allowing for chaining multiple configuration calls in a fluent manner.
/// </summary>
public sealed partial class Ytdlp
{
    // ==================================================================================================================
    // SPONSORBLOCK OPTIONS
    // ==================================================================================================================

    /// <summary>
    /// SponsorBlock categories to create chapters for, separated by commas. 
    /// Available categories are sponsor, intro, outro, selfpromo, preview, filler, interaction, music_offtopic, hook, poi_highlight, chapter, all and default (=all).
    /// You can prefix the category with a "-" to exclude it. E.g. SponsorBlockMark("all,-preview)
    /// </summary>
    /// <param name="categories"></param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithSponsorblockMark(string categories = "all") => AddOption("--sponsorblock-mark", categories);

    /// <summary>
    /// SponsorBlock categories to be removed from the video file, separated by commas. 
    /// If a category is present in both mark and remove, remove takes precedence. Working and available categories are the same as for WithSponsorblockMark()
    /// </summary>
    /// <param name="categories"></param>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithSponsorblockRemove(string categories = "all") => new Ytdlp(this, sponsorblockRemove: categories);

    /// <summary>
    /// Disable both WithSponsorblockMark() and WithSponsorblockRemove() options and do not use any sponsorblock features
    /// </summary>
    /// <returns>A new <see cref="Ytdlp"/> instance.</returns>
    public Ytdlp WithNoSponsorblock() => AddFlag("--no-sponsorblock");

}
