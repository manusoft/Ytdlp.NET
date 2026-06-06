namespace ManuHub.Ytdlp.NET;

/// <summary>
/// Represents the yt-dlp update channel used when resolving or downloading builds.
/// </summary>
/// <remarks>
/// Different channels correspond to different release stability levels:
/// stable releases, latest master builds, or nightly development snapshots.
/// </remarks>
public enum UpdateChannel
{
    /// <summary>
    /// Stable release channel. Recommended for production use.
    /// </summary>
    Stable,

    /// <summary>
    /// Master branch builds with the latest merged changes.
    /// May contain experimental or not fully tested features.
    /// </summary>
    Master,

    /// <summary>
    /// Nightly builds generated from the latest development state.
    /// Highly experimental and may be unstable.
    /// </summary>
    Nightly
}